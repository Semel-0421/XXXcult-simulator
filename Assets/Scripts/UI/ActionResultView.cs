using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 행동 결과(성공/실패)를 책 표지 패널로 보여 준다.
// 엔터·클릭·터치로 즉시 닫을 수 있다 (이벤트 결과창과 동일한 조작).
public class ActionResultView : MonoBehaviour
{
    [SerializeField] CanvasGroup group;
    [SerializeField] RawImage image;
    [SerializeField] TMP_Text label;
    [SerializeField] float duration = 1.6f;

    bool playing;
    float shownAt;
    Action doneCallback;

    void Awake()
    {
        BuildLayout();
        Hide();
    }

    // 씬의 288px 정사각 픽토그램 칸을 일지 책 표지 액자 + 가득 찬 일러스트 + 하단 글자 띠로 바꾼다
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
        var key = $"{actionKey}_{(success ? "ok" : "fail")}";
        var tex = AdultArt.Load(key) ?? (Texture)Resources.Load<Texture2D>($"Pictograms/{key}");
        image.texture = tex;
        if (tex != null)
        {
            image.uvRect = CoverCrop(tex); // 세로 일러스트를 칸 비율에 맞춰 왜곡 없이 채운다
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
    }
}
