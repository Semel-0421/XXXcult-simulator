using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 행동 결과(성공/실패)를 책 표지 패널로 보여 준다.
// 엔터·클릭·터치로 즉시 닫을 수 있다 (이벤트 결과창과 동일한 조작).
// 결정 6: 본문 칸 맨 위에 가로 띠로 띄우고, 떠 있는 동안 본문 위쪽을 띠 높이만큼 비워 질문 글자와 겹치지 않게 한다.
public class ActionResultView : MonoBehaviour
{
    [SerializeField] CanvasGroup group;
    [SerializeField] RawImage image;
    [SerializeField] TMP_Text label;
    [SerializeField] float duration = 1.6f;
    [SerializeField] float bandHeight = 300; // 가로 띠 높이 (캔버스 단위)
    const float BandGap = 16;                 // 띠와 본문 글자 사이

    QuestionPanel panel;
    bool reserved;

    bool playing;
    float shownAt;
    Action doneCallback;

    void Awake()
    {
        panel = FindAnyObjectByType<QuestionPanel>();
        BuildLayout();
        Hide();
    }

    // 씬의 288px 정사각 픽토그램 칸을 일지 책 표지 액자 + 가득 찬 일러스트 + 하단 글자 띠로 바꾼다 (자리는 PlaceBand)
    void BuildLayout()
    {
        var root = (RectTransform)transform;
        if (root.TryGetComponent<Image>(out var oldBackground)) oldBackground.enabled = false; // 씬의 기존 배경은 액자로 대체
        var frame = UiKit.Child(root, "Frame", typeof(Image));
        UiKit.Stretch(frame);
        frame.SetAsFirstSibling();
        UiSkin.ApplyBookFrame(frame.GetComponent<Image>(), "ActionBook", UiPalette.BookEventTop, UiPalette.BookEventBottom);
        frame.GetComponent<Image>().raycastTarget = false;

        var art = image.rectTransform;
        art.anchorMin = Vector2.zero;
        art.anchorMax = Vector2.one;
        art.offsetMin = new Vector2(20, 20);
        art.offsetMax = new Vector2(-20, -20);

        var shade = UiKit.Child(root, "Shade", typeof(Image));
        UiKit.Anchor(shade, Vector2.zero, new Vector2(1, 0), new Vector2(0, 20), new Vector2(-40, 84));
        shade.pivot = new Vector2(0.5f, 0);
        shade.GetComponent<Image>().color = new Color(0.03f, 0.01f, 0.06f, 0.72f);
        shade.GetComponent<Image>().raycastTarget = false;
        shade.SetSiblingIndex(label.transform.GetSiblingIndex()); // 글자 바로 아래

        var text = label.rectTransform;
        UiKit.Anchor(text, Vector2.zero, new Vector2(1, 0), new Vector2(0, 28), new Vector2(-48, 64));
        text.pivot = new Vector2(0.5f, 0);
        label.alignment = TextAlignmentOptions.Center;
        label.enableAutoSizing = false;
        label.fontSize = 40;
    }

    public void Play(string actionKey, bool success, Action done)
    {
        doneCallback = done;
        playing = true;
        shownAt = Time.unscaledTime;
        PlaceBand();
        var key = $"{actionKey}_{(success ? "ok" : "fail")}";
        var art = AdultArt.Load(key);
        var tex = art ?? (Texture)Resources.Load<Texture2D>($"Pictograms/{key}");
        image.texture = tex;
        FitArt(art != null);
        if (tex != null)
        {
            image.uvRect = art != null ? CoverCrop(tex) : new Rect(0, 0, 1, 1); // 일러스트는 띠 비율로 잘라 채우고, 픽토그램은 자르지 않는다
            image.color = Color.white;
        }
        else image.color = Color.clear;

        label.text = Lang.Get(success ? "action_success" : "action_fail");
        label.color = success ? UiPalette.Gold : new Color(0.9f, 0.35f, 0.35f);
        group.alpha = 1;
        group.blocksRaycasts = true;
        AudioManager.PlaySfx(actionKey == "Consecrate" ? "consecrate" : success ? "success" : "fail");
        StartCoroutine(AutoClose());
    }

    IEnumerator AutoClose()
    {
        yield return new WaitForSecondsRealtime(duration);
        if (playing) Skip();
    }

    void Update()
    {
        if (!playing) return;
        if (Menus.AnyOpen) return;
        if (Time.unscaledTime - shownAt < 0.05f) return; // 행동을 고른 그 클릭이 곧바로 스킵으로 겹치지 않게

        // 키보드: 엔터·스페이스
        var kb = Keyboard.current;
        if (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame))
        {
            Keybinds.Consume();
            Skip();
            return;
        }

        // 마우스 클릭
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Skip();
            return;
        }

        // 터치
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            Skip();
        }
    }

    void Skip()
    {
        if (!playing) return;
        playing = false;
        StopAllCoroutines();
        Hide();
        doneCallback?.Invoke();
        doneCallback = null;
    }

    // 일러스트는 액자 안을 가득 채우고, 정사각 픽토그램은 띠 높이에 맞춘 정사각으로 가운데 둔다 (가로 띠에 늘려 자르면 그림이 안 보인다)
    void FitArt(bool fill)
    {
        var art = image.rectTransform;
        if (fill)
        {
            art.anchorMin = Vector2.zero;
            art.anchorMax = Vector2.one;
            art.offsetMin = new Vector2(20, 20);
            art.offsetMax = new Vector2(-20, -20);
            return;
        }
        var side = Mathf.Max(0, ((RectTransform)transform).rect.height - 40);
        art.anchorMin = art.anchorMax = new Vector2(0.5f, 0.5f);
        art.sizeDelta = new Vector2(side, side);
        art.anchoredPosition = Vector2.zero;
    }

    // 칸(RawImage) 비율에 맞춰 그림을 가운데 기준으로 잘라 가득 채운다 (세로로 자를 때는 얼굴 쪽이 보이게 위를 남긴다)
    Rect CoverCrop(Texture tex)
    {
        var size = image.rectTransform.rect.size;
        float target = size.y <= 0 ? 1f : size.x / size.y;
        float texAspect = tex.width / (float)tex.height;
        if (texAspect > target) { float w = target / texAspect; return new Rect((1 - w) / 2, 0, w, 1); }
        float h = texAspect / target;
        return new Rect(0, (1 - h) * 0.7f, 1, h);
    }

    void Hide()
    {
        group.alpha = 0;
        group.blocksRaycasts = false;
        if (reserved && panel != null) panel.ReserveTop(0);
        reserved = false;
    }

    // 본문 글자 칸의 위쪽 가장자리에 맞춰 칸 너비만큼 가로 띠를 놓고, 본문은 그만큼 아래에서 시작하게 한다
    // (본문 칸 위치는 다른 창 작업이 바꿀 수 있어 띄울 때마다 다시 잰다)
    void PlaceBand()
    {
        if (panel == null) return; // 본문 창이 없으면 씬 자리 그대로
        var root = (RectTransform)transform;
        var parent = root.parent;
        var corners = new Vector3[4];
        panel.Body.GetWorldCorners(corners); // 0 왼아래, 1 왼위, 2 오른위, 3 오른아래
        var topLeft = parent.InverseTransformPoint(corners[1]);
        var topRight = parent.InverseTransformPoint(corners[2]);
        root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0.5f, 1);
        root.sizeDelta = new Vector2(topRight.x - topLeft.x, bandHeight);
        root.localPosition = (topLeft + topRight) / 2;

        // 띠 높이를 본문 글자 칸 단위로 바꿔 위쪽 여백으로 비운다
        var scale = panel.Body.lossyScale.y <= 0 ? 1 : parent.lossyScale.y / panel.Body.lossyScale.y;
        panel.ReserveTop((bandHeight + BandGap) * scale);
        reserved = true;
    }
}
