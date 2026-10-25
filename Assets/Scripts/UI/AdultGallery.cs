using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 성인 이벤트 회상 갤러리: 120개 장면을 격자로 보여 준다. 본 장면은 삽화와 제목, 못 본 장면은 ???.
// 본 장면을 누르면 삽화와 이벤트 글만 크게 보여 준다 (선택지·효과 없음). 성인 콘텐츠가 켜져 있을 때만 열린다.
public static class AdultGallery
{
    sealed class Cell
    {
        public string id;
        public Image frame;
        public RawImage picture;
        public GameObject cover;
        public TMP_Text label;
    }

    static GameObject root, viewer;
    static TMP_Text titleLabel, hintLabel, closeLabel, viewerTitle, viewerText, viewerClose;
    static RawImage viewerPicture;
    static AspectRatioFitter viewerFit;
    static RectTransform content;
    static TMP_FontAsset font;
    static readonly List<Cell> cells = new();

    public static void Open()
    {
        if (!AdultGate.ContentEnabled) return;
        if (root == null) Build();
        Refresh();
        if (!root.activeSelf) Menus.Opened();
        root.SetActive(true);
    }

    public static void Close()
    {
        if (root == null || !root.activeSelf) return;
        viewer.SetActive(false);
        Menus.Closed();
        root.SetActive(false);
    }

    // 갤러리용 삽화: 성별 변형 id를 그대로 찾고, 없으면 접미사를 뗀 기본 파일을 쓴다. 게임 중 나이 조건(AdultArt.Load)과 무관
    static Texture2D ArtOf(string id)
    {
        var art = Resources.Load<Texture2D>($"AdultArt/{id}");
        if (art == null && id.Length > 2 && id[id.Length - 2] == '_') art = Resources.Load<Texture2D>($"AdultArt/{id.Substring(0, id.Length - 2)}");
        return art;
    }

    static void Build()
    {
        cells.Clear();
        font = UiKit.FindFont();
        var canvas = UiKit.CreateOverlayCanvas("AdultGalleryCanvas", 310);
        content = UiKit.BuildScrollWindow(canvas, font, "AdultGallery", new Vector2(1040, 680),
            out root, out titleLabel, out hintLabel, out var close);
        closeLabel = close.GetComponentInChildren<TMP_Text>();
        close.onClick.AddListener(Close);

        var panel = (RectTransform)root.transform.Find("Panel");
        UiSkin.ApplyBookFrame(panel.GetComponent<Image>(), "AdultGalleryBook", UiPalette.BookGalleryTop, UiPalette.BookGalleryBottom);
        panel.Find("Close").GetComponent<Image>().color = UiSkin.Crimson;

        Object.DestroyImmediate(content.GetComponent<VerticalLayoutGroup>());
        var grid = content.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(160, 200);
        grid.spacing = new Vector2(12, 12);
        grid.padding = new RectOffset(10, 10, 10, 10);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 6;

        foreach (var t in AdultTitles.All) cells.Add(BuildCell(t.id));
        BuildViewer(panel);
    }

    static Cell BuildCell(string id)
    {
        var rect = UiKit.Child(content, $"Adult_{id}", typeof(Image), typeof(Button));
        var cell = new Cell { id = id, frame = rect.GetComponent<Image>() };
        cell.frame.color = UiPalette.TabIdle;

        var pic = UiKit.Child(rect, "Picture", typeof(RawImage));
        UiKit.Anchor(pic, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        pic.offsetMin = new Vector2(8, 44);
        pic.offsetMax = new Vector2(-8, -8);
        cell.picture = pic.GetComponent<RawImage>();
        cell.picture.raycastTarget = false;

        cell.label = UiKit.Label(rect, font, "Name", 16, TextAlignmentOptions.Bottom, UiSkin.GoldLight);
        UiKit.Anchor(cell.label.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        cell.label.rectTransform.offsetMin = new Vector2(6, 4);
        cell.label.rectTransform.offsetMax = new Vector2(-6, -156);
        cell.label.raycastTarget = false;

        var cover = UiKit.Child(rect, "Cover", typeof(Image));
        UiKit.Stretch(cover);
        cover.GetComponent<Image>().color = new Color(0, 0, 0, 0.94f);
        cover.GetComponent<Image>().raycastTarget = false;
        var mark = UiKit.Label(cover, font, "Mark", 60, TextAlignmentOptions.Center, new Color(0.5f, 0.45f, 0.56f));
        mark.text = "?";
        mark.raycastTarget = false;
        UiKit.Stretch(mark.rectTransform);
        cell.cover = cover.gameObject;

        rect.GetComponent<Button>().onClick.AddListener(() => { if (AdultRecall.IsSeen(id)) OpenViewer(id); });
        return cell;
    }

    // 패널 전체를 덮는 보기 화면 (패널 자식이라 갤러리와 함께 닫힌다)
    static void BuildViewer(RectTransform panel)
    {
        var rect = UiKit.Child(panel, "Viewer", typeof(Image));
        UiKit.Stretch(rect);
        rect.GetComponent<Image>().color = new Color(0.04f, 0.02f, 0.07f, 0.98f);
        viewer = rect.gameObject;

        viewerTitle = UiKit.Label(rect, font, "Title", 30, TextAlignmentOptions.Center, UiKit.Gold);
        UiKit.Anchor(viewerTitle.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -41), new Vector2(0, 50));

        var pic = UiKit.Child(rect, "Picture", typeof(RawImage), typeof(AspectRatioFitter));
        UiKit.Anchor(pic, Vector2.zero, new Vector2(0.45f, 1), Vector2.zero, Vector2.zero);
        pic.offsetMin = new Vector2(20, 20);
        pic.offsetMax = new Vector2(0, -80);
        viewerPicture = pic.GetComponent<RawImage>();
        viewerPicture.raycastTarget = false;
        viewerFit = pic.GetComponent<AspectRatioFitter>();
        viewerFit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;

        viewerText = UiKit.Label(rect, font, "Text", 22, TextAlignmentOptions.TopLeft, UiPalette.TextBright);
        UiKit.Anchor(viewerText.rectTransform, new Vector2(0.45f, 0), Vector2.one, Vector2.zero, Vector2.zero);
        viewerText.rectTransform.offsetMin = new Vector2(24, 20);
        viewerText.rectTransform.offsetMax = new Vector2(-24, -80);
        viewerText.enableAutoSizing = true;
        viewerText.fontSizeMin = 14;
        viewerText.fontSizeMax = 24;
        viewerText.raycastTarget = false;

        var back = UiKit.MakeButton(rect, font, "Back", "", 120, 44, out viewerClose);
        back.image.color = UiSkin.Crimson;
        UiKit.Anchor((RectTransform)back.transform, Vector2.one, Vector2.one, new Vector2(-80, -34), new Vector2(120, 44));
        back.onClick.AddListener(() => viewer.SetActive(false));
        viewer.SetActive(false);
    }

    static void OpenViewer(string id)
    {
        var art = ArtOf(id);
        viewerPicture.gameObject.SetActive(art != null);
        if (art != null)
        {
            viewerPicture.texture = art;
            viewerFit.aspectRatio = art.width / (float)art.height;
        }
        viewerTitle.text = AdultTitles.Title(id);
        var e = AdultEvents.All.FirstOrDefault(x => x.Id == id);
        viewerText.text = e == null ? "" : e.Question.Text; // 질문 글만. 선택지·효과는 보이지 않는다
        viewerClose.text = Lang.Get("close");
        viewer.SetActive(true);
    }

    static void Refresh()
    {
        titleLabel.text = Lang.Get("adult_gallery_title");
        closeLabel.text = Lang.Get("close");
        hintLabel.text = string.Format(Lang.Get("adult_gallery_progress"), AdultRecall.SeenCount, AdultTitles.All.Length);
        foreach (var cell in cells)
        {
            var seen = AdultRecall.IsSeen(cell.id);
            cell.cover.SetActive(!seen);
            var art = seen ? ArtOf(cell.id) : null;
            cell.picture.gameObject.SetActive(art != null);
            cell.picture.texture = art;
            cell.label.text = seen ? AdultTitles.Title(cell.id) : Lang.Get("ending_unseen");
            cell.label.color = seen ? UiSkin.GoldLight : UiPalette.Locked;
            cell.frame.color = seen ? new Color(0.24f, 0.12f, 0.34f, 0.96f) : UiPalette.TabIdle;
        }
    }
}
