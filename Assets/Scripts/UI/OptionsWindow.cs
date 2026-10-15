using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 옵션 창: 언어, 배경음악·효과음 음량 슬라이더. 타이틀과 게임 중 일시정지 메뉴가 함께 쓴다.
public static class OptionsWindow
{
    static GameObject root;
    static TMP_Text titleLabel, closeLabel, musicLabel, sfxLabel, languageLabel, musicValue, sfxValue;
    static Button korean, english;

    public static void Open()
    {
        if (root == null) Build();
        Refresh();
        if (!root.activeSelf) AudioManager.MenuOpened();
        root.SetActive(true);
    }

    public static void Close()
    {
        if (root == null || !root.activeSelf) return;
        AudioManager.MenuClosed();
        AudioManager.SaveVolumes();
        root.SetActive(false);
    }

    static void Build()
    {
        var font = UiKit.FindFont();
        var canvas = UiKit.CreateOverlayCanvas("OptionsCanvas", 310);
        var content = UiKit.BuildScrollWindow(canvas, font, "OptionsWindow", new Vector2(760, 440),
            out root, out titleLabel, out var hint, out var close);
        hint.gameObject.SetActive(false);
        closeLabel = close.GetComponentInChildren<TMP_Text>();
        close.onClick.AddListener(Close);

        // 언어
        var languageRow = Row(content, font, out languageLabel);
        korean = UiKit.MakeButton(languageRow, font, "Korean", Lang.Get("lang_ko"), 160, 48, out _);
        english = UiKit.MakeButton(languageRow, font, "English", Lang.Get("lang_en"), 160, 48, out _);
        UiKit.Anchor((RectTransform)korean.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-270, 0), new Vector2(160, 48));
        UiKit.Anchor((RectTransform)english.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-96, 0), new Vector2(160, 48));
        korean.onClick.AddListener(() => { Lang.Set(true); Refresh(); });
        english.onClick.AddListener(() => { Lang.Set(false); Refresh(); });

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
        titleLabel.text = Lang.Get("opt_title");
        closeLabel.text = Lang.Get("close");
        languageLabel.text = Lang.Get("opt_language");
        musicLabel.text = Lang.Get("opt_music");
        sfxLabel.text = Lang.Get("opt_sfx");
        // 현재 언어 버튼은 눌렀다는 표시로 비활성화한다
        korean.interactable = !Lang.IsKorean;
        english.interactable = Lang.IsKorean;
        musicValue.text = Percent(AudioManager.MusicVolume);
        sfxValue.text = Percent(AudioManager.SfxVolume);
    }
}
