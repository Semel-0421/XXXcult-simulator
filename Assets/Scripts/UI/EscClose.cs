using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Esc로 맨 위에 열린 창을 닫는다. 창의 닫기 버튼을 그대로 누르므로 닫을 때 하는 일은 창마다 같다.
// 단축키를 바꾸는 중의 Esc는 재설정 취소에 쓰므로 건드리지 않는다.
public class EscClose : MonoBehaviour
{
    static readonly List<EscClose> open = new();
    static int handledFrame = -1;
    Button close;

    public static void Attach(GameObject window, Button closeButton)
    {
        var esc = window.GetComponent<EscClose>();
        if (esc == null) esc = window.AddComponent<EscClose>();
        esc.close = closeButton;
    }

    void OnEnable() { open.Remove(this); open.Add(this); }
    void OnDisable() => open.Remove(this);

    void Update()
    {
        if (open.Count == 0 || open[^1] != this || handledFrame == Time.frameCount) return;
        if (Keybinds.Rebinding || Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
        handledFrame = Time.frameCount; // 한 번 누르면 창 하나만 닫는다
        if (close != null && close.interactable) close.onClick.Invoke();
    }
}
