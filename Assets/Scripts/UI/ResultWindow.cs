using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 선택지를 고른 뒤 결과를 보여 주는 창. 이벤트 글 창과 같은 책 표지 모양으로 띄운다.
//   (성공) / (실패) + 결과 글, 아래에 실제로 바뀐 스탯을 "신도 +3 / 음욕 +2"처럼 슬래시로 나눠 적는다.
// 확인 버튼·엔터·스페이스·1번 키로 닫으면 다음 진행(onClose)으로 넘어간다.
public class ResultWindow : MonoBehaviour
{
    const string SuccessColor = "#D4AF37", FailColor = "#C04040", UpColor = "#D4AF37", DownColor = "#B06070";

    public const float AutoSeconds = 2f;
    public static bool IsOpen => current != null;
    // 옵션: 켜면 AutoSeconds 뒤 저절로 닫힌다 (누르면 바로 닫힘)
    public static bool AutoAdvance
    {
        get => PlayerPrefs.GetInt("result_auto", 0) == 1;
        set { PlayerPrefs.SetInt("result_auto", value ? 1 : 0); PlayerPrefs.Save(); }
    }
    static ResultWindow current;
    Action onClose;
    float shown;

    // 바뀐 스탯 한 줄. 교단 스탯 → 개인 스탯 → 신 모드 효과 → 기프티드 순
    public static string EffectsLine(IEnumerable<(string label, int amount)> changes, IEnumerable<string> extras = null)
    {
        var parts = changes.Where(c => c.amount != 0)
            .Select(c => $"<color={(c.amount > 0 ? UpColor : DownColor)}>{c.label} {(c.amount > 0 ? "+" : "")}{c.amount}</color>")
            .Concat(extras ?? Enumerable.Empty<string>());
        return string.Join("  /  ", parts);
    }

    public static string Body(bool success, string text) =>
        $"<color={(success ? SuccessColor : FailColor)}>({Lang.Get(success ? "result_success" : "result_fail")})</color> {text}";

    public static void Show(bool success, string text, string effects, Action onClose)
    {
        if (current != null) current.Close(invoke: false);
        var font = UiKit.FindFont();
        var canvas = UiKit.CreateOverlayCanvas("ResultWindow", 250); // 메뉴(300대)·확인창(400)보다 아래
        var dim = UiKit.Child(canvas, "Dim", typeof(Image));
        UiKit.Stretch(dim);
        dim.GetComponent<Image>().color = new Color(0, 0, 0, 0.55f);

        var panel = UiKit.Child(canvas, "Panel", typeof(Image));
        UiKit.Anchor(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-185, 15), new Vector2(830, 420));
        UiSkin.ApplyBookFrame(panel.GetComponent<Image>(), "ResultBook", UiPalette.BookEventTop, UiPalette.BookEventBottom);

        var body = UiKit.Label(panel, font, "Body", 26, TextAlignmentOptions.TopLeft, Color.white);
        UiKit.Anchor(body.rectTransform, Vector2.zero, Vector2.one, new Vector2(0, 45), new Vector2(-80, -170));
        body.text = Body(success, text);
        body.enableAutoSizing = true;
        body.fontSizeMin = 16;
        body.fontSizeMax = 26;

        var line = UiKit.Label(panel, font, "Effects", 21, TextAlignmentOptions.Center, Color.white);
        UiKit.Anchor(line.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 108), new Vector2(-80, 40));
        line.text = string.IsNullOrEmpty(effects) ? Lang.Get("result_no_change") : effects;
        line.enableAutoSizing = true;
        line.fontSizeMin = 14;
        line.fontSizeMax = 21;

        var window = canvas.gameObject.AddComponent<ResultWindow>();
        window.onClose = onClose;
        current = window;
        var ok = UiKit.MakeButton(panel, font, "Ok", Lang.Get("confirm"), 220, 52, out _);
        UiKit.Anchor((RectTransform)ok.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 50), new Vector2(220, 52));
        ok.image.color = new Color(0.42f, 0.08f, 0.12f); // 어두운 진홍
        ok.onClick.AddListener(() => window.Close(invoke: true));
    }

    void Update()
    {
        if (Menus.AnyOpen) return;
        shown += Time.unscaledDeltaTime;
        if (AutoAdvance && shown >= AutoSeconds) { Close(invoke: true); return; }
        // 화면 어디를 눌러도 바로 넘어간다 (마우스·터치). 열린 직후 프레임은 이전 클릭이 겹치지 않게 건너뛴다.
        if (shown > 0.05f && (Mouse.current?.leftButton.wasPressedThisFrame == true
            || Touchscreen.current?.primaryTouch.press.wasPressedThisFrame == true))
        {
            Close(invoke: true);
            return;
        }
        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame
            || kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame)
        {
            Keybinds.Consume(); // 같은 키가 이어서 행동 단축키로 쓰이지 않게
            Close(invoke: true);
        }
    }

    void Close(bool invoke)
    {
        if (current == this) current = null;
        var next = onClose;
        onClose = null;
        Destroy(gameObject);
        if (invoke) next?.Invoke();
    }

    void OnDestroy() { if (current == this) current = null; }
}
