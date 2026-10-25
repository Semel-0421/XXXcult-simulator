using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 화면 어디를 눌러도(마우스·터치) 눌린 자리에 금빛 파문이 퍼졌다 사라진다. 게임 시작 시 스스로 만들어져 장면이 바뀌어도 남는다.
public class ClickEffect : MonoBehaviour
{
    const float Seconds = 0.45f, StartSize = 18f, EndSize = 110f;
    static readonly Color Ring = UiPalette.Gold; // UiSkin.Gold

    static Sprite ringSprite;
    RectTransform canvasRect;
    readonly List<Ripple> active = new();
    readonly Stack<Ripple> idle = new();

    class Ripple { public RectTransform rect; public Image image; public float age; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Create()
    {
        var go = new GameObject("ClickEffect");
        DontDestroyOnLoad(go);
        go.AddComponent<ClickEffect>();
    }

    void Awake()
    {
        var canvas = UiKit.CreateOverlayCanvas("ClickEffectCanvas", 999); // 모든 창 위에 그린다
        canvas.SetParent(transform, false);
        canvasRect = (RectTransform)canvas;
        Destroy(canvas.GetComponent<GraphicRaycaster>()); // 눌림을 가로채지 않는다
    }

    void Update()
    {
        if (TryGetPress(out var screen)) Spawn(screen);
        for (int i = active.Count - 1; i >= 0; i--)
        {
            var r = active[i];
            r.age += Time.unscaledDeltaTime;
            float t = r.age / Seconds;
            if (t >= 1f) { r.rect.gameObject.SetActive(false); idle.Push(r); active.RemoveAt(i); continue; }
            r.rect.sizeDelta = Vector2.one * Mathf.Lerp(StartSize, EndSize, 1f - (1f - t) * (1f - t)); // 빠르게 퍼지다 느려짐
            var c = Ring; c.a = 1f - t;
            r.image.color = c;
        }
    }

    static bool TryGetPress(out Vector2 screen)
    {
        screen = default;
        var touch = Touchscreen.current;
        if (touch != null && touch.primaryTouch.press.wasPressedThisFrame) { screen = touch.primaryTouch.position.ReadValue(); return true; }
        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame) { screen = mouse.position.ReadValue(); return true; }
        return false;
    }

    void Spawn(Vector2 screen)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out var local)) return;
        var r = idle.Count > 0 ? idle.Pop() : NewRipple();
        r.age = 0;
        r.rect.anchoredPosition = local;
        r.rect.sizeDelta = Vector2.one * StartSize;
        r.rect.gameObject.SetActive(true);
        active.Add(r);
    }

    Ripple NewRipple()
    {
        var rect = UiKit.Child(canvasRect, "Ripple", typeof(Image));
        rect.anchorMin = rect.anchorMax = Vector2.zero; // ScreenPointToLocal은 중심 기준 좌표를 주므로 중심 앵커로 맞춘다
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        var image = rect.GetComponent<Image>();
        image.sprite = RingSprite();
        image.raycastTarget = false;
        return new Ripple { rect = rect, image = image };
    }

    // 얇은 고리 하나를 코드로 그린다 (이미지 파일 없이)
    static Sprite RingSprite()
    {
        if (ringSprite != null) return ringSprite;
        const int S = 128;
        var px = new Color[S * S];
        float c = (S - 1) / 2f;
        for (int y = 0; y < S; y++)
        for (int x = 0; x < S; x++)
        {
            float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c; // 0 중심 ~ 1 가장자리
            float a = Mathf.Clamp01(1f - Mathf.Abs(d - 0.88f) / 0.1f);     // 반지름 0.88 둘레의 얇은 선
            px[y * S + x] = new Color(1f, 1f, 1f, a * a);
        }
        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        tex.SetPixels(px);
        tex.Apply();
        ringSprite = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100f);
        return ringSprite;
    }
}
