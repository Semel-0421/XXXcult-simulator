using UnityEngine;
using UnityEngine.InputSystem;

// 단축키 재설정 중 키 입력을 받는 전역 감시자. 장면이 바뀌어도 살아 있다.
public class KeybindDriver : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Create()
    {
        var go = new GameObject("KeybindDriver");
        DontDestroyOnLoad(go);
        go.AddComponent<KeybindDriver>();
    }

    void Update()
    {
        if (!Keybinds.Rebinding) return;
        var key = Keybinds.PressedThisFrame();
        if (key == null) return;
        Keybinds.Consume(); // 새로 정한 키가 곧바로 행동을 실행하지 않도록
        if (key == Key.Escape) Keybinds.CancelRebind();
        else
        {
            Keybinds.Set(Keybinds.RebindingIndex, key.Value);
            Keybinds.CancelRebind();
        }
    }
}
