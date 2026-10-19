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
        foreach (var button in buttons) { button.alpha = 0; button.interactable = false; }
        foreach (var button in buttons)
        {
            yield return CanvasFade.To(button, 1, fadeDuration);
            button.interactable = true;
        }
    }

    Button mirrorButton;
    TMP_Text mirrorLabel;

    void OnDestroy() { Lang.Changed -= RefreshGodButton; Lang.Changed -= RefreshMirrorButton; }

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
        Lang.Changed += RefreshGodButton;
        RefreshGodButton();
        return group;
    }

    void RefreshGodButton()
    {
        var unlocked = GodRules.Unlocked;
        godButton.enabled = unlocked;
        godLabel.text = unlocked ? Lang.Get("god_mode") : GodRules.LockedTitle(Lang.Get("god_mode"), GodRules.UnlockProgress);
    }

    // 거울 세계: 준비된 신 아래. 해금 조건이 준비된 신과 같고, 글자를 좌우 반전해 보여 준다.
    CanvasGroup BuildMirrorButton()
    {
        var group = CloneNewGame("MirrorModeButton", 2, NewMirrorGame, out mirrorButton, out mirrorLabel);
        var scale = mirrorLabel.rectTransform.localScale;
        mirrorLabel.rectTransform.localScale = new Vector3(-Mathf.Abs(scale.x), scale.y, scale.z);
        Lang.Changed += RefreshMirrorButton;
        RefreshMirrorButton();
        return group;
    }

    void RefreshMirrorButton()
    {
        var unlocked = GodRules.Unlocked;
        mirrorButton.enabled = unlocked;
        mirrorLabel.text = unlocked ? Lang.Get("mirror_mode") : GodRules.LockedTitle(Lang.Get("mirror_mode"), GodRules.UnlockProgress);
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
