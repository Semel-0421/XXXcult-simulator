using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 옵션 창: 언어, 행동 단축키, 화면 비율, 배경음악·효과음 음량. 타이틀과 게임 중 일시정지 메뉴가 함께 쓴다.
public static class OptionsWindow
{
    const float EdgeMargin = 16, ButtonHeight = 46, KeyWidth = 56, KeyGap = 4, ResetKeyWidth = 90;

    static GameObject root;
    static TMP_Text titleLabel, closeLabel, musicLabel, sfxLabel, languageLabel, musicValue, sfxValue;
    static TMP_Text keyLabel, resetKeysLabel, aspectLabel, aspectValue;
    static readonly TMP_Text[] keyButtonLabels = new TMP_Text[Keybinds.ActionCount];
    static Button korean, english, aspect, resetKeysButton;

    public static void Open()
    {
        if (root == null) Build();
        Refresh();
        if (!root.activeSelf) Menus.Opened();
        root.SetActive(true);
    }

    public static void Close()
    {
        if (root == null || !root.activeSelf) return;
        Menus.Closed();
        AudioManager.SaveVolumes();
        root.SetActive(false);
    }

    static void Build()
    {
        var font = UiKit.FindFont();
        var canvas = UiKit.CreateOverlayCanvas("OptionsCanvas", 310);
        var content = UiKit.BuildScrollWindow(canvas, font, "OptionsWindow", new Vector2(760, 650),
            out root, out titleLabel, out var hint, out var close);
        hint.gameObject.SetActive(false);
        closeLabel = close.GetComponentInChildren<TMP_Text>();
        close.onClick.AddListener(Close);

        // 언어
        var languageRow = Row(content, font, out languageLabel);
        english = RightButton(languageRow, font, "English", 0, 160, out var englishLabel);
        korean = RightButton(languageRow, font, "Korean", 160 + KeyGap * 2, 160, out var koreanLabel);
        englishLabel.text = Lang.Get("lang_en");
        koreanLabel.text = Lang.Get("lang_ko");
        korean.onClick.AddListener(() => { Lang.Set(true); Refresh(); });
        english.onClick.AddListener(() => { Lang.Set(false); Refresh(); });

        // 행동 단축키: 버튼을 누른 뒤 원하는 키를 누르면 바뀐다 (Esc 취소, 맨 오른쪽 "기본"으로 초기화)
        var keyRow = Row(content, font, out keyLabel);
        keyRow.GetComponent<LayoutElement>().preferredHeight = 96; // 재설정 중 안내가 두 번째 줄에 붙는다
        keyLabel.richText = true;
        resetKeysButton = RightButton(keyRow, font, "KeyReset", 0, ResetKeyWidth, out resetKeysLabel);
        resetKeysButton.onClick.AddListener(Keybinds.ResetAll);
        for (int i = 0; i < Keybinds.ActionCount; i++)
        {
            var index = i;
            var right = ResetKeyWidth + KeyGap * 2 + (Keybinds.ActionCount - 1 - i) * (KeyWidth + KeyGap);
            RightButton(keyRow, font, "Key" + i, right, KeyWidth, out keyButtonLabels[i])
                .onClick.AddListener(() => Keybinds.BeginRebind(index));
        }
        Keybinds.Changed -= Refresh; // 장면이 바뀌어 창을 다시 만들어도 한 번만 연결
        Keybinds.Changed += Refresh;

        // 화면 비율 (기기별로 UI가 겹칠 때 고른다)
        var aspectRow = Row(content, font, out aspectLabel);
        aspect = RightButton(aspectRow, font, "Aspect", 0, 200, out aspectValue);
        aspect.onClick.AddListener(() =>
        {
            AspectFit.Mode = AspectFit.Next(AspectFit.Mode);
            AspectFit.ApplyAll();
            Refresh();
        });

        // 배경음악
        var musicRow = Row(content, font, out musicLabel);
        var music = AddSlider(musicRow, font, AudioManager.MusicVolume, out musicValue);
        music.onValueChanged.AddListener(value => { AudioManager.MusicVolume = value; musicValue.text = Percent(value); });

        // 효과음 (손을 뗄 때 예시 소리)
        var sfxRow = Row(content, font, out sfxLabel);
        var sfx = AddSlider(sfxRow, font, AudioManager.SfxVolume, out sfxValue);
        sfx.onValueChanged.AddListener(value => { AudioManager.SfxVolume = value; sfxValue.text = Percent(value); });
        var trigger = sfx.gameObject.AddComponent<EventTrigger>();
        var release = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        release.callback.AddListener(_ => AudioManager.PlaySfx("click"));
        trigger.triggers.Add(release);

        root.SetActive(false);
    }

    // 행 오른쪽 끝에서 right만큼 떨어진 곳에 버튼을 놓는다 (가장자리 여백 포함)
    static Button RightButton(RectTransform row, TMP_FontAsset font, string name, float right, float width, out TMP_Text label)
    {
        var button = UiKit.MakeButton(row, font, name, "", width, ButtonHeight, out label);
        var rect = (RectTransform)button.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(1, 0.5f);
        rect.pivot = new Vector2(1, 0.5f);
        rect.anchoredPosition = new Vector2(-(EdgeMargin + right), 0);
        return button;
    }

    static RectTransform Row(RectTransform content, TMP_FontAsset font, out TMP_Text label)
    {
        var row = UiKit.Child(content, "Row", typeof(Image), typeof(LayoutElement));
        row.GetComponent<Image>().color = UiKit.RowColor;
        row.GetComponent<LayoutElement>().preferredHeight = 84;
        label = UiKit.Label(row, font, "Label", 26, TextAlignmentOptions.MidlineLeft, Color.white);
        UiKit.Anchor(label.rectTransform, new Vector2(0, 0), new Vector2(0, 1), new Vector2(150, 0), new Vector2(260, 0));
        return row;
    }

    static Slider AddSlider(RectTransform row, TMP_FontAsset font, float value, out TMP_Text valueLabel)
    {
        var slider = UiKit.MakeSlider(row, "Slider", value);
        var rect = (RectTransform)slider.transform;
        rect.anchorMin = new Vector2(0, 0.5f);
        rect.anchorMax = new Vector2(1, 0.5f);
        rect.offsetMin = new Vector2(300, -22);
        rect.offsetMax = new Vector2(-110, 22);

        valueLabel = UiKit.Label(row, font, "Value", 24, TextAlignmentOptions.MidlineRight, UiKit.Gold);
        UiKit.Anchor(valueLabel.rectTransform, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-56, 0), new Vector2(90, 0));
        valueLabel.text = Percent(value);
        return slider;
    }

    static string Percent(float value) => $"{Mathf.RoundToInt(value * 100)}%";

    static void Refresh()
    {
        if (root == null) return;
        titleLabel.text = Lang.Get("opt_title");
        closeLabel.text = Lang.Get("close");
        languageLabel.text = Lang.Get("opt_language");
        musicLabel.text = Lang.Get("opt_music");
        sfxLabel.text = Lang.Get("opt_sfx");
        // 현재 언어 버튼은 눌렀다는 표시로 비활성화한다
        korean.interactable = !Lang.IsKorean;
        english.interactable = Lang.IsKorean;
        keyLabel.text = Lang.Get("opt_keys") + (Keybinds.Rebinding ? $"\n<color=#F2D978>{Lang.Get("key_press")}</color>" : "");
        for (int i = 0; i < Keybinds.ActionCount; i++)
            keyButtonLabels[i].text = Keybinds.RebindingIndex == i ? "..." : Keybinds.Label(Keybinds.Get(i));
        resetKeysLabel.text = Lang.Get("key_reset");
        aspectLabel.text = Lang.Get("opt_aspect");
        aspectValue.text = Lang.Get("aspect_" + AspectFit.Mode.Replace(":", "_"));
        musicValue.text = Percent(AudioManager.MusicVolume);
        sfxValue.text = Percent(AudioManager.SfxVolume);
    }
}
