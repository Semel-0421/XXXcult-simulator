using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IntroSequence : MonoBehaviour
{
    [SerializeField] CanvasGroup blackOverlay, promptGroup, inputGroup, panelGroup, questionGroup;
    [SerializeField] TMP_InputField nameInput;
    [SerializeField] Button confirmButton;
    [SerializeField] PersonalStatPanel statPanel;
    [SerializeField] QuestionRunner questionRunner;
    [SerializeField] float blackHold = 0.5f, promptFade = 0.8f, panelFade = 0.5f;

    IEnumerator Start()
    {
        blackOverlay.alpha = 1;
        blackOverlay.blocksRaycasts = true;
        promptGroup.alpha = inputGroup.alpha = panelGroup.alpha = questionGroup.alpha = 0;
        inputGroup.interactable = false;

        yield return new WaitForSecondsRealtime(blackHold);
        yield return CanvasFade.To(promptGroup, 1, promptFade);

        inputGroup.alpha = 1;
        inputGroup.interactable = true;
        nameInput.onSubmit.AddListener(ConfirmName);
        confirmButton.onClick.AddListener(ConfirmFromButton);
        nameInput.ActivateInputField();
    }

    void ConfirmFromButton() => ConfirmName(nameInput.text);

    void ConfirmName(string typed)
    {
        var playerName = typed.Trim();
        if (playerName.Length == 0) { nameInput.ActivateInputField(); return; }

        nameInput.onSubmit.RemoveListener(ConfirmName);
        confirmButton.onClick.RemoveListener(ConfirmFromButton);
        inputGroup.interactable = false;
        GameSession.Personal.Name = playerName;
        statPanel.Show(GameSession.Personal);

        blackOverlay.blocksRaycasts = false;
        StartCoroutine(CanvasFade.To(blackOverlay, 0, panelFade));
        StartCoroutine(CanvasFade.To(panelGroup, 1, panelFade));
        StartCoroutine(CanvasFade.To(questionGroup, 1, panelFade));
        StartCoroutine(RunFirstQuestion());
    }

    IEnumerator RunFirstQuestion()
    {
        yield return new WaitForSecondsRealtime(panelFade);
        questionRunner.Run(IntroQuestions.First);
    }
}
