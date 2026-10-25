using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 본 엔딩을 크게 보는 창: 삽화, 이름, 굿/배드 표시, 엔딩 글, 수수께끼 조건.
public static class EndingDetail
{
    static GameObject root;
    static Image picture;
    static TMP_Text title, tag, body, quiz;
    static TMP_Text closeLabel;

    public static void Build(Transform canvas, TMP_FontAsset font)
    {
        var content = UiKit.BuildScrollWindow(canvas, font, "EndingDetail", new Vector2(760, 640),
            out root, out title, out tag, out var close);
        closeLabel = close.GetComponentInChildren<TMP_Text>();
        close.onClick.AddListener(Close);
        root.GetComponent<Canvas>().sortingOrder = 320; // 도감 위에 뜬다

        var panel = (RectTransform)root.transform.Find("Panel");
        UiSkin.ApplyBookFrame(panel.GetComponent<Image>(), "EndingDetailBook", UiPalette.BookGalleryTop, UiPalette.BookGalleryBottom);
        panel.Find("Close").GetComponent<Image>().color = UiSkin.Crimson;
        ((RectTransform)panel.Find("Scroll")).offsetMax = new Vector2(-18, -108);

        var pictureRect = UiKit.Child(content, "Picture", typeof(Image), typeof(LayoutElement));
        pictureRect.GetComponent<LayoutElement>().preferredHeight = 300;
        picture = pictureRect.GetComponent<Image>();
        picture.preserveAspect = true;
        picture.raycastTarget = false;

        body = Text(content, font, 24, UiPalette.TextBright);
        quiz = Text(content, font, 21, new Color(0.98f, 0.86f, 0.5f));
        root.SetActive(false);
    }

    static TMP_Text Text(RectTransform parent, TMP_FontAsset font, float size, Color color)
    {
        var rect = UiKit.Child(parent, "Text", typeof(TextMeshProUGUI), typeof(LayoutElement));
        var text = rect.GetComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = size;
        text.color = color;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.raycastTarget = false;
        return text;
    }

    public static void Open(Endings.Ending ending)
    {
        var sprite = EndingIllustrations.Get(ending);
        picture.sprite = sprite;
        picture.color = EndingIllustrations.ColorOf(ending);
        title.text = Endings.DisplayName(ending);
        tag.text = ending.Kind switch
        {
            EndingKind.Good => $"<color=#9BE08A>{Lang.Get("end_tag_good")}</color>",
            EndingKind.Bad => $"<color=#DC5050>{Lang.Get("end_tag_bad")}</color>",
            _ => "",
        };
        // 엔딩 글은 자리표시(○○, 연수 ?)로 보여 준다. 실제 교단 이름·연수는 판을 해야 나온다.
        body.text = $"{Lang.Get(ending.DescriptionKey)}\n\n{string.Format(Lang.Get(ending.TextKey), "○○", "?", "?")}";
        quiz.text = $"<color=#F2D978>{Lang.Get("quiz_hint")}</color>\n{EndingQuiz.Of(ending)}";
        closeLabel.text = Lang.Get("close");
        if (!root.activeSelf) Menus.Opened();
        root.SetActive(true);
    }

    public static void Close()
    {
        if (root == null || !root.activeSelf) return;
        Menus.Closed();
        root.SetActive(false);
    }
}
