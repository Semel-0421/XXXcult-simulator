using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 검은 화면에서 이름 → 성별을 입력받고, 첫 질문 후 교단 이름을 정해 창설한다.
public class IntroSequence : MonoBehaviour
{
    [SerializeField] CanvasGroup blackOverlay, promptGroup, inputGroup, panelGroup, questionGroup;
    [SerializeField] TMP_InputField nameInput;
    [SerializeField] Button confirmButton;
    [SerializeField] PersonalStatPanel statPanel;
    [SerializeField] QuestionRunner questionRunner;
    [SerializeField] QuestionPanel questionPanel;
    [SerializeField] float blackHold = 0.5f, promptFade = 0.8f, panelFade = 0.5f;

    bool askingGender;

    IEnumerator Start()
    {
        blackOverlay.alpha = 1;
        blackOverlay.blocksRaycasts = true;
        promptGroup.alpha = inputGroup.alpha = panelGroup.alpha = questionGroup.alpha = 0;
        inputGroup.interactable = false;

        yield return new WaitForSecondsRealtime(blackHold);
        yield return CanvasFade.To(promptGroup, 1, promptFade);

        nameInput.onSubmit.AddListener(Submit);
        confirmButton.onClick.AddListener(() => Submit(nameInput.text));
        SetInputActive(true);
    }

    void Submit(string typed)
    {
        if (!inputGroup.interactable) return;
        typed = typed.Trim();
        nameInput.text = "";

        if (!askingGender)
        {
            if (typed.Length == 0) { nameInput.ActivateInputField(); return; }
            GameSession.Personal.Name = typed;
            StartCoroutine(AskGender());
        }
        else if (TryParseGender(typed, out var gender))
        {
            GameSession.Personal.Gender = gender;
            SetInputActive(false);
            RevealPanels();
        }
        else nameInput.ActivateInputField();
    }

    IEnumerator AskGender()
    {
        askingGender = true;
        SetInputActive(false);
        yield return CanvasFade.To(promptGroup, 0, promptFade / 2);
        SetKey(promptGroup, "gender_prompt");
        SetKey(nameInput.placeholder, "choice_hint");
        yield return CanvasFade.To(promptGroup, 1, promptFade / 2);
        SetInputActive(true);
    }

    static bool TryParseGender(string typed, out Gender gender)
    {
        gender = Gender.Male;
        switch (typed.ToLowerInvariant())
        {
            case "1": case "남": case "남성": case "male": return true;
            case "2": case "여": case "여성": case "female": gender = Gender.Female; return true;
            default: return false;
        }
    }

    void RevealPanels()
    {
        statPanel.Show(GameSession.Personal);
        blackOverlay.blocksRaycasts = false;
        StartCoroutine(CanvasFade.To(blackOverlay, 0, panelFade));
        StartCoroutine(CanvasFade.To(promptGroup, 0, panelFade));
        StartCoroutine(CanvasFade.To(panelGroup, 1, panelFade));
        StartCoroutine(CanvasFade.To(questionGroup, 1, panelFade));
        StartCoroutine(RunFirstQuestion());
    }

    IEnumerator RunFirstQuestion()
    {
        yield return new WaitForSecondsRealtime(panelFade);
        questionRunner.Answered += OnIntroAnswered;
        questionRunner.Run(IntroQuestions.First);
    }

    // 초기 캐릭터 설정이 끝나면 교단을 창설하고 이름을 정한다.
    void OnIntroAnswered(Choice _)
    {
        questionRunner.Answered -= OnIntroAnswered;
        questionPanel.AskText(Lang.Get("cult_name_prompt"), "cult_name_hint", cultName =>
        {
            GameSession.FoundCult(cultName);
            questionPanel.Show(string.Format(Lang.Get("cult_founded"), cultName));
        });
    }

    void SetInputActive(bool active)
    {
        inputGroup.alpha = 1;
        inputGroup.interactable = active;
        if (active) nameInput.ActivateInputField();
    }

    static void SetKey(Component root, string key)
    {
        var localized = root.GetComponentInChildren<LocalizedText>(true);
        if (localized != null) localized.SetKey(key);
        else root.GetComponentInChildren<TMP_Text>(true).text = Lang.Get(key);
    }
}
