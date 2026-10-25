using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StartScreenController : MonoBehaviour
{
    [SerializeField] CanvasGroup[] menuButtons;
    [SerializeField] GameObject settingsPanel;
    [SerializeField] SaveSlotWindow saveWindow;
    [SerializeField] float fadeDuration = 0.3f;

    Button godButton;
    TMP_Text godLabel;

    IEnumerator Start()
    {
        settingsPanel.SetActive(false);
        var buttons = new System.Collections.Generic.List<CanvasGroup>(menuButtons);
        buttons.Insert(1, BuildGodButton());
        buttons.Insert(2, BuildMirrorButton());
        buttons.Insert(3, BuildEyeButton());
        ImageLabels(); // 복제 뒤에 (복제본은 새 게임의 TMP 글자에서 시작한다)
        foreach (var button in buttons) { button.alpha = 0; button.interactable = false; }
        yield return null; // 화면 틀 크기가 정해진 뒤에 잰다 (Start 첫 프레임에는 아직 0)
        FitMenu(buttons);
        foreach (var button in buttons)
        {
            yield return CanvasFade.To(button, 1, fadeDuration);
            button.interactable = true;
        }
    }

    Button mirrorButton;
    TMP_Text mirrorLabel;

    // 모드 버튼이 늘어 메뉴가 화면 아래로 넘치면 버튼 높이와 간격을 같은 비율로 줄인다 (8개부터 '종료'가 잘렸다)
    static void FitMenu(System.Collections.Generic.List<CanvasGroup> buttons)
    {
        var menu = (RectTransform)buttons[0].transform.parent;
        var frame = menu.parent as RectTransform;
        var layout = menu.GetComponent<VerticalLayoutGroup>();
        if (frame == null || layout == null) return;
        var menuTop = menu.localPosition.y + menu.rect.yMax; // 화면 틀 좌표에서 메뉴 윗변
        var available = menuTop - frame.rect.yMin - 16;
        var height = ((RectTransform)buttons[0].transform).rect.height;
        var needed = buttons.Count * height + (buttons.Count - 1) * layout.spacing;
        if (needed <= available || needed <= 0) return;
        var scale = available / needed;
        layout.spacing *= scale;
        foreach (var button in buttons)
        {
            var rect = (RectTransform)button.transform;
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, height * scale);
            if (button.TryGetComponent<LayoutElement>(out var element)) element.minHeight = element.preferredHeight = height * scale; // 세로 배치가 이 높이를 쓴다
        }
    }


    // 새 게임 버튼을 복제해 그 아래 offset번째 자리에 둔다
    CanvasGroup CloneNewGame(string name, int offset, UnityEngine.Events.UnityAction onClick, out Button button, out TMP_Text label)
    {
        var source = menuButtons[0]; // 새 게임
        var copy = Instantiate(source.gameObject, source.transform.parent);
        copy.name = name;
        copy.transform.SetSiblingIndex(source.transform.GetSiblingIndex() + offset);
        foreach (var localized in copy.GetComponentsInChildren<LocalizedText>(true)) Destroy(localized);
        label = copy.GetComponentInChildren<TMP_Text>(true);
        button = copy.GetComponentInChildren<Button>(true);
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(onClick);
        return copy.GetComponent<CanvasGroup>();
    }

    // 준비된 신: 새 게임 버튼 바로 아래. 사망·해체를 뺀 엔딩 5개를 보기 전에는 취소선과 함께 잠겨 있다.
    CanvasGroup BuildGodButton()
    {
        var group = CloneNewGame("GodModeButton", 1, NewGodGame, out godButton, out godLabel);
        godButton.enabled = GodRules.Unlocked;
        LockableLabel(godLabel, "god_mode");
        return group;
    }

    // 거울 세계: 준비된 신 아래. 해금 조건이 준비된 신과 같고, 글자 그림이 좌우 반전돼 있다.
    CanvasGroup BuildMirrorButton()
    {
        var group = CloneNewGame("MirrorModeButton", 2, NewMirrorGame, out mirrorButton, out mirrorLabel);
        mirrorButton.enabled = GodRules.Unlocked;
        LockableLabel(mirrorLabel, "mirror_mode");
        return group;
    }

    static void LockableLabel(TMP_Text label, string key) =>
        MenuLabel.Replace(label, key, () => !GodRules.Unlocked, () => GodRules.LockedFill(GodRules.UnlockProgress), () => GodRules.LockedCount(GodRules.UnlockProgress));

    Button eyeButton;

    // 황금의 눈동자: 거울 세계 아래. 신 승천 엔딩 + 거울 전용 엔딩을 보기 전에는 잠겨 있다.
    // 잠긴 글자는 거울처럼 좌우 반전된 채 금빛이 차오르고, 열리면 정방향 금빛 글자가 된다.
    CanvasGroup BuildEyeButton()
    {
        var group = CloneNewGame("EyeModeButton", 3, NewEyeGame, out eyeButton, out var label);
        eyeButton.enabled = EyeRules.Unlocked;
        MenuLabel.Replace(label, "eye_mode", () => !EyeRules.Unlocked, () => EyeRules.LockedFill(EyeRules.UnlockProgress), () => EyeRules.LockedCount(EyeRules.UnlockProgress));
        return group;
    }

    // 판 진행은 아직 없어서 모드 설명과 준비 중 안내만 보여 준다 (SYS_DEMIURGE 구현 순서 2단계부터)
    public void NewEyeGame()
    {
        if (!EyeRules.Unlocked) return;
        ConfirmDialog.Show($"<b>{Lang.Get("eye_mode")}</b>\n\n{Lang.Get("eye_desc")}\n\n<size=80%>{Lang.Get("eye_wip")}</size>", Lang.Get("close"), null, null);
    }

    // 장면에 있는 메뉴 버튼 글자(LocalizedText)도 그림으로 바꾼다
    void ImageLabels()
    {
        foreach (var group in menuButtons)
            foreach (var localized in group.GetComponentsInChildren<LocalizedText>(true))
            {
                var key = localized.Key;
                var label = localized.GetComponent<TMP_Text>();
                Destroy(localized);
                MenuLabel.Replace(label, key);
            }
    }

    // 모드 설명을 보여 주고, 들어가면 새 판을 거울 세계로 시작한다 (회차·시작 보상은 새 게임과 같다)
    public void NewMirrorGame()
    {
        if (!GodRules.Unlocked) return;
        ConfirmDialog.Show($"<b>{Lang.Get("mirror_mode")}</b>\n\n{Lang.Get("mirror_desc")}", Lang.Get("mirror_start"), Lang.Get("close"), () =>
        {
            GameSession.StartNew();
            GameSession.SetMirrorMode(true);
            Progress.BeginRun(Progress.NextRun);
            Profile.Count("runs");
            Achievements.ApplyStartRewards(GameSession.Personal);
            UnityEngine.SceneManagement.SceneManager.LoadScene("GameMain");
        });
    }

    const string AdultPref = "god_adult_ok";

    public void NewGodGame()
    {
        if (!GodRules.Unlocked) return;
        // 처음 들어갈 때 한 번 성인 확인 (이 모드는 관능·퇴폐 묘사가 있다)
        if (PlayerPrefs.GetInt(AdultPref, 0) != 1 && !AdultGate.Verified) // 옵션에서 성인 인증을 마쳤으면 묻지 않는다
        {
            ConfirmDialog.Show(Lang.Get("god_adult_confirm"), Lang.Get("god_adult_ok"), Lang.Get("close"), () =>
            {
                PlayerPrefs.SetInt(AdultPref, 1);
                PlayerPrefs.Save();
                NewGodGame();
            });
            return;
        }
        GodRules.Begin();
        Profile.Count("runs");
        UnityEngine.SceneManagement.SceneManager.LoadScene("GameMain");
    }

    public void OpenSaves() => saveWindow.Open(false);
    public void OpenAchievements() => AchievementWindow.Open();
    // 옵션은 타이틀과 게임 중이 같은 창(언어·음량)을 쓴다
    public void OpenSettings() => OptionsWindow.Open();

    public void CloseSettings()
    {
        if (settingsPanel.activeSelf) Menus.Closed();
        settingsPanel.SetActive(false);
    }
    public void SetEnglish() => Lang.Set(false);
    public void SetKorean() => Lang.Set(true);

    public void NewGame()
    {
        GameSession.StartNew();
        Progress.BeginRun(Progress.NextRun); // 회차 잠금·시작 보너스
        Profile.Count("runs");
        Achievements.ApplyStartRewards(GameSession.Personal);
        UnityEngine.SceneManagement.SceneManager.LoadScene("GameMain");
    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
