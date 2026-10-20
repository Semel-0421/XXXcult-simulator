using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 업적 창 안의 기프티드 도감. 선택한 모드 탭의 기프티드(공유 전체 + 해당 모드 전용)를 칸으로 보여 준다.
// 획득(연결 업적 달성)한 칸은 이름·효과를, 못 한 칸은 ???와 연결 업적 힌트만 보인다. 칸을 누르면 그 모드의 업적 목록으로 이동.
public static class GiftedGalleryView
{
    sealed class Cell
    {
        public GameObject go;
        public Image back, icon;
        public Button button;
        public TMP_Text glyph, name, tag, body;
        public int tabIndex = -1;
    }

    static readonly string[] ModeKeys = { "normal", "god", "mirror" };
    static readonly List<Cell> cells = new List<Cell>();
    static RectTransform root, content;
    static TMP_Text header, empty;
    static TMP_FontAsset font;
    static Action<int> jump;

    // jumpToList(tabIndex): 해당 모드 업적 목록으로 돌아가는 동작
    public static RectTransform Build(RectTransform parent, TMP_FontAsset f, Action<int> jumpToList)
    {
        font = f; jump = jumpToList;
        root = UiKit.Child(parent, "GiftedGallery", typeof(ScrollRect));
        UiKit.Anchor(root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        root.offsetMin = new Vector2(18, 18);
        root.offsetMax = new Vector2(-18, -214);

        header = UiKit.Label(root, font, "Header", 22, TextAlignmentOptions.MidlineLeft, UiSkin.GoldLight);
        header.rectTransform.anchorMin = new Vector2(0, 1); header.rectTransform.anchorMax = Vector2.one;
        header.rectTransform.pivot = new Vector2(0.5f, 1);
        header.rectTransform.offsetMin = new Vector2(8, -34); header.rectTransform.offsetMax = new Vector2(-8, 0);

        var viewport = UiKit.Child(root, "Viewport", typeof(Image), typeof(RectMask2D));
        viewport.anchorMin = Vector2.zero; viewport.anchorMax = Vector2.one;
        viewport.offsetMin = Vector2.zero; viewport.offsetMax = new Vector2(0, -38);
        viewport.GetComponent<Image>().color = new Color(0, 0, 0, 0.01f); // 빈 곳도 끌어서 스크롤되게

        content = UiKit.Child(viewport, "Content", typeof(GridLayoutGroup), typeof(ContentSizeFitter));
        content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one; content.pivot = new Vector2(0.5f, 1);
        content.anchoredPosition = Vector2.zero; content.sizeDelta = Vector2.zero;
        var grid = content.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(452, 130);
        grid.spacing = new Vector2(12, 12);
        grid.padding = new RectOffset(6, 6, 6, 6);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 2;
        content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scrollRect = root.GetComponent<ScrollRect>();
        scrollRect.viewport = viewport; scrollRect.content = content;
        scrollRect.horizontal = false; scrollRect.scrollSensitivity = 30;

        empty = UiKit.Label(root, font, "Empty", 26, TextAlignmentOptions.Center, UiKit.Muted);
        UiKit.Anchor(empty.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        return root;
    }

    // tabIndex: 0 일반, 1 신, 2 거울, 3 히든(기프티드 없음)
    public static void Refresh(int tabIndex)
    {
        var all = tabIndex >= 0 && tabIndex < ModeKeys.Length
            ? GiftedV2Catalog.All.Where(e => (e.Shared || e.Mode == ModeKeys[tabIndex]) && (!e.AdultGated || AdultGate.AchievementsEnabled)).ToList()
            : new List<GiftedV2Catalog.Entry>();
        var got = all.Count(e => Profile.IsUnlocked(e.LinkedAchievement));
        header.text = string.Format(Lang.Get("gg_header"), got, all.Count);
        empty.text = Lang.Get("gg_none");
        empty.gameObject.SetActive(all.Count == 0);

        while (cells.Count < all.Count) cells.Add(MakeCell(cells.Count));
        for (int i = 0; i < cells.Count; i++)
        {
            cells[i].go.SetActive(i < all.Count);
            if (i < all.Count) Fill(cells[i], all[i], tabIndex);
        }
    }

    static Cell MakeCell(int index)
    {
        var rect = UiKit.Child(content, $"Cell{index + 1:000}", typeof(Image), typeof(Button));
        var c = new Cell { go = rect.gameObject, back = rect.GetComponent<Image>(), button = rect.GetComponent<Button>() };
        c.button.targetGraphic = c.back;
        c.button.onClick.AddListener(() => { if (c.tabIndex >= 0) jump?.Invoke(c.tabIndex); });

        var icon = UiKit.Child(rect, "Icon", typeof(Image));
        icon.anchorMin = icon.anchorMax = new Vector2(0, 0.5f);
        icon.sizeDelta = new Vector2(56, 56);
        icon.anchoredPosition = new Vector2(40, 0);
        c.icon = icon.GetComponent<Image>();
        c.icon.raycastTarget = false;
        c.glyph = UiKit.Label(icon, font, "Glyph", 30, TextAlignmentOptions.Center, Color.white);
        UiKit.Anchor(c.glyph.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        c.name = UiKit.Label(rect, font, "Name", 22, TextAlignmentOptions.TopLeft, UiKit.Gold);
        Place(c.name, new Vector2(80, 94), new Vector2(-112, -8));
        c.tag = UiKit.Label(rect, font, "Tag", 16, TextAlignmentOptions.TopRight, UiKit.Muted);
        Place(c.tag, new Vector2(340, 94), new Vector2(-8, -8));
        c.body = UiKit.Label(rect, font, "Body", 17, TextAlignmentOptions.TopLeft, UiKit.Muted);
        c.body.overflowMode = TextOverflowModes.Ellipsis;
        Place(c.body, new Vector2(80, 8), new Vector2(-10, -38));
        return c;
    }

    static void Place(TMP_Text t, Vector2 min, Vector2 max)
    {
        UiKit.Anchor(t.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        t.rectTransform.offsetMin = min; t.rectTransform.offsetMax = max;
        t.raycastTarget = false;
    }

    static void Fill(Cell c, GiftedV2Catalog.Entry e, int tabIndex)
    {
        var done = Profile.IsUnlocked(e.LinkedAchievement);
        var linked = AchievementCatalog.All.FirstOrDefault(a => a.Id == e.LinkedAchievement);
        c.tabIndex = linked != null ? tabIndex : -1;
        c.button.interactable = linked != null;

        var accent = e.Shared ? new Color(0.96f, 0.80f, 0.36f) : new Color(0.66f, 0.52f, 0.95f);
        c.back.color = done ? new Color(0.42f, 0.12f, 0.24f, 0.96f) : new Color(0.16f, 0.07f, 0.24f, 0.9f);
        c.icon.color = done ? accent * 0.35f : new Color(0.1f, 0.06f, 0.15f);
        c.glyph.color = done ? accent : UiKit.Muted;
        c.glyph.text = done ? "✦" : "?";
        c.tag.text = Lang.Get(e.Shared ? "gg_shared" : "gg_exclusive");
        c.tag.color = accent;

        if (!done)
        {
            c.name.text = "???";
            c.name.color = new Color(0.55f, 0.5f, 0.6f);
            c.body.text = linked != null ? string.Format(Lang.Get("gg_hint"), linked.Name) : "";
            c.body.color = new Color(0.5f, 0.47f, 0.57f);
            return;
        }
        var scope = e.Shared ? Lang.Get("gg_all_modes") : Lang.Get("ach_tab_" + e.Mode);
        c.name.text = Lang.Pick(e.Name);
        c.name.color = UiSkin.GoldLight;
        c.body.text = $"{Lang.Pick(e.Flavor)}\n<color=#9BE08A>{Lang.Pick(e.Effect)}</color>  <color=#8F86A0>[{scope}]</color>";
        c.body.color = new Color(0.85f, 0.82f, 0.92f);
    }
}
