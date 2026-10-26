using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 황금의 눈동자: 아이온 트리 창 (SYS_DEMIURGE 6절). 교단 정보 패널의 트리 버튼으로 연다.
// 섹터 4개를 세로 기둥으로, 교리 0~10단계를 아래에서 위로 쌓는다. 0~4단계는 선(왼쪽)·악(오른쪽) 두 칸, 5~10단계는 가운데 한 칸.
// 노드를 누르면 오른쪽에 설명이 뜨고, "익히기"(또는 같은 노드를 한 번 더 누르기)로 익힌다.
// 5~10단계는 네 섹터가 함께 오르는 "하늘의 사다리": 왼쪽 단계 칸에 사다리 기둥과 그리스 머리글자(Γ Ε Ο Σ Π Δ)를 두고,
// 단계 띠를 누르면 그 하늘의 영지주의 설명을 보여 준다 (SYS_DEMIURGE 15절).
public static class AeonTreeWindow
{
    static GameObject root;
    static TMP_Text titleLabel, hintLabel, pointsLabel, stageLabel, closeLabel, nameLabel, infoLabel, learnLabel;
    static Button learnButton;
    static readonly Dictionary<AeonSkill, (Image fill, Outline edge, TMP_Text label)> nodes = new();
    static AeonSkill selected;
    static int selectedRung = -1; // 고른 사다리 단계 (5~10), 없으면 -1
    static readonly Dictionary<int, Image> rungBands = new();
    static readonly Dictionary<EyeSector, TMP_Text> heads = new(); // 섹터 머리글: 언어를 바꿔도 맞게 Refresh에서 쓴다

    // 사다리 단계의 그리스 머리글자와 원어 (두 언어 공통)
    static readonly Dictionary<int, (string glyph, string greek)> Rungs = new()
    {
        [5] = ("Γ", "ΓΝΩΣΙΣ"), [6] = ("Ε", "ΕΒΔΟΜΑΣ"), [7] = ("Ο", "ΟΓΔΟΑΣ"),
        [8] = ("Σ", "ΣΟΦΙΑ"), [9] = ("Π", "ΠΛΗΡΩΜΑ"), [10] = ("Δ", "ΔΗΜΙΟΥΡΓΟΣ"),
    };

    const float LevelColumn = 34, RowGap = 4;

    public static bool IsOpen => root != null && root.activeSelf;

    public static Color SectorColor(EyeSector sector) => sector switch
    {
        EyeSector.Yahweh => new Color(0.95f, 0.82f, 0.42f),
        EyeSector.Satan => new Color(0.86f, 0.32f, 0.36f),
        EyeSector.Samael => new Color(0.42f, 0.78f, 0.62f),
        _ => new Color(0.95f, 0.56f, 0.26f),
    };

    public static void Open()
    {
        if (!GameSession.EyeMode) return;
        if (root == null) Build();
        if (!root.activeSelf) Menus.Opened();
        root.SetActive(true);
        Refresh();
    }

    static void OnLangChanged() { if (IsOpen) Refresh(); } // 창을 연 채 언어를 바꿔도 머리글·사다리 설명이 따라오게

    public static void Close()
    {
        if (!IsOpen) return;
        root.SetActive(false);
        Menus.Closed();
    }

    static void Build()
    {
        nodes.Clear();
        rungBands.Clear();
        heads.Clear();
        selected = null;
        selectedRung = -1;
        var font = UiKit.FindFont();
        var canvas = UiKit.CreateOverlayCanvas("AeonTreeCanvas", 305);
        var rootRect = UiKit.Child(canvas, "AeonTreeWindow", typeof(Image));
        UiKit.Stretch(rootRect);
        rootRect.GetComponent<Image>().color = new Color(0, 0, 0, 0.8f);
        root = rootRect.gameObject;

        var panel = UiKit.Child(rootRect, "Panel", typeof(Image), typeof(Outline));
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = new Vector2(1240, 700);
        UiSkin.ApplyBookFrame(panel.GetComponent<Image>(), "AeonTreeBook", UiPalette.BookAltarTop, UiPalette.BookAltarBottom);
        panel.GetComponent<Outline>().effectColor = UiPalette.GoldFaint;

        titleLabel = UiKit.Label(panel, font, "Title", 32, TextAlignmentOptions.Center, UiKit.Gold);
        UiKit.Anchor(titleLabel.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -32), new Vector2(0, 44));
        hintLabel = UiKit.Label(panel, font, "Hint", 18, TextAlignmentOptions.Center, UiKit.Muted);
        UiKit.Anchor(hintLabel.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -64), new Vector2(0, 26));
        pointsLabel = UiKit.Label(panel, font, "Points", 22, TextAlignmentOptions.Left, UiKit.Gold);
        UiKit.Anchor(pointsLabel.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(150, -30), new Vector2(260, 30));
        stageLabel = UiKit.Label(panel, font, "Stage", 18, TextAlignmentOptions.Left, new Color(1f, 0.95f, 0.8f));
        UiKit.Anchor(stageLabel.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(145, -58), new Vector2(250, 26)); // 가운데 안내문과 겹치지 않게 폭은 그대로 두고 글자를 줄인다
        stageLabel.textWrappingMode = TextWrappingModes.NoWrap; // 영어 "Doctrine tier 10  Throne: Yaldabaoth"가 두 줄로 넘쳤다
        stageLabel.enableAutoSizing = true;
        stageLabel.fontSizeMin = UiType.MinFont(13);
        stageLabel.fontSizeMax = 18;

        var close = UiKit.MakeButton(panel, font, "Close", "", 120, 44, out closeLabel);
        close.image.color = UiSkin.Crimson;
        UiKit.Anchor((RectTransform)close.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-80, -36), new Vector2(120, 44));
        close.onClick.AddListener(Close);
        EscClose.Attach(root, close);

        var area = UiKit.Child(panel, "TreeArea", typeof(Image));
        area.anchorMin = Vector2.zero;
        area.anchorMax = Vector2.one;
        area.offsetMin = new Vector2(20, 20);
        area.offsetMax = new Vector2(-400, -86);
        area.GetComponent<Image>().color = new Color(0.04f, 0.02f, 0.07f, 0.85f);
        BuildGrid(area, font);
        BuildInfoPanel(panel, font);
        Lang.Changed -= OnLangChanged; // 장면이 바뀌어 창을 다시 만들 때 중복 구독하지 않게
        Lang.Changed += OnLangChanged;
        root.SetActive(false);
    }

    // 영역 크기는 패널(1240×700)에서 정해진 값으로 계산한다 (레이아웃 갱신 전에도 맞게)
    static void BuildGrid(RectTransform area, TMP_FontAsset font)
    {
        const float width = 1240 - 20 - 400, height = 700 - 86 - 20, header = 30;
        var columnWidth = (width - LevelColumn) / EyeRules.Sectors.Length;
        var rowHeight = (height - header) / (AeonSkill.AscensionLevel + 1);

        // 사다리 기둥: 10단계 위끝부터 5단계 아래끝까지 단계 칸 양쪽에 가는 금선
        var ladderHeight = (AeonSkill.AscensionLevel - AeonSkill.GnosisLevel + 1) * rowHeight;
        foreach (var railX in new[] { 3f, LevelColumn - 5 })
        {
            var rail = UiKit.Child(area, "LadderRail", typeof(Image));
            Place(rail, railX, -header, 2, ladderHeight);
            var railImage = rail.GetComponent<Image>();
            railImage.color = new Color(UiPalette.Gold.r, UiPalette.Gold.g, UiPalette.Gold.b, 0.45f);
            railImage.raycastTarget = false;
        }

        for (int level = 0; level <= AeonSkill.AscensionLevel; level++)
        {
            var y = -header - (AeonSkill.AscensionLevel - level) * rowHeight; // 위가 10단계
            if (Rungs.TryGetValue(level, out var rung))
            {
                // 네 섹터를 가로지르는 단계 띠 (노드 뒤). 누르면 그 하늘의 설명
                var band = UiKit.Child(area, $"Rung{level}", typeof(Image), typeof(Button));
                Place(band, 0, y - 1, width, rowHeight - 2);
                rungBands[level] = band.GetComponent<Image>();
                var rungLevel = level;
                band.GetComponent<Button>().onClick.AddListener(() => SelectRung(rungLevel));

                var glyph = UiKit.Label(area, font, $"Glyph{level}", 20, TextAlignmentOptions.Center, UiPalette.Gold);
                Place(glyph.rectTransform, 0, y, LevelColumn, rowHeight * 0.62f);
                glyph.text = rung.glyph;
                glyph.raycastTarget = false;
                var small = UiKit.Label(area, font, $"Level{level}", UiType.MinFont(11), TextAlignmentOptions.Center, UiKit.Muted);
                Place(small.rectTransform, 0, y - rowHeight * 0.58f, LevelColumn, rowHeight * 0.4f);
                small.text = level.ToString();
                small.raycastTarget = false;
                continue;
            }
            var number = UiKit.Label(area, font, $"Level{level}", 15, TextAlignmentOptions.Center, UiKit.Muted);
            Place(number.rectTransform, 0, y, LevelColumn, rowHeight);
            number.text = level.ToString();
        }

        for (int c = 0; c < EyeRules.Sectors.Length; c++)
        {
            var sector = EyeRules.Sectors[c];
            var x = LevelColumn + c * columnWidth;
            var head = UiKit.Label(area, font, $"Head{sector}", 18, TextAlignmentOptions.Center, SectorColor(sector));
            Place(head.rectTransform, x, 0, columnWidth, header);
            heads[sector] = head;

            foreach (var skill in AeonSkillCatalog.OfSector(sector))
            {
                var y = -header - (AeonSkill.AscensionLevel - skill.Level) * rowHeight;
                var half = (columnWidth - 12) / 2;
                var (nx, nw) = skill.Kind switch
                {
                    AeonKind.Good => (x + 4, half),
                    AeonKind.Evil => (x + 8 + half, half),
                    _ => (x + 4 + half / 2, half + 4),
                };
                nodes[skill] = MakeNode(area, font, skill, nx, y - RowGap / 2, nw, rowHeight - RowGap);
            }
        }
    }

    static void Place(RectTransform rect, float x, float y, float w, float h)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(w, h);
    }

    static (Image, Outline, TMP_Text) MakeNode(RectTransform area, TMP_FontAsset font, AeonSkill skill, float x, float y, float w, float h)
    {
        var rect = UiKit.Child(area, skill.Id, typeof(Image), typeof(Outline), typeof(Button));
        Place(rect, x, y, w, h);
        var fill = rect.GetComponent<Image>();
        var edge = rect.GetComponent<Outline>();
        edge.effectDistance = new Vector2(2, 2);
        rect.GetComponent<Button>().onClick.AddListener(() => Select(skill));
        var label = UiKit.Label(rect, font, "Name", 13, TextAlignmentOptions.Center, Color.white);
        UiKit.Stretch(label.rectTransform);
        label.rectTransform.offsetMin = new Vector2(3, 1);
        label.rectTransform.offsetMax = new Vector2(-3, -1);
        label.enableAutoSizing = true;
        label.fontSizeMin = UiType.MinFont(9);
        label.fontSizeMax = 13;
        label.raycastTarget = false;
        return (fill, edge, label);
    }

    static void BuildInfoPanel(RectTransform panel, TMP_FontAsset font)
    {
        var info = UiKit.Child(panel, "Info", typeof(Image));
        info.anchorMin = new Vector2(1, 0);
        info.anchorMax = new Vector2(1, 1);
        info.pivot = new Vector2(1, 0.5f);
        info.offsetMin = new Vector2(-380, 20);
        info.offsetMax = new Vector2(-20, -86);
        info.GetComponent<Image>().color = new Color(0.06f, 0.04f, 0.10f, 1f);

        nameLabel = UiKit.Label(info, font, "Name", 28, TextAlignmentOptions.TopLeft, Color.white);
        UiKit.Anchor(nameLabel.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -40), new Vector2(-36, 60));

        infoLabel = UiKit.Label(info, font, "Body", 19, TextAlignmentOptions.TopLeft, new Color(0.88f, 0.85f, 0.95f));
        infoLabel.richText = true;
        infoLabel.rectTransform.anchorMin = Vector2.zero;
        infoLabel.rectTransform.anchorMax = Vector2.one;
        infoLabel.rectTransform.offsetMin = new Vector2(18, 90);
        infoLabel.rectTransform.offsetMax = new Vector2(-18, -76);

        learnButton = UiKit.MakeButton(info, font, "Learn", "", 330, 52, out learnLabel);
        UiKit.Anchor((RectTransform)learnButton.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 46), new Vector2(330, 52));
        learnLabel.enableAutoSizing = true;
        learnLabel.fontSizeMin = 14;
        learnLabel.fontSizeMax = 22;
        learnButton.onClick.AddListener(TryLearn);
    }

    static int DoctrineLevelNow => DoctrineLevel.Of(GameSession.Cult[StatId.Doctrine].Value);

    static string Blocker(AeonSkill skill) => GameSession.God.Aeons.Blocker(skill, GameSession.God.Available, DoctrineLevelNow);

    static void Select(AeonSkill skill)
    {
        if (selected == skill) { TryLearn(); return; } // 같은 노드를 한 번 더 누르면 익힌다
        selected = skill;
        selectedRung = -1;
        AudioManager.PlaySfx("click");
        Refresh();
    }

    static void SelectRung(int level)
    {
        selected = null;
        selectedRung = selectedRung == level ? -1 : level;
        AudioManager.PlaySfx("click");
        Refresh();
    }

    static void TryLearn()
    {
        var god = GameSession.God;
        if (selected == null || !god.Aeons.Learn(selected, god.Available, DoctrineLevelNow)) return;
        AudioManager.PlaySfx("success");
        Refresh();
    }

    static void Refresh()
    {
        var god = GameSession.God;
        var aeons = god.Aeons;
        titleLabel.text = Lang.Get("eye_tree_title");
        hintLabel.text = Lang.Get("eye_tree_hint");
        pointsLabel.text = string.Format(Lang.Get("eye_points"), god.Available);
        var ascension = aeons.AscensionSector == EyeSector.None ? "" : $"  <color=#F2D978>{string.Format(Lang.Get("eye_ascension_sector"), Lang.Get(EyeRules.SectorKey(aeons.AscensionSector)))}</color>";
        stageLabel.text = string.Format(Lang.Get("eye_doctrine_level"), DoctrineLevelNow) + ascension;
        closeLabel.text = Lang.Get("close");
        foreach (var (sector, head) in heads) head.text = Lang.Get(EyeRules.SectorKey(sector));

        foreach (var (skill, view) in nodes)
        {
            var color = SectorColor(skill.Sector);
            var learned = aeons.IsLearned(skill);
            var blocker = learned ? "learned" : Blocker(skill);
            var learnable = blocker == null;
            var waiting = blocker is "doctrine" or "points"; // 조건은 갖췄고 단계·포인트만 모자람
            view.fill.color = learned ? color * 0.85f : learnable ? color * 0.45f : waiting ? color * 0.22f : new Color(0.12f, 0.10f, 0.16f);
            view.fill.color = new Color(view.fill.color.r, view.fill.color.g, view.fill.color.b, 1f);
            view.edge.effectColor = skill == selected ? Color.white : learnable ? UiPalette.Gold : new Color(0, 0, 0, 0.5f);
            view.label.color = learned ? new Color(0.08f, 0.04f, 0.1f) : learnable || waiting ? Color.white : UiPalette.Locked;
            view.label.text = skill.Name;
        }

        foreach (var (level, band) in rungBands)
            band.color = new Color(UiPalette.Gold.r, UiPalette.Gold.g, UiPalette.Gold.b, level == selectedRung ? 0.22f : level % 2 == 1 ? 0.07f : 0.04f);

        if (selectedRung >= 0)
        {
            nameLabel.text = $"<color=#F2D978>{Lang.Get($"eye_rung_{selectedRung}")}</color>"; // 제목 글꼴에는 그리스 문자가 없어 머리글자는 단계 칸과 본문에만 둔다
            infoLabel.text = $"<color=#9A8FB0>{Rungs[selectedRung].greek} · {string.Format(Lang.Get("eye_level"), selectedRung)} · {Lang.Get("eye_rung_tag")}</color>\n{Lang.Get($"eye_rung_{selectedRung}_desc")}";
            learnButton.gameObject.SetActive(false);
            return;
        }

        if (selected == null)
        {
            nameLabel.text = "";
            infoLabel.text = Lang.Get("eye_tree_empty");
            learnButton.gameObject.SetActive(false);
            return;
        }

        var sectorColor = ColorUtility.ToHtmlStringRGB(SectorColor(selected.Sector));
        nameLabel.text = $"<color=#{sectorColor}>{selected.Name}</color>";
        var lines = new List<string>
        {
            $"<color=#9A8FB0>{Lang.Get(EyeRules.SectorKey(selected.Sector))} · {string.Format(Lang.Get("eye_level"), selected.Level)} · {Lang.Get($"eye_kind_{selected.Kind}")} · {string.Format(Lang.Get("god_cost"), selected.Cost)}</color>",
            selected.Description,
            "",
        };
        if (aeons.IsFree(selected)) lines.Insert(1, $"<color=#F2D978>{Lang.Get("eye_start_tag")}</color>");
        lines.AddRange(selected.Effects.Select(e => $"· {GodSkillInfo.Line(e, e.Amount)}"));
        if (selected.Build is { } building) lines.Add($"· {string.Format(Lang.Get("god_eff_build"), Lang.Get($"god_building_{building}"))}");
        if (selected.Calamity > 0) lines.Add($"<color=#E65959>· {string.Format(Lang.Get("eye_eff_calamity"), selected.Calamity)}</color>");
        if (selected.CalamityGuard > 0) lines.Add($"<color=#9BE08A>· {string.Format(Lang.Get("eye_eff_calamity_guard"), selected.CalamityGuard)}</color>");
        infoLabel.text = string.Join("\n", lines);

        var reason = aeons.IsLearned(selected) ? "learned" : Blocker(selected);
        learnButton.gameObject.SetActive(true);
        learnButton.interactable = reason == null;
        learnLabel.text = reason switch
        {
            null => Lang.Get("god_learn"),
            "learned" => Lang.Get("god_learned"),
            "doctrine" => string.Format(Lang.Get("eye_need_doctrine"), selected.DoctrineNeed),
            "points" => Lang.Get("eye_need_points"),
            "ascension" => Lang.Get("eye_need_ascension"),
            "start" => Lang.Get("eye_need_start"),
            _ => Lang.Get(selected.Kind == AeonKind.Gnosis && selected.Level == AeonSkill.GnosisLevel ? "eye_need_both" : "eye_need_below"),
        };
    }
}
