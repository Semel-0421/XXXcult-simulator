using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 스킬트리 확대·이동. 마우스 휠·두 손가락 집기로 확대/축소, 끌어서 이동, 화면 버튼(+ − 원래대로)도 쓸 수 있다.
// 이 컴포넌트가 붙은 투명 영역이 빈 곳의 입력을 받고, 노드 위에서 시작한 끌기도 여기로 올라온다.
public class TreeZoom : MonoBehaviour, IDragHandler, IScrollHandler
{
    const float MinScale = 0.6f, MaxScale = 2.5f, WheelStep = 0.12f, ButtonStep = 1.25f, PanLimit = 520f;

    RectTransform viewport, content;
    float pinchDistance;

    public static RectTransform Create(Transform window, int siblingIndex)
    {
        var viewport = UiKit.Child(window, "TreeViewport", typeof(Image), typeof(RectMask2D));
        UiKit.Stretch(viewport);
        viewport.SetSiblingIndex(siblingIndex);
        viewport.GetComponent<Image>().color = Color.clear; // 빈 곳의 끌기·휠을 받기 위한 투명 판
        var content = UiKit.Child(viewport, "Content");
        content.anchorMin = content.anchorMax = new Vector2(0.5f, 0.5f);
        content.sizeDelta = Vector2.zero;
        var zoom = viewport.gameObject.AddComponent<TreeZoom>();
        zoom.viewport = viewport;
        zoom.content = content;
        zoom.BuildButtons(window);
        return content;
    }

    void BuildButtons(Transform window)
    {
        var font = UiKit.FindFont();
        var bar = UiKit.Child(window, "ZoomButtons");
        bar.anchorMin = bar.anchorMax = bar.pivot = new Vector2(0, 0);
        bar.anchoredPosition = new Vector2(40, 40);
        bar.sizeDelta = new Vector2(200, 56);
        (string name, string text, System.Action action)[] buttons =
        {
            ("ZoomIn", "+", () => ZoomAt(Center(), ButtonStep)),
            ("ZoomOut", "-", () => ZoomAt(Center(), 1 / ButtonStep)),
            ("ZoomReset", "1:1", Reset),
        };
        for (int i = 0; i < buttons.Length; i++)
        {
            var (name, text, action) = buttons[i];
            var button = UiKit.MakeButton(bar, font, name, text, 56, 56, out var label);
            label.fontSize = text.Length > 1 ? 22 : 32;
            label.alignment = TextAlignmentOptions.Center;
            UiKit.Anchor((RectTransform)button.transform, Vector2.zero, Vector2.zero, new Vector2(28 + i * 66, 28), new Vector2(56, 56));
            button.onClick.AddListener(() => action());
        }
    }

    void OnEnable() => Reset();

    void Reset()
    {
        if (content == null) return;
        content.localScale = Vector3.one;
        content.anchoredPosition = Vector2.zero;
    }

    public void OnScroll(PointerEventData eventData) =>
        ZoomAt(eventData.position, 1 + Mathf.Sign(eventData.scrollDelta.y) * WheelStep);

    public void OnDrag(PointerEventData eventData)
    {
        if (Touchscreen.current != null && TouchCount() >= 2) return; // 집는 중에는 이동하지 않는다
        content.anchoredPosition += eventData.delta / ScaleFactor();
        Clamp();
    }

    void Update()
    {
        if (Touchscreen.current == null || TouchCount() < 2) { pinchDistance = 0; return; }
        var touches = Touchscreen.current.touches;
        Vector2 a = touches[0].position.ReadValue(), b = touches[1].position.ReadValue();
        var distance = Vector2.Distance(a, b);
        if (pinchDistance > 0 && distance > 0) ZoomAt((a + b) / 2, distance / pinchDistance);
        pinchDistance = distance;
    }

    static int TouchCount()
    {
        int count = 0;
        foreach (var touch in Touchscreen.current.touches) if (touch.isInProgress) count++;
        return count;
    }

    // 화면 좌표 point가 가리키는 트리 지점이 확대 후에도 같은 자리에 머물도록 위치를 맞춘다
    void ZoomAt(Vector2 screenPoint, float factor)
    {
        var oldScale = content.localScale.x;
        var newScale = Mathf.Clamp(oldScale * factor, MinScale, MaxScale);
        if (Mathf.Approximately(oldScale, newScale)) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, screenPoint, EventCamera(), out var local);
        content.anchoredPosition = local - (local - content.anchoredPosition) * (newScale / oldScale);
        content.localScale = new Vector3(newScale, newScale, 1);
        Clamp();
    }

    void Clamp()
    {
        var limit = PanLimit * content.localScale.x;
        var p = content.anchoredPosition;
        content.anchoredPosition = new Vector2(Mathf.Clamp(p.x, -limit, limit), Mathf.Clamp(p.y, -limit, limit));
    }

    Vector2 Center() => RectTransformUtility.WorldToScreenPoint(EventCamera(), viewport.position);

    float ScaleFactor() => GetComponentInParent<Canvas>().rootCanvas.scaleFactor;

    Camera EventCamera()
    {
        var canvas = GetComponentInParent<Canvas>().rootCanvas;
        return canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
    }
}
