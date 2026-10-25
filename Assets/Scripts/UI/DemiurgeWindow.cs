using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 황금의 눈동자: 선택된 데미우르고스의 스탯 창 (SYS_DEMIURGE 4절). 이름 옆 버튼(PersonalStatPanel)으로 연다.
// 위에 주도권 비율(2배를 넘으면 승천 때 주도권을 뺏긴다)과 재앙 카운터, 아래에 스탯마다 데미우르고스 / 나를 나란히 보여 준다.
public static class DemiurgeWindow
{
    static GameObject root;
    static TMP_Text title, hint, closeLabel;
    static RectTransform content;
    static TMP_FontAsset font;

    public static bool IsOpen => root != null && root.activeSelf;

    public static void Open()
    {
        if (!GameSession.EyeMode) return;
        if (root == null) Build();
        if (!root.activeSelf) Menus.Opened();
        root.SetActive(true);
        Refresh();
    }

    public static void Close()
    {
        if (!IsOpen) return;
        root.SetActive(false);
        Menus.Closed();
    }

    static void Build()
    {
        font = UiKit.FindFont();
        var canvas = UiKit.CreateOverlayCanvas("DemiurgeCanvas", 306);
        content = UiKit.BuildScrollWindow(canvas, font, "DemiurgeWindow", new Vector2(700, 600), out root, out title, out hint, out var close);
        closeLabel = close.GetComponentInChildren<TMP_Text>();
        close.onClick.AddListener(Close);
        root.SetActive(false);
    }

    // 주도권 막대: 0~3배 범위에 2배 경계를 ┃로 표시하고, 지금 비율까지 채운다
    static string Bar(float ratio)
    {
        const int cells = 24;
        var filled = Mathf.Clamp(Mathf.RoundToInt(ratio / 3f * cells), 0, cells);
        var mark = Mathf.RoundToInt(EyeRules.LoseDominanceAt / 3f * cells);
        var color = ratio > EyeRules.LoseDominanceAt ? "#E65959" : ratio > WarnAt ? "#F2B05A" : "#F2D978";
        var bar = new System.Text.StringBuilder();
        for (int i = 0; i < cells; i++)
        {
            if (i == mark) bar.Append("</color><color=#FFFFFF>|</color><color=" + color + ">");
            bar.Append(i < filled ? '■' : '□');
        }
        return $"<color={color}>{bar}</color>";
    }

    public const float WarnAt = 1.5f; // 이름 옆 버튼이 붉어지는 비율

    static void Refresh()
    {
        var god = GameSession.God;
        var demiurge = god.Demiurge;
        var sector = GameSession.EyeSector;
        title.text = Lang.Get(EyeRules.SectorKey(sector));
        closeLabel.text = Lang.Get("close");
        var ratio = EyeRules.Dominance;
        hint.text = string.Format(Lang.Get("eye_counter"), demiurge.Counter, EyeRules.CalamityThreshold, EyeRules.CounterGain(god.Aeons), demiurge.Calamities);

        foreach (Transform child in content) Object.Destroy(child.gameObject);
        Row($"{string.Format(Lang.Get("eye_dominance"), ratio.ToString("0.00"), EyeRules.LoseDominanceAt.ToString("0"))}\n{Bar(ratio)}", UiType.Body, UiPalette.TextBright);
        Row($"<b>{Lang.Get("eye_col_stat")}<pos=52%>{Lang.Get("eye_col_demiurge")}<pos=78%>{Lang.Get("eye_col_me")}</b>", UiType.Label, UiPalette.Gold);
        foreach (var id in DemiurgeState.CultStats)
            Row(Line(StatLabel.Of(id), demiurge[id], GameSession.Cult[id].Value), UiType.Label, UiPalette.TextSoft);
        foreach (var id in DemiurgeState.PersonalStats)
            Row(Line(Lang.Get($"pstat_{id}"), demiurge[id], GameSession.Personal[id].Value), UiType.Label, UiPalette.TextSoft);
    }

    // 데미우르고스 쪽이 더 크면 붉게
    static string Line(string name, int theirs, int mine) =>
        $"{name}<pos=52%>{(theirs > mine ? $"<color=#E65959>{theirs:N0}</color>" : theirs.ToString("N0"))}<pos=78%>{mine:N0}";

    static void Row(string text, float size, Color color)
    {
        var rect = UiKit.Child(content, "Row", typeof(TextMeshProUGUI), typeof(LayoutElement));
        var label = rect.GetComponent<TextMeshProUGUI>();
        label.font = UiFonts.Body != null ? UiFonts.Body : font;
        label.fontSize = size;
        label.color = color;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.raycastTarget = false;
        label.text = text;
    }
}
