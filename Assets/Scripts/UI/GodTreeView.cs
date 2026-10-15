using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 권능 트리 화면(125개). 360° 원형으로 바깥 고리(64)에서 안쪽 핵(4)을 거쳐 중심(승천)으로 모인다.
// 기적 ↔ 예언, 자비 ↔ 욕망이 마주 보고, 가지 사이 위쪽 빈틈에 고리 단계(그릇~승천)를 적는다.
//  - 열 때 중심을 확대한 상태에서 서서히 축소하며 전체를 보여 준다 (클릭하면 건너뜀)
//  - 지금 익힐 수 있는 노드는 맥박처럼 빛나고, 바깥 두 노드 중 하나만 익힌 노드는 테두리가 반만 찬다
//  - 익힌 노드에서 아직 못 익힌 안쪽 노드로 빛 점이 흘러 바깥 → 안 방향을 보여 준다
//  - 공격형은 원, 방어형은 마름모. 누르면 오른쪽 정보창, 더블 클릭하면 바로 익힌다
public class GodTreeView : MonoBehaviour
{
    static readonly (GodBranch branch, float angle)[] Sectors =
    {
        (GodBranch.Miracle, 45f), (GodBranch.Mercy, 135f), (GodBranch.Prophecy, 225f), (GodBranch.Desire, 315f),
    };
    const float SectorGap = 12f;                                         // 가지 사이 빈틈 (단계 이름 자리)
    static readonly float[] RingRadius = { 0f, 285f, 230f, 175f, 120f, 68f }; // 고리 번호별 반지름 (0 = 중심)
    static readonly float[] NodeSize = { 58f, 18f, 22f, 26f, 30f, 38f };
    static readonly float[] BandEdge = { 40f, 310f, 257f, 202f, 147f, 94f };  // 단계 띠 바깥 반지름 (0 = 승천 띠)
    static readonly Color[] BandColor =
    {
        new(0.80f, 0.68f, 0.42f), new(0.13f, 0.11f, 0.17f), new(0.18f, 0.14f, 0.24f),
        new(0.26f, 0.18f, 0.30f), new(0.38f, 0.25f, 0.30f), new(0.55f, 0.40f, 0.28f),
    };
    static readonly Color Gold = new(0.98f, 0.88f, 0.55f);
    static readonly Color LockedFill = new(0.12f, 0.10f, 0.16f);
    static readonly Color LockedRing = new(0.30f, 0.28f, 0.36f);
    const float FullIntro = 1.6f, ShortIntro = 0.45f;

    sealed class Node
    {
        public GodSkill skill;
        public RectTransform rect;
        public Image shape, fill, half, glow;
        public float baseScale = 1f;
        public bool pulsing;
    }

    sealed class Link
    {
        public GodSkill child, parent;
        public Vector2 from, to;
        public Image line, dot;
        public float phase;
    }

    readonly Dictionary<string, Node> nodes = new();
    readonly List<Link> links = new();
    readonly Image[] bands = new Image[6];
    readonly TMP_Text[] stageLabels = new TMP_Text[6];
    readonly Dictionary<GodBranch, TMP_Text> branchLabels = new();
    RectTransform content;
    CanvasGroup group;
    TMP_FontAsset font;
    GodState state;
    GodSkill selected;
    float introTime, introLength, introScale;

    public GodSkill Selected => selected;
    public event System.Action<GodSkill> SelectionChanged;

    public static GodTreeView Create(RectTransform area, TMP_FontAsset font)
    {
        var rect = UiKit.Child(area, "Tree", typeof(CanvasGroup));
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = Vector2.zero;
        var view = rect.gameObject.AddComponent<GodTreeView>();
        view.content = rect;
        view.group = rect.GetComponent<CanvasGroup>();
        view.font = font;
        view.Build(area);
        return view;
    }

    // ---- 만들기 ----

    void Build(RectTransform area)
    {
        for (int stage = 1; stage <= 6; stage++) // 바깥 띠부터 깔아야 안쪽 띠가 위에 보인다
        {
            var index = stage == 6 ? 0 : stage;
            var band = UiKit.Child(content, $"Band{stage}", typeof(Image));
            UiKit.Anchor(band, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * BandEdge[index] * 2);
            var image = band.GetComponent<Image>();
            image.sprite = GodShapes.Circle;
            image.raycastTarget = false;
            bands[index] = image;
        }

        var positions = Layout();
        foreach (var skill in GodSkillCatalog.All)
            if (skill.Parent != null) CreateLink(skill, positions[skill.Id], positions[skill.Parent.Id]);
        foreach (var skill in GodSkillCatalog.All) CreateNode(skill, positions[skill.Id]);
        foreach (var link in links) link.dot.transform.SetAsLastSibling(); // 빛 점은 노드 위로

        // 단계 이름: 위쪽 빈틈(90°)에 고리마다
        for (int stage = 1; stage <= 5; stage++)
        {
            var label = UiKit.Label(content, font, $"Stage{stage}", 13, TextAlignmentOptions.Center, new Color(1f, 0.95f, 0.8f, 0.85f));
            UiKit.Anchor(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, RingRadius[stage]), new Vector2(80, 20));
            label.raycastTarget = false;
            label.name = $"StageLabel{stage}";
            stageLabels[stage] = label;
        }

        // 가지 이름: 사분면 바깥 모서리 (트리 영역의 네 귀퉁이)
        var half = area.rect.size / 2 - new Vector2(70, 24);
        foreach (var (branch, angle) in Sectors)
        {
            var corner = new Vector2(Mathf.Sign(Mathf.Cos(angle * Mathf.Deg2Rad)) * half.x, Mathf.Sign(Mathf.Sin(angle * Mathf.Deg2Rad)) * half.y);
            var label = UiKit.Label((RectTransform)content.parent, font, $"{branch}Label", 24, TextAlignmentOptions.Center, BranchColor(branch));
            UiKit.Anchor(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), corner, new Vector2(140, 34));
            label.raycastTarget = false;
            label.name = $"BranchLabel_{branch}";
            branchLabels[branch] = label;
        }
    }

    // 바깥 노드를 가지 사분면 안에 고르게 펼치고, 안쪽 노드는 자식들의 가운데 각도에 놓는다
    static Dictionary<string, Vector2> Layout()
    {
        var angles = new Dictionary<string, float>();
        foreach (var (branch, center) in Sectors)
        {
            var core = GodSkillCatalog.Cores.First(c => c.Branch == branch);
            var leaves = Leaves(core).ToList();
            var span = 90f - SectorGap;
            var step = span / leaves.Count;
            for (int i = 0; i < leaves.Count; i++) angles[leaves[i].Id] = center - span / 2 + step * (i + 0.5f);
            Average(core, angles);
        }
        var result = new Dictionary<string, Vector2>();
        foreach (var skill in GodSkillCatalog.All)
            result[skill.Id] = skill.IsCenter ? Vector2.zero : Direction(angles[skill.Id]) * RingRadius[skill.Ring];
        return result;
    }

    static IEnumerable<GodSkill> Leaves(GodSkill skill) =>
        skill.Children.Count == 0 ? new[] { skill } : skill.Children.SelectMany(Leaves);

    static float Average(GodSkill skill, Dictionary<string, float> angles)
    {
        if (skill.Children.Count == 0) return angles[skill.Id];
        var mean = skill.Children.Average(child => Average(child, angles));
        angles[skill.Id] = mean;
        return mean;
    }

    static Vector2 Direction(float degrees) => new(Mathf.Cos(degrees * Mathf.Deg2Rad), Mathf.Sin(degrees * Mathf.Deg2Rad));

    void CreateNode(GodSkill skill, Vector2 position)
    {
        var size = NodeSize[skill.Ring];
        var diamond = skill.Kind == GodSkillKind.Defense;
        var node = new Node { skill = skill };

        node.glow = Shape(content, $"{skill.Id}_Glow", position, size * 2f, GodShapes.Glow, false);
        node.glow.raycastTarget = false;

        node.half = Shape(content, $"{skill.Id}_Half", position, size + 8, GodShapes.Ring, false);
        node.half.type = Image.Type.Filled;
        node.half.fillMethod = Image.FillMethod.Radial360;
        node.half.fillOrigin = (int)Image.Origin360.Top;
        node.half.fillAmount = 0.5f;
        node.half.raycastTarget = false;

        node.shape = Shape(content, skill.Id, position, diamond ? size * 0.82f : size, diamond ? GodShapes.Square : GodShapes.Circle, diamond);
        node.rect = node.shape.rectTransform;
        var button = node.shape.gameObject.AddComponent<Button>();
        button.targetGraphic = node.shape;
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(() => Select(skill));
        node.shape.gameObject.AddComponent<DoubleClick>().DoubleClicked += () => { Select(skill); TryLearn(); };

        var inner = diamond ? size * 0.82f - 6 : size - 6;
        node.fill = Shape(node.rect, "Fill", Vector2.zero, inner, diamond ? GodShapes.Square : GodShapes.Circle, false);
        node.fill.raycastTarget = false;
        nodes[skill.Id] = node;
    }

    static Image Shape(RectTransform parent, string name, Vector2 position, float size, Sprite sprite, bool rotate)
    {
        var rect = UiKit.Child(parent, name, typeof(Image));
        UiKit.Anchor(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(size, size));
        if (rotate) rect.localRotation = Quaternion.Euler(0, 0, 45);
        var image = rect.GetComponent<Image>();
        image.sprite = sprite;
        return image;
    }

    void CreateLink(GodSkill child, Vector2 from, Vector2 to)
    {
        var rect = UiKit.Child(content, $"Link_{child.Id}", typeof(Image));
        var delta = to - from;
        UiKit.Anchor(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), (from + to) / 2, new Vector2(delta.magnitude, 2));
        rect.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        var line = rect.GetComponent<Image>();
        line.raycastTarget = false;
        var dot = Shape(content, $"Dot_{child.Id}", from, 6, GodShapes.Glow, false);
        dot.raycastTarget = false;
        dot.color = Gold;
        dot.gameObject.SetActive(false);
        links.Add(new Link { child = child, parent = child.Parent, from = from, to = to, line = line, dot = dot, phase = Random.value });
    }

    // ---- 상태 ----

    public void Bind(GodState godState)
    {
        if (state != null) state.Tree.Changed -= OnChanged;
        state = godState;
        state.Tree.Changed += OnChanged;
        if (selected == null || !state.Tree.HasNature) selected = null;
        Refresh();
    }

    void OnDestroy()
    {
        if (state != null) state.Tree.Changed -= OnChanged;
    }

    void OnChanged(GodSkill _) => Refresh();

    public void Select(GodSkill skill)
    {
        selected = skill;
        AudioManager.PlaySfx("click");
        Refresh();
        SelectionChanged?.Invoke(skill);
    }

    // 본성을 아직 안 골랐으면 본성으로, 아니면 포인트를 써서 익힌다
    public bool TryLearn()
    {
        if (state == null || selected == null) return false;
        var tree = state.Tree;
        var done = tree.HasNature ? tree.Learn(selected, state.Available) : tree.ChooseNature(selected);
        if (done) AudioManager.PlaySfx("success");
        SelectionChanged?.Invoke(selected);
        return done;
    }

    public bool CanLearnNow(GodSkill skill)
    {
        var tree = state.Tree;
        return tree.HasNature ? tree.CanLearn(skill, state.Available) : tree.CanChooseNature(skill);
    }

    public void Refresh()
    {
        if (state == null) return;
        var tree = state.Tree;
        var reached = tree.ReachedStage;
        for (int stage = 1; stage <= 5; stage++)
        {
            stageLabels[stage].text = GodSkillInfo.StageName(stage);
            stageLabels[stage].color = stage <= reached ? Gold : new Color(1f, 0.95f, 0.8f, 0.6f);
        }
        foreach (var pair in branchLabels) pair.Value.text = Lang.Get($"god_branch_{pair.Key}");
        for (int index = 0; index < 6; index++)
        {
            var stage = index == 0 ? 6 : index;
            var color = BandColor[index];
            if (stage == reached) color = Color.Lerp(color, Gold, 0.3f);
            color.a = stage <= reached ? 1f : 0.82f;
            bands[index].color = color;
        }

        foreach (var node in nodes.Values)
        {
            var skill = node.skill;
            var branch = BranchColor(skill.Branch);
            var learned = tree.IsLearned(skill);
            var learnable = !learned && CanLearnNow(skill);
            var partial = !learned && !skill.IsOuter && !skill.IsCenter && tree.LearnedChildren(skill) == 1;
            node.pulsing = learnable;
            node.half.gameObject.SetActive(partial);
            node.half.color = branch;
            node.glow.gameObject.SetActive(learnable || tree.IsNature(skill));
            node.glow.color = tree.IsNature(skill) ? new Color(Gold.r, Gold.g, Gold.b, 0.55f) : new Color(1, 1, 1, 0.4f);
            node.fill.color = learned ? branch : learnable ? branch * 0.5f : partial ? branch * 0.3f : LockedFill;
            node.shape.color = skill == selected ? Color.white : learned ? Gold : learnable ? new Color(0.92f, 0.9f, 1f) : partial ? branch * 0.7f : LockedRing;
            node.baseScale = skill == selected ? 1.3f : 1f;
            if (!learnable) node.rect.localScale = Vector3.one * node.baseScale;
        }

        foreach (var link in links)
        {
            var childLearned = tree.IsLearned(link.child);
            var parentLearned = tree.IsLearned(link.parent);
            link.line.color = childLearned && parentLearned ? Gold : childLearned ? new Color(0.9f, 0.8f, 0.55f, 0.55f) : new Color(0.4f, 0.37f, 0.48f, 0.5f);
            link.dot.gameObject.SetActive(childLearned && !parentLearned);
        }
    }

    // ---- 연출 ----

    public void PlayIntro(bool full)
    {
        introLength = full ? FullIntro : ShortIntro;
        introScale = full ? 3.4f : 1.5f;
        introTime = 0;
        content.localScale = Vector3.one * introScale;
        group.blocksRaycasts = false;
    }

    public bool IntroPlaying => introTime < introLength;

    public void SkipIntro()
    {
        introTime = introLength;
        content.localScale = Vector3.one;
        group.blocksRaycasts = true;
    }

    void Update()
    {
        var dt = Time.unscaledDeltaTime;
        if (IntroPlaying)
        {
            if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame) introTime = introLength;
            introTime = Mathf.Min(introLength, introTime + dt);
            var t = 1f - Mathf.Pow(1f - introTime / introLength, 3f); // 끝으로 갈수록 느려진다
            content.localScale = Vector3.one * Mathf.Lerp(introScale, 1f, t);
            if (!IntroPlaying) group.blocksRaycasts = true;
        }

        var time = Time.unscaledTime;
        var pulse = 0.5f + 0.5f * Mathf.Sin(time * 4f);
        foreach (var node in nodes.Values)
        {
            if (!node.pulsing) continue;
            node.rect.localScale = Vector3.one * node.baseScale * (1f + 0.12f * pulse);
            var glow = node.glow.color;
            glow.a = 0.2f + 0.45f * pulse;
            node.glow.color = glow;
        }
        foreach (var link in links)
        {
            if (!link.dot.gameObject.activeSelf) continue;
            var t = Mathf.Repeat(time * 0.6f + link.phase, 1f);
            link.dot.rectTransform.anchoredPosition = Vector2.Lerp(link.from, link.to, t);
            var color = link.dot.color;
            color.a = Mathf.Sin(t * Mathf.PI); // 출발·도착에서 흐려진다
            link.dot.color = color;
        }
    }

    public static Color BranchColor(GodBranch branch) => branch switch
    {
        GodBranch.Miracle => new Color(0.98f, 0.80f, 0.40f),
        GodBranch.Prophecy => new Color(0.55f, 0.78f, 1f),
        GodBranch.Mercy => new Color(0.72f, 0.95f, 0.72f),
        GodBranch.Desire => new Color(0.95f, 0.38f, 0.50f),
        _ => new Color(1f, 1f, 0.9f),
    };
}

// 권능 트리에 쓰는 도형 스프라이트 (코드로 만든다)
public static class GodShapes
{
    static Sprite circle, ring, glow, square;

    public static Sprite Circle => circle ??= Make(64, (d, r) => Mathf.Clamp01(r - d));
    public static Sprite Ring => ring ??= Make(64, (d, r) => Mathf.Clamp01(1.5f - Mathf.Abs(d - (r - 3f))) );
    public static Sprite Glow => glow ??= Make(64, (d, r) => Mathf.Pow(Mathf.Clamp01(1f - d / r), 2f));
    public static Sprite Square => square ??= Make(32, (d, r) => 1f, true);

    static Sprite Make(int size, System.Func<float, float, float> alpha, bool square = false)
    {
        var pixels = new Color32[size * size];
        var r = size / 2f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = x + 0.5f - r, dy = y + 0.5f - r;
            var a = square ? 1f : alpha(Mathf.Sqrt(dx * dx + dy * dy), r);
            pixels[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255));
        }
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
