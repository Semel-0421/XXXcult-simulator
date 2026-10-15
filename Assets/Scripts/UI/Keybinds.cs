using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// 게임 진행 단축키. 행동 버튼 5개는 기본 1~5 키로 고를 수 있고, 옵션에서 다른 키로 바꿀 수 있다.
// 이벤트 선택지는 숫자 키(1~N, 키패드 포함)와 글자 클릭으로 고른다. 모두 키보드·마우스만으로 동작한다.
public static class Keybinds
{
    public const int ActionCount = 5;
    public const string ActionLinkPrefix = "act";
    static readonly Key[] Defaults = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5 };

    public static event Action Changed;

    public static int RebindingIndex { get; private set; } = -1; // 키를 기다리는 행동 번호 (-1이면 없음)
    public static bool Rebinding => RebindingIndex >= 0;

    static int consumedFrame = -1;

    // 진행 단축키를 받아도 되는가: 키 재설정 중이 아니고, 메뉴(옵션·저장 등)가 가리고 있지 않고,
    // 이번 프레임의 키 입력을 아직 아무도 쓰지 않았을 때
    public static bool Listening => !Rebinding && !Menus.AnyOpen && consumedFrame != Time.frameCount;

    // 키 입력 한 번이 같은 프레임에 두 번 쓰이지 않게 한다 (선택지를 고르자마자 바로 행동이 실행되는 일 방지)
    public static void Consume() => consumedFrame = Time.frameCount;

    static string Pref(int index) => "key_action_" + index;

    public static Key Get(int index) =>
        PlayerPrefs.HasKey(Pref(index)) ? (Key)PlayerPrefs.GetInt(Pref(index)) : Defaults[index];

    public static void Set(int index, Key key)
    {
        // 다른 행동이 같은 키를 쓰고 있으면 자리를 맞바꾼다
        for (int other = 0; other < ActionCount; other++)
            if (other != index && Get(other) == key) SetRaw(other, Get(index));
        SetRaw(index, key);
        PlayerPrefs.Save();
        Changed?.Invoke();
    }

    static void SetRaw(int index, Key key) => PlayerPrefs.SetInt(Pref(index), (int)key);

    public static void ResetAll()
    {
        for (int i = 0; i < ActionCount; i++) PlayerPrefs.DeleteKey(Pref(i));
        PlayerPrefs.Save();
        Changed?.Invoke();
    }

    public static void BeginRebind(int index)
    {
        RebindingIndex = index;
        Changed?.Invoke();
    }

    public static void CancelRebind()
    {
        RebindingIndex = -1;
        Changed?.Invoke();
    }

    public static string Label(Key key) => key switch
    {
        >= Key.Digit1 and <= Key.Digit9 => ((int)key - (int)Key.Digit1 + 1).ToString(),
        >= Key.Numpad1 and <= Key.Numpad9 => ((int)key - (int)Key.Numpad1 + 1).ToString(),
        _ => key.ToString(),
    };

    public static bool WasPressed(Key key) => Keyboard.current != null && Keyboard.current[key].wasPressedThisFrame;

    // 숫자 n을 윗줄 숫자 키나 키패드로 눌렀는가
    public static bool NumberPressed(int n) =>
        n >= 1 && n <= 9 && (WasPressed((Key)((int)Key.Digit1 + n - 1)) || WasPressed((Key)((int)Key.Numpad1 + n - 1)));

    // 이번 프레임에 눌린 첫 키 (없으면 null)
    public static Key? PressedThisFrame()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return null;
        foreach (var control in keyboard.allKeys)
            if (control.wasPressedThisFrame && control.keyCode != Key.None) return control.keyCode;
        return null;
    }

    // 행동 버튼 아래 안내에 쓰는 문자열 (예: "1 운동 · 2 명상 · ...")
    public static string Legend(IReadOnlyList<string> names)
    {
        var parts = new List<string>();
        // 글자를 눌러도 행동이 선택되도록 링크로 만든다 (QuestionPanel이 act0~act4를 처리)
        for (int i = 0; i < ActionCount && i < names.Count; i++)
            parts.Add($"<link=\"{ActionLinkPrefix}{i}\"><color=#F2D978><u>[{Label(Get(i))}] {names[i]}</u></color></link>");
        return string.Join("   ", parts);
    }
}
