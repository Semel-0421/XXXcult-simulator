using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 검은 화면에서 이름 → 성별을 입력받은 뒤 (황금의 눈동자 모드는 그 앞에 눈동자 질문) 패널을 띄우고 생애 진행(LifeFlow)을 시작한다.
public class IntroSequence : MonoBehaviour
{
    [SerializeField] CanvasGroup blackOverlay, promptGroup, inputGroup, panelGroup, questionGroup;
    [SerializeField] TMP_InputField nameInput;
    [SerializeField] Button confirmButton;
    [SerializeField] PersonalStatPanel statPanel;
    [SerializeField] LifeFlow lifeFlow;
    [SerializeField] float blackHold = 0.5f, promptFade = 0.8f, panelFade = 0.5f;

    bool askingGender;

    bool genderReady, eyeReady;

    void Update()
    {
        if (Menus.AnyOpen) return;
        if (eyeReady)
        {
            for (int n = 1; n <= EyeRules.Sectors.Length; n++)
                if (Keybinds.NumberPressed(n)) { ChooseEye(n); return; }
            return;
        }
        if (!genderReady) return;
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
        if (GameSession.EyeMode && GameSession.EyeSector == EyeSector.None) yield return AskEye();
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

    // 황금의 눈동자: 이름보다 먼저 "당신을 바라본 불타는 황금의 눈동자는 누구의 것이었는가." (DEC-027). 답이 이 판의 섹터가 된다
    IEnumerator AskEye()
    {
        var nameKey = promptGroup.GetComponentInChildren<LocalizedText>(true)?.Key;
        var label = LinkLabel();
        label.text = EyeRules.QuestionText();
        yield return CanvasFade.To(promptGroup, 1, promptFade);
        eyeReady = true;
        while (eyeReady) yield return null;
        yield return CanvasFade.To(promptGroup, 0, promptFade / 2);
        promptGroup.blocksRaycasts = promptGroup.interactable = false;
        label.raycastTarget = false;
        if (nameKey != null) SetKey(promptGroup, nameKey);
        else label.text = "";
    }

    void ChooseEye(int number)
    {
        if (!eyeReady || number < 1 || number > EyeRules.Sectors.Length) return;
        EyeRules.ChooseSector(EyeRules.Sectors[number - 1]);
        AudioManager.PlaySfx("click");
        eyeReady = false;
    }

    // 안내문 글자 링크를 누를 수 있게 한다 (e1~e4: 눈동자 답, g1·g2: 성별)
    TMP_Text LinkLabel()
    {
        var label = promptGroup.GetComponentInChildren<TMP_Text>(true);
        label.raycastTarget = true;
        promptGroup.blocksRaycasts = true;
        promptGroup.interactable = true;
        var click = label.GetComponent<TextLinkClick>();
        if (click == null)
        {
            click = label.gameObject.AddComponent<TextLinkClick>();
            click.Clicked += OnLink;
        }
        return label;
    }

    void OnLink(string id)
    {
        if (id.StartsWith("e") && int.TryParse(id.Substring(1), out var n)) ChooseEye(n);
        else if (id is "g1" or "g2") ChooseGender(id == "g2" ? Gender.Female : Gender.Male);
    }

    // 안내문의 "남성" "여성" 글자를 눌러 고를 수 있게 만든다
    void ShowGenderLinks()
    {
        var label = LinkLabel();
        var male = Lang.IsKorean ? "남성" : "Male";
        var female = Lang.IsKorean ? "여성" : "Female";
        label.text = Lang.Get("gender_prompt")
            .Replace(male, $"<link=\"g1\"><color=#F2D978>{male}</color></link>")
            .Replace(female, $"<link=\"g2\"><color=#F2D978>{female}</color></link>");
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
        if (GameSession.GodMode) lifeFlow.Resume(); // 유년기 없이 성인으로 시작: 곧바로 교단 이름 입력
        else lifeFlow.Begin();
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
