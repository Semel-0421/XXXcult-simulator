using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 확인/취소 두 버튼짜리 작은 창. 코드로 만들어 띄우고, 고르면 스스로 사라진다.
public static class ConfirmDialog
{
    public static void Show(string message, string okText, string cancelText, Action onOk)
    {
        var font = UiKit.FindFont();
        var canvas = UiKit.CreateOverlayCanvas("ConfirmDialog", 400);
        var dim = UiKit.Child(canvas, "Dim", typeof(Image));
        UiKit.Stretch(dim);
        dim.GetComponent<Image>().color = new Color(0, 0, 0, 0.75f);

        var panel = UiKit.Child(canvas, "Panel", typeof(Image));
        UiKit.Anchor(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620, 300));
        UiSkin.ApplyBookFrame(panel.GetComponent<Image>(), "ConfirmBook", new Color(0.30f, 0.10f, 0.22f), new Color(0.14f, 0.05f, 0.20f));

        var text = UiKit.Label(panel, font, "Message", 24, TextAlignmentOptions.Center, Color.white);
        UiKit.Anchor(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(0, 30), new Vector2(-70, -110));
        text.text = message;

        Menus.Opened();
        void Close() { Menus.Closed(); UnityEngine.Object.Destroy(canvas.gameObject); }
        var ok = UiKit.MakeButton(panel, font, "Ok", okText, 220, 52, out _);
        UiKit.Anchor((RectTransform)ok.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-120, 50), new Vector2(220, 52));
        ok.image.color = UiSkin.Crimson;
        ok.onClick.AddListener(() => { Close(); onOk?.Invoke(); });
        var cancel = UiKit.MakeButton(panel, font, "Cancel", cancelText, 220, 52, out _);
        UiKit.Anchor((RectTransform)cancel.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(120, 50), new Vector2(220, 52));
        cancel.onClick.AddListener(Close);
    }
}
