using UnityEngine;
using UnityEngine.InputSystem;

// 창모드 / 전체화면 전환. 옵션 창 버튼이나 F11 키로 바꾸며, 고른 설정은 기억한다.
// 데스크톱(Windows·macOS·Linux)에서만 적용되고 모바일에서는 아무것도 하지 않는다.
public class WindowMode : MonoBehaviour
{
    const string Pref = "window_mode_windowed";
    const int WindowedWidth = 1280, WindowedHeight = 720;

    public static bool Supported =>
        Application.platform is RuntimePlatform.WindowsPlayer or RuntimePlatform.OSXPlayer or RuntimePlatform.LinuxPlayer
        or RuntimePlatform.WindowsEditor or RuntimePlatform.OSXEditor or RuntimePlatform.LinuxEditor;

    public static bool Windowed
    {
        get => PlayerPrefs.GetInt(Pref, 0) == 1;
        private set { PlayerPrefs.SetInt(Pref, value ? 1 : 0); PlayerPrefs.Save(); }
    }

    public static event System.Action Changed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        var host = new GameObject("WindowModeHost");
        DontDestroyOnLoad(host);
        host.AddComponent<WindowMode>();
        Apply();
    }

    public static void Toggle()
    {
        Windowed = !Windowed;
        Apply();
        Changed?.Invoke();
    }

    static void Apply()
    {
        if (!Supported) return;
        if (Windowed)
        {
            Screen.SetResolution(WindowedWidth, WindowedHeight, FullScreenMode.Windowed);
        }
        else
        {
            var native = Screen.currentResolution;
            Screen.SetResolution(native.width, native.height, FullScreenMode.FullScreenWindow);
        }
    }

    // F11: 어느 장면에서든 창모드를 토글한다
    void Update()
    {
        if (Supported && Keyboard.current != null && Keyboard.current[Key.F11].wasPressedThisFrame) Toggle();
    }
}
