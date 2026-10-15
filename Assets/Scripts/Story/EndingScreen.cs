using UnityEngine;

// 엔딩 화면: 엔딩 글과 "처음으로 돌아가기 / 엔딩 목록" 선택지를 띄운다. 생애·교단 흐름이 함께 쓴다.
public static class EndingScreen
{
    static QuestionPanel panel;
    static string text;

    public static void Show(QuestionPanel questionPanel, string endingText)
    {
        if (panel != null) panel.Chosen -= OnChosen;
        panel = questionPanel;
        text = endingText;
        panel.Chosen += OnChosen;
        Prompt();
    }

    // 1 처음으로 돌아가기, 2 엔딩 목록
    static void Prompt() => panel.Show(text, Lang.Get("ending_restart"), Lang.Get("ending_list"));

    static void OnChosen(int number)
    {
        if (number == 1)
        {
            panel.Chosen -= OnChosen;
            panel = null;
            Time.timeScale = 1;
            UnityEngine.SceneManagement.SceneManager.LoadScene("XXXcult_simulater_game");
            return;
        }
        EndingWindow.Open();
        Prompt(); // 목록을 닫으면 같은 선택지가 다시 보이도록
    }
}
