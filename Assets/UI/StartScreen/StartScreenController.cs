using System.Collections;
using UnityEngine;

public class StartScreenController : MonoBehaviour
{
    [SerializeField] CanvasGroup[] menuButtons;
    [SerializeField] GameObject settingsPanel;
    [SerializeField] float fadeDuration = 0.3f;

    IEnumerator Start()
    {
        settingsPanel.SetActive(false);
        foreach (var button in menuButtons) { button.alpha = 0; button.interactable = false; }
        foreach (var button in menuButtons)
        {
            for (float elapsed = 0; elapsed < fadeDuration; elapsed += Time.unscaledDeltaTime)
            {
                button.alpha = elapsed / fadeDuration;
                yield return null;
            }
            button.alpha = 1;
            button.interactable = true;
        }
    }

    public void OpenSettings() => settingsPanel.SetActive(true);
    public void CloseSettings() => settingsPanel.SetActive(false);
    public void SetEnglish() => Lang.Set(false);
    public void SetKorean() => Lang.Set(true);

    public void NewGame() => UnityEngine.SceneManagement.SceneManager.LoadScene("GameMain");

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
