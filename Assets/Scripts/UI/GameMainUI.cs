using UnityEngine;

public class GameMainUI : MonoBehaviour
{
    [SerializeField] StatBarUI[] statBars;
    [SerializeField] GameObject pausePanel;
    [SerializeField] SaveSlotWindow saveWindow;
    [SerializeField] ActionBar actionBar;
    [SerializeField] Texture2D cursorTexture;
    [SerializeField] Vector2 cursorHotspot = new(1, 1); // 검날 끝

    void Start()
    {
        Time.timeScale = 1;
        foreach (var bar in statBars) bar.Bind(GameSession.Cult);
        pausePanel.SetActive(false);
        Cursor.SetCursor(cursorTexture, cursorHotspot, CursorMode.Auto);
    }

    void OnDestroy() => Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

    public void Pause()
    {
        if (!pausePanel.activeSelf) Menus.Opened();
        pausePanel.SetActive(true);
        Time.timeScale = 0;
    }

    public void Resume()
    {
        if (pausePanel.activeSelf) Menus.Closed();
        pausePanel.SetActive(false);
        Time.timeScale = 1;
    }
    // 인트로가 끝난 뒤 엔딩 전까지 언제든 저장할 수 있다 (이벤트가 떠 있는 상태도 그대로 저장된다)
    public void OpenSaves() => saveWindow.Open(GameSession.Started && !GameSession.Ended);

    public void OpenSettings() => OptionsWindow.Open();
    public void OpenAchievements() => AchievementWindow.Open();

    public void ExitToTitle()
    {
        Time.timeScale = 1;
        UnityEngine.SceneManagement.SceneManager.LoadScene("XXXcult_simulater_game");
    }
}
