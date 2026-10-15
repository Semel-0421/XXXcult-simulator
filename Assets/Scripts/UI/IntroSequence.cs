using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 검은 화면에서 이름 → 성별을 입력받은 뒤 패널을 띄우고 생애 진행(LifeFlow)을 시작한다.
public class IntroSequence : MonoBehaviour
{
    [SerializeField] CanvasGroup blackOverlay, promptGroup, inputGroup, panelGroup, questionGroup;
    [SerializeField] TMP_InputField nameInput;
    [SerializeField] Button confirmButton;
    [SerializeField] PersonalStatPanel statPanel;
    [SerializeField] LifeFlow lifeFlow;
    [SerializeField] float blackHold = 0.5f, promptFade = 0.8f, panelFade = 0.5f;

    bool askingGender;

    IEnumerator Start()
    {
        if (GameSession.ConsumeResume())
        {
            ShowResumed();
            yield break;
        }

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

    // 불러온 판: 이름·성별 입력과 첫 질문을 건너뛰고 저장 시점으로 이어간다
    void ShowResumed()
    {
        blackOverlay.alpha = 0;
        blackOverlay.blocksRaycasts = false;
        promptGroup.alpha = inputGroup.alpha = 0;
        inputGroup.interactable = false;
        panelGroup.alpha = questionGroup.alpha = 1;
        statPanel.Show(GameSession.Personal);
        if (!GameSession.CultFounded) lifeFlow.Resume();
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
        lifeFlow.Begin();
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
