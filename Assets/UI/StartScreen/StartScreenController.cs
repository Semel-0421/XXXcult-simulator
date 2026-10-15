using System.Collections;
using UnityEngine;

public class StartScreenController : MonoBehaviour
{
    [SerializeField] CanvasGroup[] menuButtons;
    [SerializeField] GameObject settingsPanel;
    [SerializeField] SaveSlotWindow saveWindow;
    [SerializeField] float fadeDuration = 0.3f;

    IEnumerator Start()
    {
        settingsPanel.SetActive(false);
        foreach (var button in menuButtons) { button.alpha = 0; button.interactable = false; }
        foreach (var button in menuButtons)
        {
            yield return CanvasFade.To(button, 1, fadeDuration);
            button.interactable = true;
        }
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
