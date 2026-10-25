using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 엔딩 목록 창: 지금까지 본 엔딩은 이름과 설명을, 아직 못 본 엔딩은 ???로 보여 준다. 아래 "뒤로 돌아가기"로 닫는다.
// 본편 / 준비된 신 / 거울 세계 탭으로 나눈다. 열 때는 방금 끝난 판의 모드 탭을 보여 준다.
public static class EndingWindow
{
    sealed class Row
    {
        public TMP_Text name, description;
        public Image background;
        public Endings.Ending ending;
    }

    static GameObject root;
    static TMP_Text titleLabel, hintLabel, backLabel;
    static Row[] rows;
    static Button[] tabs;
    static TMP_Text[] tabLabels;
    static int tab;

    public static void Open()
    {
        if (root == null) Build();
        tab = EndingGallery.TabOf();
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
        var canvas = UiKit.CreateOverlayCanvas("EndingCanvas", 320);
        var content = UiKit.BuildScrollWindow(canvas, font, "EndingWindow", new Vector2(820, 560),
            out root, out titleLabel, out hintLabel, out var close);
        backLabel = close.GetComponentInChildren<TMP_Text>();
        close.onClick.AddListener(Close);
        // 닫기 버튼을 아래로 길게 놓아 "뒤로 돌아가기"가 잘 보이게 한다
        var closeRect = (RectTransform)close.transform;
        closeRect.sizeDelta = new Vector2(200, 44);
        closeRect.anchoredPosition = new Vector2(-120, -34);
        close.image.color = UiSkin.Crimson;

        var panel = (RectTransform)root.transform.Find("Panel");
        UiSkin.ApplyBookFrame(panel.GetComponent<Image>(), "EndingBook", UiPalette.BookGalleryTop, UiPalette.BookGalleryBottom);

        var endings = Endings.All.Concat(GodRules.AllEndings).Concat(MirrorEndings.All).ToArray();
        rows = new Row[endings.Length];
        for (int i = 0; i < rows.Length; i++) rows[i] = BuildRow(content, font, i, endings[i]);
        tabs = EndingGallery.BuildTabs(panel, font, out tabLabels, t => { tab = t; Refresh(); });
        titleLabel.rectTransform.anchoredPosition += new Vector2(60, 0); // 왼쪽 탭 셋과 오른쪽 뒤로 버튼 사이 가운데로
    }

    static Row BuildRow(RectTransform parent, TMP_FontAsset font, int index, Endings.Ending ending)
    {
        var rect = UiKit.Child(parent, $"Ending{index + 1:00}", typeof(Image), typeof(LayoutElement));
        rect.GetComponent<LayoutElement>().preferredHeight = 128; // 설명 아래 판정 조건 한 줄이 더 들어갈 수 있게
        var row = new Row { background = rect.GetComponent<Image>(), ending = ending };

        row.name = UiKit.Label(rect, font, "Name", 28, TextAlignmentOptions.TopLeft, UiSkin.GoldLight);
        UiKit.Anchor(row.name.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        row.name.rectTransform.offsetMin = new Vector2(18, 10);
        row.name.rectTransform.offsetMax = new Vector2(-18, -10);

        row.description = UiKit.Label(rect, font, "Description", 22, TextAlignmentOptions.BottomLeft, UiKit.Muted);
        UiKit.Anchor(row.description.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        row.description.rectTransform.offsetMin = new Vector2(18, 10);
        row.description.rectTransform.offsetMax = new Vector2(-18, -44);
        return row;
    }

    static string Tag(EndingKind kind) => kind switch
    {
        EndingKind.Good => $"  <size=65%><color=#9BE08A>{Lang.Get("end_tag_good")}</color></size>",
        EndingKind.Bad => $"  <size=65%><color=#DC5050>{Lang.Get("end_tag_bad")}</color></size>",
        _ => "",
    };

    static void Refresh()
    {
        titleLabel.text = Lang.Get("ending_list_title");
        backLabel.text = Lang.Get("ending_back");
        EndingGallery.RefreshTabs(tabs, tabLabels, tab);
        var shown = EndingGallery.ShownIn(tab);
        hintLabel.text = string.Format(Lang.Get("ending_progress"), shown.Count(e => Endings.Seen(e.Id)), shown.Count);

        foreach (var row in rows)
        {
            var ending = row.ending;
            var inTab = shown.Contains(ending);
            row.background.gameObject.SetActive(inTab);
            if (!inTab) continue;
            var seen = Endings.Seen(ending.Id);
            row.background.color = seen ? UiPalette.RowDone : UiPalette.RowIdle;
            row.name.text = seen ? $"{Endings.DisplayName(ending)}{Tag(ending.Kind)}" : Lang.Get("ending_unseen");
            row.name.color = seen ? UiSkin.GoldLight : UiPalette.Locked;
            row.description.text = !seen ? ""
                : Progress.ShowsConditions && ending.Final != null
                    ? $"{Lang.Get(ending.DescriptionKey)}\n<size=85%><color=#F2D978>{Progress.ConditionOf(ending)}</color></size>" // 4회차부터 판정 조건 공개
                    : Lang.Get(ending.DescriptionKey);
        }
    }
}
