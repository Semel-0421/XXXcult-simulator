using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 옵션 창: 언어, 행동 단축키, 화면 비율, 성인 인증, 배경음악·효과음 음량. 타이틀과 게임 중 일시정지 메뉴가 함께 쓴다.
public static class OptionsWindow
{
    const float EdgeMargin = 16, ButtonHeight = 46, KeyWidth = 56, KeyGap = 4, ResetKeyWidth = 90;

    static GameObject root;
    static TMP_Text titleLabel, closeLabel, musicLabel, sfxLabel, languageLabel, musicValue, sfxValue;
    static TMP_Text keyLabel, resetKeysLabel, aspectLabel, aspectValue;
    static readonly TMP_Text[] keyButtonLabels = new TMP_Text[Keybinds.ActionCount];
    static TMP_Text windowLabel, windowValue, resultAutoLabel, resultAutoValue, adultAchLabel, adultAchValue;
    static Button adultAch;
    static TMP_Text masterLabel, masterValue, musicMuteLabel, musicMuteValue, sfxMuteLabel, sfxMuteValue, duckLabel, duckValue;
    static Button korean, english, aspect, window, resetKeysButton;
    static TMP_Text adultLabel, adultValue;
    static TMP_InputField[] birthInputs; // 연·월·일
    static string adultMessage = ""; // 인증 실패 안내 (두 번째 줄)

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

        // 창모드 / 전체화면 (F11과 같은 동작)
        var windowRow = Row(content, font, out windowLabel);
        window = RightButton(windowRow, font, "WindowMode", 0, 200, out windowValue);
        window.onClick.AddListener(WindowMode.Toggle);
        WindowMode.Changed -= Refresh;
        WindowMode.Changed += Refresh;

        // 선택 결과 창 자동 넘김 (켜면 잠시 뒤 저절로 닫힌다)
        var resultRow = Row(content, font, out resultAutoLabel);
        var resultAuto = RightButton(resultRow, font, "ResultAuto", 0, 200, out resultAutoValue);
        resultAuto.onClick.AddListener(() => { ResultWindow.AutoAdvance = !ResultWindow.AutoAdvance; Refresh(); });

        // 성인 인증: 생년월일을 넣고 인증하면 입력칸이 사라지고 성인 콘텐츠 켜기/끄기만 남는다
        var adultRow = Row(content, font, out adultLabel);
        adultRow.GetComponent<LayoutElement>().preferredHeight = 96;
        adultLabel.richText = true;
        var adult = RightButton(adultRow, font, "AdultVerify", 0, 120, out adultValue);
        adult.onClick.AddListener(OnAdultButton);
        birthInputs = new[]
        {
            BirthInput(adultRow, font, "BirthYear", 120 + KeyGap * 2 + 2 * (64 + KeyGap), 90, 4, "YYYY"),
            BirthInput(adultRow, font, "BirthMonth", 120 + KeyGap * 2 + 64 + KeyGap, 64, 2, "MM"),
            BirthInput(adultRow, font, "BirthDay", 120 + KeyGap * 2, 64, 2, "DD"),
        };

        // 성인 업적: 성인 콘텐츠를 켠 사람만 따로 켤 수 있다
        var adultAchRow = Row(content, font, out adultAchLabel);
        adultAch = RightButton(adultAchRow, font, "AdultAchievements", 0, 200, out adultAchValue);
        adultAch.onClick.AddListener(() => { AdultGate.AchievementsEnabled = !AdultGate.AchievementsEnabled; Refresh(); });

        // 전체 음량
        var masterRow = Row(content, font, out masterLabel);
        var master = AddSlider(masterRow, font, AudioManager.MasterVolume, out masterValue);
        master.onValueChanged.AddListener(value => { AudioManager.MasterVolume = value; masterValue.text = Percent(value); });

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

        // 소리 켜기/끄기와 메뉴 덕킹
        ToggleRow(content, font, "MusicMute", out musicMuteLabel, out musicMuteValue, () => AudioManager.MusicMuted = !AudioManager.MusicMuted);
        ToggleRow(content, font, "SfxMute", out sfxMuteLabel, out sfxMuteValue, () => AudioManager.SfxMuted = !AudioManager.SfxMuted);
        ToggleRow(content, font, "DuckOnMenu", out duckLabel, out duckValue, () => AudioManager.DuckOnMenu = !AudioManager.DuckOnMenu);

        root.SetActive(false);
    }

    static void ToggleRow(RectTransform content, TMP_FontAsset font, string name, out TMP_Text label, out TMP_Text value, System.Action toggle)
    {
        var row = Row(content, font, out label);
        var button = RightButton(row, font, name, 0, 200, out value);
        button.onClick.AddListener(() => { toggle(); AudioManager.SaveVolumes(); Refresh(); });
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

    static TMP_InputField BirthInput(RectTransform row, TMP_FontAsset font, string name, float right, float width, int limit, string placeholder)
    {
        var go = TMP_DefaultControls.CreateInputField(new TMP_DefaultControls.Resources());
        go.name = name;
        var rect = (RectTransform)go.transform;
        rect.SetParent(row, false);
        rect.anchorMin = rect.anchorMax = new Vector2(1, 0.5f);
        rect.pivot = new Vector2(1, 0.5f);
        rect.sizeDelta = new Vector2(width, ButtonHeight);
        rect.anchoredPosition = new Vector2(-(EdgeMargin + right), 0);
        var input = go.GetComponent<TMP_InputField>();
        input.contentType = TMP_InputField.ContentType.IntegerNumber;
        input.characterLimit = limit;
        input.image.color = new Color(0.08f, 0.05f, 0.12f);
        input.textComponent.color = Color.white;
        input.textComponent.fontSize = 22;
        input.textComponent.alignment = TextAlignmentOptions.Center;
        var hint = (TMP_Text)input.placeholder;
        hint.text = placeholder;
        hint.fontSize = 22;
        hint.color = UiKit.Muted;
        hint.alignment = TextAlignmentOptions.Center;
        if (font != null) input.fontAsset = font;
        return input;
    }

    // 인증 전: 생년월일 확인 / 인증 후: 성인 콘텐츠 켜기·끄기
    static void OnAdultButton()
    {
        if (AdultGate.Verified)
        {
            AdultGate.ContentEnabled = !AdultGate.ContentEnabled;
        }
        else
        {
            var ok = int.TryParse(birthInputs[0].text, out var year) & int.TryParse(birthInputs[1].text, out var month)
                & int.TryParse(birthInputs[2].text, out var day);
            var result = ok ? AdultGate.Verify(year, month, day) : AdultGate.Result.Invalid;
            adultMessage = result switch
            {
                AdultGate.Result.Minor => Lang.Get("adult_minor"),
                AdultGate.Result.Invalid => Lang.Get("adult_invalid"),
                _ => "",
            };
            foreach (var input in birthInputs) input.text = ""; // 생년월일은 남기지 않는다
        }
        Refresh();
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
        windowLabel.text = Lang.Get("opt_window");
        windowValue.text = Lang.Get(WindowMode.Windowed ? "window_on" : "window_off");
        window.interactable = WindowMode.Supported;
        resultAutoLabel.text = Lang.Get("opt_result_auto");
        resultAutoValue.text = Lang.Get(ResultWindow.AutoAdvance ? "adult_on" : "adult_off");
        var verified = AdultGate.Verified;
        adultLabel.text = Lang.Get(verified ? "opt_adult_content" : "opt_adult_verify")
            + (!verified && adultMessage != "" ? $"\n<size=70%><color=#F2D978>{adultMessage}</color></size>" : "");
        adultValue.text = verified ? Lang.Get(AdultGate.ContentEnabled ? "adult_on" : "adult_off") : Lang.Get("adult_check");
        foreach (var input in birthInputs) input.gameObject.SetActive(!verified);
        adultAchLabel.text = Lang.Get("opt_adult_ach");
        adultAchValue.text = Lang.Get(AdultGate.AchievementsEnabled ? "adult_on" : "adult_off");
        adultAch.interactable = AdultGate.ContentEnabled;
        masterLabel.text = Lang.Get("opt_master");
        masterValue.text = Percent(AudioManager.MasterVolume);
        musicMuteLabel.text = Lang.Get("opt_music_mute");
        musicMuteValue.text = Lang.Get(AudioManager.MusicMuted ? "adult_off" : "adult_on");
        sfxMuteLabel.text = Lang.Get("opt_sfx_mute");
        sfxMuteValue.text = Lang.Get(AudioManager.SfxMuted ? "adult_off" : "adult_on");
        duckLabel.text = Lang.Get("opt_duck");
        duckValue.text = Lang.Get(AudioManager.DuckOnMenu ? "adult_on" : "adult_off");
        musicValue.text = Percent(AudioManager.MusicVolume);
        sfxValue.text = Percent(AudioManager.SfxVolume);
    }
}
