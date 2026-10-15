using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 캔버스의 UI를 담는 비율 프레임. 캔버스 크기가 바뀌면(창 크기 조절·화면 회전) 스스로 다시 맞춘다.
public class AspectFrame : MonoBehaviour
{
    static readonly List<AspectFrame> Active = new();

    Vector2 fittedFor;

    RectTransform Frame => (RectTransform)transform;
    RectTransform CanvasRect => (RectTransform)transform.parent;

    // 캔버스의 기존 자식들을 새 프레임 아래로 옮긴다
    public static AspectFrame Wrap(RectTransform canvas)
    {
        var rect = new GameObject("AspectFrame", typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(canvas, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        // 원래 그리는 순서(뒤의 자식이 위)를 지키려고 앞에서부터 옮기고,
        // 로컬 값을 유지해 크기·앵커가 프레임 기준으로 다시 맞춰지게 한다
        var children = new List<Transform>();
        foreach (Transform child in canvas)
            if (child != rect) children.Add(child);
        foreach (var child in children) child.SetParent(rect, false);

        var aspect = rect.gameObject.AddComponent<AspectFrame>();
        aspect.Fit();
        return aspect;
    }

    public static void FitAll()
    {
        foreach (var aspect in Active) aspect.Fit();
    }

    void OnEnable() => Active.Add(this);
    void OnDisable() => Active.Remove(this);

    void LateUpdate()
    {
        if (CanvasRect.rect.size != fittedFor) Fit();
    }

    // 캔버스 배율 설정(CanvasScaler)과 무관하게 항상 설계 해상도 기준으로 배치한 뒤 크기만 맞춘다
    public void Fit()
    {
        var size = CanvasRect.rect.size;
        fittedFor = size;
        if (size.x <= 0 || size.y <= 0) return;
        var frameSize = AspectFit.FrameSize(DesignSize, AspectFit.Ratio(AspectFit.Mode));
        Frame.sizeDelta = frameSize;
        Frame.localScale = Vector3.one * AspectFit.FitScale(size, frameSize);
        Frame.anchoredPosition = Vector2.zero;
    }

    // 장면 UI를 설계한 해상도 (캔버스의 기준 해상도, 없으면 1920×1080)
    Vector2 DesignSize
    {
        get
        {
            var scaler = CanvasRect.GetComponent<CanvasScaler>();
            return scaler != null && scaler.referenceResolution.x > 0 && scaler.referenceResolution.y > 0
                ? scaler.referenceResolution
                : new Vector2(1920, 1080);
        }
    }
}
