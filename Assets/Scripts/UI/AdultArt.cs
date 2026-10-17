using UnityEngine;
using UnityEngine.UI;

// 성인 콘텐츠 전용 삽화. Resources/AdultArt/{이벤트 id 또는 행동키_ok|fail}.png가 있을 때만,
// AdultGate.Active일 때만 쓴다. 없으면 아무것도 띄우지 않고 기존 픽토그램을 그대로 쓴다.
// 그릴 그림 목록과 프롬프트는 Docs/adult_art_prompts.csv.
public static class AdultArt
{
    static GameObject root;
    static RawImage image;
    static AspectRatioFitter fitter;

    public static Texture2D Load(string key) =>
        AdultGate.Active && !string.IsNullOrEmpty(key) ? Resources.Load<Texture2D>($"AdultArt/{key}") : null;

    // 이벤트가 떠 있는 동안 삽화를 띄운다 (삽화가 없으면 닫는다)
    public static void Show(string eventId)
    {
        var art = Load(eventId);
        if (art == null) { Hide(); return; }
        if (root == null) Build();
        image.texture = art;
        fitter.aspectRatio = art.width / (float)art.height;
        root.SetActive(true);
    }

    public static void Hide()
    {
        if (root != null) root.SetActive(false);
    }

    static void Build()
    {
        var canvas = UiKit.CreateOverlayCanvas("AdultArtCanvas", 50); // 메뉴(300대)보다 아래
        root = canvas.gameObject;
        var frame = UiKit.Child(canvas, "Frame", typeof(Image));
        // 오른쪽 개인 스탯 칸을 덮는다 (왼쪽 이벤트 글은 가리지 않게)
        UiKit.Anchor(frame, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-194, 15), new Vector2(350, 510));
        frame.GetComponent<Image>().color = UiSkin.Crimson;
        frame.GetComponent<Image>().raycastTarget = false;
        var picture = UiKit.Child(frame, "Picture", typeof(RawImage), typeof(AspectRatioFitter));
        UiKit.Stretch(picture);
        picture.offsetMin = new Vector2(6, 6);
        picture.offsetMax = new Vector2(-6, -6);
        image = picture.GetComponent<RawImage>();
        image.raycastTarget = false;
        fitter = picture.GetComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
    }
}
