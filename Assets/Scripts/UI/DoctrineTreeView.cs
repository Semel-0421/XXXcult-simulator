using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 교리 스킬트리 화면(120개). 가운데 교리 선포에서 여덟 가지가 뻗어 나가고, 덕목은 위쪽·욕망은 아래쪽에 놓인다.
// 각 가지는 5단계까지 한 줄, 6단계부터 A/B 두 갈래로 나뉘어 10단계 최종 스킬(16개)에 이른다.
// 서로 반대되는 가지(절제↔음욕, 자애↔재물욕, 인내↔식욕, 청렴↔권력욕)는 마주 보게 배치한다.
// 노드를 누르면 오른쪽 정보창에 효과·비용이 뜨고, 배우기 버튼이나 노드 더블 클릭으로 포인트를 써서 익힌다.
public class DoctrineTreeView : MonoBehaviour
{
    const float FirstRadius = 120f, RadiusStep = 33f, NodeSize = 30f, FinalSize = 40f, LabelRadius = 462f;
    const float PathSpread = 8f; // 6단계부터 두 갈래가 벌어지는 각도
    const float CenterX = -170f; // 오른쪽 정보창과 겹치지 않게 트리를 왼쪽으로 옮긴다

    static readonly (DoctrineBranch branch, float angle)[] Layout =
    {
        (DoctrineBranch.Temperance, 22.5f), (DoctrineBranch.Charity, 67.5f), (DoctrineBranch.Patience, 112.5f), (DoctrineBranch.Integrity, 157.5f),
        (DoctrineBranch.Lust, 202.5f), (DoctrineBranch.Greed, 247.5f), (DoctrineBranch.Appetite, 292.5f), (DoctrineBranch.Ambition, 337.5f),
    };

    static readonly Color Locked = new(0.25f, 0.23f, 0.30f, 0.9f);
    static readonly Color VirtueColor = new(0.98f, 0.86f, 0.50f);
    static readonly Color DesireColor = new(0.86f, 0.30f, 0.38f);

    sealed class Node
    {
        public DoctrineSkill skill;
        public Image fill, ring;
        public TMP_Text label;
        public Image linkToPrevious;
    }

    readonly List<Node> nodes = new();
    readonly Dictionary<DoctrineBranch, TMP_Text> branchLabels = new();
    TMP_FontAsset font;
    TMP_Text pointsLabel, nameLabel, infoLabel, learnLabel;
    Button learnButton;
    DoctrineSkill selected;
    DoctrineTree tree;
    static Sprite circle;
    Transform window;

    public static DoctrineTreeView Build(Transform window)
    {
        // 트리와 가운데 교리 선포 노드를 확대·이동 영역(TreeZoom) 안에 함께 넣는다
        var declaration = window.Find("Declaration") as RectTransform;
        var content = TreeZoom.Create(window, declaration != null ? declaration.GetSiblingIndex() : 0);
        var root = UiKit.Child(content, "DoctrineTree");
        root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
        root.sizeDelta = Vector2.zero;
        root.anchoredPosition = new Vector2(CenterX, 0);
        if (declaration != null)
        {
            declaration.SetParent(content, false);
            declaration.anchorMin = declaration.anchorMax = new Vector2(0.5f, 0.5f);
            declaration.anchoredPosition = new Vector2(CenterX, declaration.anchoredPosition.y);
        }
        root.SetAsFirstSibling(); // 교리 선포 노드보다 뒤에
        var view = root.gameObject.AddComponent<DoctrineTreeView>();
        view.window = window;
        view.Create();
        return view;
    }

    void Create()
    {
        font = UiKit.FindFont();
        var rect = (RectTransform)transform;

        foreach (var (branch, layoutAngle) in Layout)
        {
            var angle = GameSession.MirrorMode ? -layoutAngle : layoutAngle; // 거울 세계: 위아래를 뒤집어 욕망이 위쪽
            var direction = Direction(angle);
            Vector2 trunkEnd = Vector2.zero;
            Node previous = null;
            for (int tier = 1; tier <= 5; tier++)
            {
                var position = direction * Radius(tier);
                var node = CreateNode(rect, DoctrineCatalog.At(branch, tier), position);
                node.linkToPrevious = CreateLink(rect, previous == null ? direction * 96f : direction * Radius(tier - 1), position);
                nodes.Add(node);
                previous = node;
                trunkEnd = position;
            }
            foreach (var (path, spread) in new[] { ("A", -PathSpread), ("B", PathSpread) })
            {
                var pathDirection = Direction(angle + spread);
                var from = trunkEnd;
                for (int tier = 6; tier <= 10; tier++)
                {
                    var position = pathDirection * Radius(tier);
                    var node = CreateNode(rect, DoctrineCatalog.At(branch, tier, path), position);
                    node.linkToPrevious = CreateLink(rect, from, position);
                    nodes.Add(node);
                    from = position;
                }
            }
            var label = UiKit.Label(rect, font, $"{branch}Label", 22, TextAlignmentOptions.Center, Color.white);
            UiKit.Anchor(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), direction * LabelRadius, new Vector2(160, 30));
            branchLabels[branch] = label;
        }

        BuildInfoPanel();
    }

    static Vector2 Direction(float degrees) => new(Mathf.Cos(degrees * Mathf.Deg2Rad), Mathf.Sin(degrees * Mathf.Deg2Rad));
    static float Radius(int tier) => FirstRadius + RadiusStep * (tier - 1);

    Node CreateNode(RectTransform parent, DoctrineSkill skill, Vector2 position)
    {
        var size = skill.IsFinal ? FinalSize : NodeSize;
        var rect = UiKit.Child(parent, skill.Id, typeof(Image), typeof(Button));
        UiKit.Anchor(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(size, size));
        var ring = rect.GetComponent<Image>();
        ring.sprite = Circle();
        var button = rect.GetComponent<Button>();
        button.targetGraphic = ring;
        button.onClick.AddListener(() => Select(skill));
        rect.gameObject.AddComponent<DoubleClick>().DoubleClicked += () => { selected = skill; Learn(); };

        var fill = UiKit.Child(rect, "Fill", typeof(Image)).GetComponent<Image>();
        UiKit.Anchor((RectTransform)fill.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size - 8, size - 8));
        fill.sprite = skill.IsFinal ? DoctrineEmblems.Get(skill) : Circle(); // 최종 스킬 노드에는 문양을 보여 준다
        fill.raycastTarget = false;

        var label = UiKit.Label(rect, font, "Level", 14, TextAlignmentOptions.Center, Color.white);
        UiKit.Stretch(label.rectTransform);
        return new Node { skill = skill, ring = ring, fill = fill, label = label };
    }

    Image CreateLink(RectTransform parent, Vector2 from, Vector2 to)
    {
        var rect = UiKit.Child(parent, "Link", typeof(Image));
        var delta = to - from;
        UiKit.Anchor(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), (from + to) / 2, new Vector2(delta.magnitude, 3));
        rect.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        rect.SetAsFirstSibling(); // 선은 노드 뒤에
        var image = rect.GetComponent<Image>();
        image.raycastTarget = false;
        return image;
    }

    void BuildInfoPanel()
    {
        // 정보창은 창의 오른쪽 가장자리에 붙인다 (화면 비율이 달라도 잘리지 않게)
        var panel = UiKit.Child(window, "DoctrineInfo", typeof(Image), typeof(Outline));
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(1, 0.5f);
        panel.anchoredPosition = new Vector2(-40, 0);
        panel.sizeDelta = new Vector2(380, 560);
        panel.GetComponent<Image>().color = new Color(0.07f, 0.05f, 0.12f, 0.92f);
        panel.GetComponent<Outline>().effectColor = new Color(0.83f, 0.69f, 0.22f, 0.5f);

        pointsLabel = UiKit.Label(panel, font, "Points", 22, TextAlignmentOptions.TopLeft, UiKit.Gold);
        UiKit.Anchor(pointsLabel.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -50), new Vector2(-36, 80));

        nameLabel = UiKit.Label(panel, font, "Name", 28, TextAlignmentOptions.TopLeft, Color.white);
        UiKit.Anchor(nameLabel.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -130), new Vector2(-36, 70));

        infoLabel = UiKit.Label(panel, font, "Info", 18, TextAlignmentOptions.TopLeft, new Color(0.85f, 0.82f, 0.92f));
        infoLabel.rectTransform.anchorMin = new Vector2(0, 0);
        infoLabel.rectTransform.anchorMax = new Vector2(1, 1);
        infoLabel.rectTransform.offsetMin = new Vector2(18, 90);
        infoLabel.rectTransform.offsetMax = new Vector2(-18, -170);

        learnButton = UiKit.MakeButton(panel, font, "Learn", "", 220, 52, out learnLabel);
        UiKit.Anchor((RectTransform)learnButton.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 46), new Vector2(220, 52));
        learnButton.onClick.AddListener(Learn);
    }

    public void Bind(DoctrineTree doctrineTree)
    {
        if (tree != null) tree.Changed -= OnChanged;
        tree = doctrineTree;
        tree.Changed += OnChanged;
        Refresh();
    }

    void OnDestroy() { if (tree != null) tree.Changed -= OnChanged; }

    void OnChanged(DoctrineSkill _) => Refresh();

    void Select(DoctrineSkill skill)
    {
        selected = skill;
        AudioManager.PlaySfx("click");
        Refresh();
    }

    void Learn()
    {
        if (selected == null || !tree.CanLearn(selected, DoctrinePoints.Available())) return;
        tree.LevelUp(selected);
        AudioManager.PlaySfx("doctrine_learn");
    }

    public void Refresh()
    {
        if (tree == null) return;
        var available = DoctrinePoints.Available();
        var cult = GameSession.Cult;
        pointsLabel.text = $"{string.Format(Lang.Get("doctrine_points"), available)}\n<size=70%><color=#9A8FB0>" +
            string.Format(Lang.Get("doctrine_points_detail"), GameSession.CultTime.Years, Achievements.CountedUnlocked(),
                DoctrineLevel.Of(cult[StatId.Doctrine].Value), tree.SpentPoints) + "</color></size>";

        foreach (var (branch, _) in Layout)
        {
            var desire = branch is DoctrineBranch.Lust or DoctrineBranch.Greed or DoctrineBranch.Appetite or DoctrineBranch.Ambition;
            branchLabels[branch].text = Lang.Get($"pstat_{branch}");
            branchLabels[branch].color = desire ? DesireColor : VirtueColor;
        }

        foreach (var node in nodes)
        {
            var level = tree.LevelOf(node.skill);
            var color = node.skill.IsDesire ? DesireColor : VirtueColor;
            var open = tree.PrerequisitesMet(node.skill);
            var affordable = tree.CanLearn(node.skill, available);
            var dark = new Color(0.08f, 0.06f, 0.12f, 1f);
            node.fill.color = node.skill.IsFinal
                ? (level > 0 ? color : open ? color * 0.55f : new Color(0.3f, 0.28f, 0.34f, 0.8f)) // 문양은 배우기 전에도 흐리게 보인다
                : level > 0 ? color : dark;
            node.ring.color = node.skill == selected ? Color.white : affordable ? color : open ? color * 0.6f : Locked;
            node.label.text = node.skill.IsFinal ? "" : node.skill.Repeatable ? $"{level}/{node.skill.MaxLevel}" : node.skill.Tier.ToString();
            node.label.color = level > 0 ? new Color(0.1f, 0.06f, 0.08f) : open ? Color.white : new Color(0.5f, 0.48f, 0.55f);
            node.linkToPrevious.color = open ? color * 0.8f : Locked;
        }

        RefreshInfo(available);
    }

    void RefreshInfo(int available)
    {
        if (selected == null)
        {
            nameLabel.text = "";
            infoLabel.text = Lang.Get("doctrine_select_hint");
            learnButton.gameObject.SetActive(false);
            return;
        }

        var level = tree.LevelOf(selected);
        var color = selected.IsDesire ? "#DC5A6A" : "#F2D978";
        nameLabel.text = $"<color={color}>{selected.Name}</color>";

        var lines = new List<string>
        {
            $"<color=#9A8FB0>{string.Format(Lang.Get("doctrine_tier"), Lang.Get($"pstat_{selected.Branch}"), selected.Tier)} · " +
            $"{string.Format(Lang.Get("doctrine_level_of"), level, selected.MaxLevel)} · {string.Format(Lang.Get("doctrine_cost"), selected.Cost)}</color>",
            selected.Description,
            "",
            $"<color=#9A8FB0>{Lang.Get(selected.Repeatable ? "doctrine_effects_per_level" : "talent_effects")}</color>",
        };
        lines.AddRange(GiftedInfo.EffectLines(DoctrineTree.ToTalent(selected, 1)));
        if (selected.ResistPercent > 0) lines.Add(string.Format(Lang.Get("doctrine_resist"), selected.ResistPercent));
        if (selected.Tier >= DoctrineTree.OppositeLockTier && tree.IsOppositeLocked(selected)) lines.Insert(1, $"<color=#DC5050>{Lang.Get("doctrine_opposite_blocked")}</color>");
        if (selected.Path != "") lines.Insert(1, $"<color=#9A8FB0>{string.Format(Lang.Get("doctrine_path"), selected.Path)}</color>");
        if (selected.IsFinal) lines.Add("\n<color=#F2D978>" + string.Format(Lang.Get("doctrine_final_hint"), selected.CostForLevel(2)) + "</color>");
        infoLabel.text = string.Join("\n", lines);

        learnButton.gameObject.SetActive(true);
        learnButton.interactable = tree.CanLearn(selected, available);
        learnLabel.text = selected.IsFinal && level >= 1 ? Lang.Get(tree.IsMax(selected) ? "doctrine_final_done" : "doctrine_final_center")
            : tree.IsMax(selected) ? Lang.Get("doctrine_maxed")
            : tree.IsFinalTaken(selected) ? Lang.Get("doctrine_final_taken")
            : tree.IsFinalLocked(selected) ? string.Format(Lang.Get("doctrine_final_locked"), Progress.FinalSkillRun)
            : tree.IsPathBlocked(selected) ? Lang.Get("doctrine_path_blocked")
            : tree.IsOppositeLocked(selected) ? Lang.Get("doctrine_opposite_blocked")
            : !tree.PrerequisitesMet(selected) ? Lang.Get(tree.IsMax(DoctrineSkill.Declaration) ? "doctrine_need_prev" : "doctrine_need_declaration")
            : available < selected.CostForLevel(level + 1) ? Lang.Get("doctrine_need_points")
            : Lang.Get("doctrine_learn_btn");
    }

    static Sprite Circle()
    {
        if (circle != null) return circle;
        const int size = 64;
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = x + 0.5f - size / 2f, dy = y + 0.5f - size / 2f;
            var alpha = Mathf.Clamp01(size / 2f - Mathf.Sqrt(dx * dx + dy * dy));
            pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
        }
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        circle = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return circle;
    }
}
