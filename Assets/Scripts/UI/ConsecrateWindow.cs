using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 축성(봉헌 건축) 창. 건물 5종의 레벨·지키는 최소치·유지비와 다음 축성 비용을 보여 주고, 고르면 행동 턴을 써서 짓는다.
// 짓는 일은 CultFlow가 Chosen을 받아 처리한다 (행동을 고를 수 있을 때만: CanAct).
public static class ConsecrateWindow
{
    sealed class Row
    {
        public GodBuildingType type;
        public TMP_Text name, info, buttonLabel;
        public Button button;
    }

    public static event Action<GodBuildingType> Chosen;
    public static Func<bool> CanAct { get; set; } = () => false;

    static GameObject root;
    static TMP_Text titleLabel, hintLabel, closeLabel;
    static Row[] rows;

    public static void Open()
    {
        if (root == null) Build();
        Refresh();
        if (!root.activeSelf) Menus.Opened();
        root.SetActive(true);
    }

    public static void Close()
    {
        if (root == null || !root.activeSelf) return;
        Menus.Closed();
        root.SetActive(false);
    }

    static void Build()
    {
        var font = UiKit.FindFont();
        var canvas = UiKit.CreateOverlayCanvas("ConsecrateCanvas", 300);
        var content = UiKit.BuildScrollWindow(canvas, font, "ConsecrateWindow", new Vector2(900, 600),
            out root, out titleLabel, out hintLabel, out var close);
        closeLabel = close.GetComponentInChildren<TMP_Text>();
        close.onClick.AddListener(Close);
        var panel = (RectTransform)root.transform.Find("Panel");
        UiSkin.ApplyBookFrame(panel.GetComponent<Image>(), "ConsecrateBook", new Color(0.30f, 0.12f, 0.32f), new Color(0.12f, 0.06f, 0.22f));

        rows = new Row[GodBuildings.All.Length];
        for (int i = 0; i < rows.Length; i++) rows[i] = BuildRow(content, font, GodBuildings.All[i]);
    }

    static Row BuildRow(RectTransform parent, TMP_FontAsset font, GodBuildingType type)
    {
        var rect = UiKit.Child(parent, type.ToString(), typeof(Image), typeof(LayoutElement));
        rect.GetComponent<Image>().color = UiKit.RowColor;
        rect.GetComponent<LayoutElement>().preferredHeight = 84;
        var row = new Row { type = type };

        row.name = UiKit.Label(rect, font, "Name", 24, TextAlignmentOptions.TopLeft, UiKit.Gold);
        UiKit.Anchor(row.name.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        row.name.rectTransform.offsetMin = new Vector2(16, 8);
        row.name.rectTransform.offsetMax = new Vector2(-300, -8);

        row.info = UiKit.Label(rect, font, "Info", 19, TextAlignmentOptions.BottomLeft, UiKit.Muted);
        UiKit.Anchor(row.info.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        row.info.rectTransform.offsetMin = new Vector2(16, 8);
        row.info.rectTransform.offsetMax = new Vector2(-300, -8);

        row.button = UiKit.MakeButton(rect, font, "Build", "", 270, 52, out row.buttonLabel);
        UiKit.Anchor((RectTransform)row.button.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-151, 0), new Vector2(270, 52));
        row.buttonLabel.enableAutoSizing = true;
        row.buttonLabel.fontSizeMin = 14;
        row.buttonLabel.fontSizeMax = 22;
        row.button.onClick.AddListener(() =>
        {
            if (!CanAct()) return;
            Close();
            Chosen?.Invoke(type);
        });
        return row;
    }

    static void Refresh()
    {
        var god = GameSession.God;
        var cult = GameSession.Cult;
        titleLabel.text = Lang.Get("god_consecrate_title");
        hintLabel.text = Lang.Get(CanAct() ? "god_build_hint" : "god_build_wait");
        closeLabel.text = Lang.Get("close");
        foreach (var row in rows)
        {
            var level = god.Buildings.LevelOf(row.type);
            var stat = GodBuildings.Guards(row.type);
            row.name.text = $"{Lang.Get($"god_building_{row.type}")}  <size=75%>Lv.{level}/{GodBuildings.MaxLevel}</size>";
            row.info.text = string.Format(Lang.Get("god_build_info"), Lang.Get($"stat_{stat}"), GodBuildings.Floor(row.type, level),
                GodBuildings.UpkeepOf(level));
            var blocker = god.Buildings.Blocker(row.type, god.Tree, cult);
            row.button.interactable = blocker == null && CanAct();
            row.buttonLabel.text = blocker switch
            {
                null => string.Format(Lang.Get("god_build_next"), GodBuildings.CostOf(level + 1), GodBuildings.CommunionCost),
                "stage" => string.Format(Lang.Get("god_build_stage"), GodSkillInfo.StageName(level + 1)),
                "offerings" => string.Format(Lang.Get("god_build_offerings"), GodBuildings.CostOf(level + 1)),
                _ => Lang.Get($"god_build_{blocker}"),
            };
        }
    }
}
