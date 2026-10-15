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

    bool genderReady;

    void Update()
    {
        if (!genderReady || Menus.AnyOpen) return;
        if (Keybinds.NumberPressed(1)) ChooseGender(Gender.Male);
        else if (Keybinds.NumberPressed(2)) ChooseGender(Gender.Female);
    }

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
        ShowGenderLinks();
        // 성별은 입력칸 대신 글자를 눌러 고른다 (모바일에서 키보드가 화면을 가리지 않도록)
        inputGroup.alpha = 0;
        inputGroup.interactable = false;
        yield return CanvasFade.To(promptGroup, 1, promptFade / 2);
        genderReady = true;
    }

    // 안내문의 "남성" "여성" 글자를 눌러 고를 수 있게 만든다
    void ShowGenderLinks()
    {
        var label = promptGroup.GetComponentInChildren<TMP_Text>(true);
        var male = Lang.IsKorean ? "남성" : "Male";
        var female = Lang.IsKorean ? "여성" : "Female";
        label.text = Lang.Get("gender_prompt")
            .Replace(male, $"<link=\"g1\"><color=#F2D978><u>{male}</u></color></link>")
            .Replace(female, $"<link=\"g2\"><color=#F2D978><u>{female}</u></color></link>");
        label.raycastTarget = true;
        promptGroup.blocksRaycasts = true;
        promptGroup.interactable = true;
        var click = label.GetComponent<TextLinkClick>();
        if (click == null)
        {
            click = label.gameObject.AddComponent<TextLinkClick>();
            click.Clicked += id => ChooseGender(id == "g2" ? Gender.Female : Gender.Male);
        }
    }

    void ChooseGender(Gender gender)
    {
        if (!genderReady) return;
        genderReady = false;
        GameSession.Personal.Gender = gender;
        SetInputActive(false);
        RevealPanels();
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
        GameSession.MarkStarted();
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
        else nameInput.DeactivateInputField(); // 숨긴 입력칸이 포커스(모바일 키보드)를 쥐고 있지 않게
    }

    static void SetKey(Component root, string key)
    {
        var localized = root.GetComponentInChildren<LocalizedText>(true);
        if (localized != null) localized.SetKey(key);
        else root.GetComponentInChildren<TMP_Text>(true).text = Lang.Get(key);
    }
}
