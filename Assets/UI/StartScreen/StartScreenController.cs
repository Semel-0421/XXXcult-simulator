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
        foreach (var button in buttons) { button.alpha = 0; button.interactable = false; }
        foreach (var button in buttons)
        {
            yield return CanvasFade.To(button, 1, fadeDuration);
            button.interactable = true;
        }
    }

    void OnDestroy() => Lang.Changed -= RefreshGodButton;

    // 준비된 신: 새 게임 버튼을 복제해 바로 아래에 둔다. 사망·해체를 뺀 엔딩 5개를 보기 전에는 취소선과 함께 잠겨 있다.
    CanvasGroup BuildGodButton()
    {
        var source = menuButtons[0]; // 새 게임
        var copy = Instantiate(source.gameObject, source.transform.parent);
        copy.name = "GodModeButton";
        copy.transform.SetSiblingIndex(source.transform.GetSiblingIndex() + 1);
        foreach (var localized in copy.GetComponentsInChildren<LocalizedText>(true)) Destroy(localized);
        godLabel = copy.GetComponentInChildren<TMP_Text>(true);
        godButton = copy.GetComponentInChildren<Button>(true);
        godButton.onClick = new Button.ButtonClickedEvent();
        godButton.onClick.AddListener(NewGodGame);
        Lang.Changed += RefreshGodButton;
        RefreshGodButton();
        return copy.GetComponent<CanvasGroup>();
    }

    void RefreshGodButton()
    {
        var unlocked = GodRules.Unlocked;
        godButton.enabled = unlocked;
        godLabel.text = unlocked ? Lang.Get("god_mode")
            : $"<color=#808080><s>{Lang.Get("god_mode")}</s> ({System.Math.Min(GodRules.UnlockProgress, GodRules.UnlockEndings)}/{GodRules.UnlockEndings})</color>";
    }

    public void NewGodGame()
    {
        if (!GodRules.Unlocked) return;
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
