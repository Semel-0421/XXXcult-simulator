using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 코드로 만드는 UI(저장 창, 업적 창, 알림)가 함께 쓰는 작은 도구 모음.
public static class UiKit
{
    // 색은 UiPalette에 모았다 (기존 호출부를 위해 이름만 남김)
    public static readonly Color PanelColor = UiPalette.Panel;
    public static readonly Color RowColor = UiPalette.Row;
    public static readonly Color ButtonColor = UiPalette.Button;
    public static readonly Color Gold = UiPalette.Gold;
    public static readonly Color Muted = UiPalette.Muted;

    // 씬에 이미 있는 글자의 글꼴을 빌려 쓴다 (한글 글꼴을 따로 지정하지 않아도 되도록)
    public static TMP_FontAsset FindFont()
    {
        // 장식 글꼴을 먼저 쓴다 (본문 명조는 Label이 이름을 보고 따로 고른다)
        if (UiFonts.Display != null) return UiFonts.Display;
        // 한글이 들어 있는 글꼴을 우선한다 (기본 영문 글꼴을 빌리면 글자가 빈칸으로 보인다)
        TMP_FontAsset fallback = null;
        foreach (var text in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include))
        {
            if (text.font == null || text.font == UiFonts.Body || text.font == UiFonts.Strong) continue;
            if (text.font.HasCharacter('가')) return text.font;
            fallback ??= text.font;
        }
        return fallback != null ? fallback : TMP_Settings.defaultFontAsset;
    }

    public static RectTransform Child(Transform parent, string name, params System.Type[] components)
    {
        var go = new GameObject(name, typeof(RectTransform));
        foreach (var type in components) go.AddComponent(type);
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    public static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    public static void Anchor(RectTransform rect, Vector2 min, Vector2 max, Vector2 position, Vector2 size)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    public static TMP_Text Label(RectTransform parent, TMP_FontAsset font, string name, float size, TextAlignmentOptions align, Color color)
    {
        var rect = Child(parent, name, typeof(TextMeshProUGUI));
        var text = rect.GetComponent<TextMeshProUGUI>();
        if (font != null) text.font = UiFonts.IsDisplayName(name) || UiFonts.Body == null ? font : UiFonts.Body; // 제목·항목 이름만 장식 글꼴
        text.fontSize = size;
        text.alignment = align;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    public static Button MakeButton(RectTransform parent, TMP_FontAsset font, string name, string label, float width, float height, out TMP_Text labelText)
    {
        var rect = Child(parent, name, typeof(Image), typeof(Button));
        rect.sizeDelta = new Vector2(width, height);
        var image = rect.GetComponent<Image>();
        image.color = ButtonColor;
        var button = rect.GetComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.highlightedColor = new Color(1.2f, 1.2f, 1.3f);
        colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.45f);
        button.colors = colors;

        labelText = Label(rect, font, "Label", 20, TextAlignmentOptions.Center, Color.white);
        if (font != null && UiFonts.Strong != null) labelText.font = UiFonts.Strong; // 버튼은 굵은 명조
        Stretch(labelText.rectTransform);
        labelText.text = label;
        return button;
    }

    // 0~1 값을 고르는 가로 슬라이더
    public static Slider MakeSlider(RectTransform parent, string name, float value)
    {
        var root = Child(parent, name, typeof(Slider));
        var background = Child(root, "Background", typeof(Image));
        background.anchorMin = new Vector2(0, 0.35f);
        background.anchorMax = new Vector2(1, 0.65f);
        background.offsetMin = background.offsetMax = Vector2.zero;
        background.GetComponent<Image>().color = new Color(0.05f, 0.04f, 0.09f);

        var fillArea = Child(root, "Fill Area");
        fillArea.anchorMin = new Vector2(0, 0.35f);
        fillArea.anchorMax = new Vector2(1, 0.65f);
        fillArea.offsetMin = new Vector2(6, 0);
        fillArea.offsetMax = new Vector2(-6, 0);
        var fill = Child(fillArea, "Fill", typeof(Image));
        fill.anchorMin = new Vector2(0, 0);
        fill.anchorMax = new Vector2(0, 1);
        fill.sizeDelta = new Vector2(10, 0);
        fill.GetComponent<Image>().color = new Color(0.62f, 0.52f, 0.9f);

        var handleArea = Child(root, "Handle Slide Area");
        Stretch(handleArea);
        handleArea.offsetMin = new Vector2(12, 0);
        handleArea.offsetMax = new Vector2(-12, 0);
        var handle = Child(handleArea, "Handle", typeof(Image));
        handle.sizeDelta = new Vector2(24, 0);
        handle.anchorMin = new Vector2(0, 0);
        handle.anchorMax = new Vector2(0, 1);
        var handleImage = handle.GetComponent<Image>();
        handleImage.color = Gold;

        var slider = root.GetComponent<Slider>();
        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.targetGraphic = handleImage;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0;
        slider.maxValue = 1;
        slider.value = value;
        return slider;
    }

    // 화면 전체를 덮는 어두운 배경 + 가운데 패널 + 제목·안내·닫기 버튼 + 스크롤 영역을 만든다.
    // 반환값은 스크롤 안의 Content (세로 목록). root는 창 전체 오브젝트(켜고 끄는 용도).
    public static RectTransform BuildScrollWindow(Transform canvasRoot, TMP_FontAsset font, string name, Vector2 size,
        out GameObject root, out TMP_Text title, out TMP_Text hint, out Button close)
    {
        var rootRect = Child(canvasRoot, name, typeof(Canvas), typeof(GraphicRaycaster));
        root = rootRect.gameObject;
        Stretch(rootRect);
        var canvas = rootRect.GetComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 300;

        var dim = Child(rootRect, "Dim", typeof(Image));
        Stretch(dim);
        dim.GetComponent<Image>().color = new Color(0, 0, 0, 0.82f);

        var panel = Child(rootRect, "Panel", typeof(Image), typeof(Outline));
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = size;
        panel.GetComponent<Image>().color = PanelColor;
        panel.GetComponent<Outline>().effectColor = UiPalette.GoldFaint;

        title = Label(panel, font, "Title", UiType.Title, TextAlignmentOptions.Center, Gold);
        Anchor(title.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -41), new Vector2(0, 50));

        hint = Label(panel, font, "Hint", 20, TextAlignmentOptions.Center, Muted);
        Anchor(hint.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -84), new Vector2(0, 30));

        close = MakeButton(panel, font, "Close", "", 120, 44, out _);
        Anchor((RectTransform)close.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-80, -34), new Vector2(120, 44));

        var scroll = Child(panel, "Scroll", typeof(Image), typeof(ScrollRect));
        Anchor(scroll, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        scroll.offsetMin = new Vector2(18, 18);
        scroll.offsetMax = new Vector2(-18, -108);
        scroll.GetComponent<Image>().color = new Color(0, 0, 0, 0.25f);

        var viewport = Child(scroll, "Viewport", typeof(RectMask2D));
        Stretch(viewport);
        var content = Child(viewport, "Content", typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = new Vector2(1, 1);
        content.pivot = new Vector2(0.5f, 1);
        content.offsetMin = content.offsetMax = Vector2.zero;
        var layout = content.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.spacing = 6;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scrollRect = scroll.GetComponent<ScrollRect>();
        scrollRect.viewport = viewport;
        scrollRect.content = content;
        scrollRect.horizontal = false;
        scrollRect.scrollSensitivity = 40;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        root.SetActive(false);
        return content;
    }

    // 장면 캔버스가 없는 곳(타이틀 등)에서도 쓸 수 있게 독립된 오버레이 캔버스를 만든다.
    public static Transform CreateOverlayCanvas(string name, int sortingOrder)
    {
        var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;
        return go.transform;
    }
}
