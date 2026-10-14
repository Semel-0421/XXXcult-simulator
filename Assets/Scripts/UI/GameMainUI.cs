using UnityEngine;

public class GameMainUI : MonoBehaviour
{
    [SerializeField] StatBarUI[] statBars;
    [SerializeField] GameObject pausePanel;

    void Start()
    {
        Time.timeScale = 1;
        foreach (var bar in statBars) bar.Bind(GameSession.Cult);
        pausePanel.SetActive(false);
    }

    public void Pause() { pausePanel.SetActive(true); Time.timeScale = 0; }
    public void Resume() { pausePanel.SetActive(false); Time.timeScale = 1; }
    public void OpenSettings() { /* 추후 설정 화면 연결 */ }
    public void OpenAchievements() { /* 추후 업적 화면 연결 */ }

    public void ExitToTitle()
    {
        Time.timeScale = 1;
        UnityEngine.SceneManagement.SceneManager.LoadScene("XXXcult_simulater_game");
    }
}
