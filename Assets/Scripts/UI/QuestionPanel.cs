using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 질문 표시창 + 번호 입력창. 선택지는 1, 2, 3... 번호가 붙어 표시되고 고르는 방법은 네 가지다:
// 숫자 키(윗줄·키패드), 번호 입력 후 엔터/확인, 선택지 글자 클릭. 키보드·마우스만으로 고를 수 있다.
public class QuestionPanel : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] TMP_Text displayText;
    [SerializeField] TMP_InputField numberInput;
    [SerializeField] Button confirmButton;

    int choiceCount;
    Action<string> textAnswer;
    public event Action<int> Chosen; // 1부터 시작하는 선택 번호

    public const string MutedColor = "#9A8FB0";

    // 본문 앞에 직전 상황(행동 결과 등)을 흐린 글씨로 붙인다
    public static string WithPreface(string preface, string body) =>
        string.IsNullOrEmpty(preface) ? body : $"<color={MutedColor}>{preface}</color>\n\n{body}";

    bool AcceptsChoice => choiceCount > 0 && textAnswer == null && numberInput.interactable;

    void Awake()
    {
        numberInput.onSubmit.AddListener(Submit);
        displayText.raycastTarget = true; // 선택지 글자 클릭을 받기 위해
        // 이벤트 글 창은 자줏빛 책 표지에 금실 박음질 테두리
        UiSkin.ApplyBookFrame(GetComponent<Image>(), "EventBook", new Color(0.36f, 0.12f, 0.46f), new Color(0.2f, 0.06f, 0.3f));
        confirmButton.onClick.AddListener(() => Submit(numberInput.text));
        SetInputActive(false);
    }

    public void Show(string question, params string[] choices)
    {
        textAnswer = null;
        SetInputMode(TMP_InputField.ContentType.IntegerNumber, 3, "choice_hint");
        choiceCount = choices.Length;
        var numbered = choices.Select((choice, i) => $"{i + 1}. <link=\"{i + 1}\">{choice}</link>");
        displayText.text = choiceCount == 0 ? question : $"{question}\n\n{string.Join("\n", numbered)}";
        numberInput.text = "";
        SetInputActive(choiceCount > 0);
        // 번호 입력칸은 자동으로 포커스하지 않는다: 모바일에서 키보드가 화면을 가리므로 클릭·숫자 키로 고르게 둔다
    }

    // 번호 대신 글자를 입력받는다 (예: 교단 이름).
    public void AskText(string question, string hintKey, Action<string> answered)
    {
        textAnswer = answered;
        displayText.text = question;
        SetInputMode(TMP_InputField.ContentType.Standard, 20, hintKey);
        numberInput.text = "";
        SetInputActive(true);
        numberInput.ActivateInputField();
    }

    void SetInputMode(TMP_InputField.ContentType type, int limit, string hintKey)
    {
        numberInput.contentType = type;
        numberInput.characterLimit = limit;
        var hint = numberInput.placeholder.GetComponent<LocalizedText>();
        if (hint != null) hint.SetKey(hintKey);
    }

    void Submit(string typed)
    {
        if (textAnswer != null) { SubmitText(typed.Trim()); return; }
        if (choiceCount == 0) return;
        if (!int.TryParse(typed.Trim(), out var number) || number < 1 || number > choiceCount)
        {
            numberInput.text = "";
            numberInput.ActivateInputField();
            return;
        }
        Choose(number);
    }

    void Choose(int number)
    {
        numberInput.text = "";
        SetInputActive(false);
        AudioManager.PlaySfx("click");
        Chosen?.Invoke(number);
    }

    // 숫자 키로 바로 고른다
    void Update()
    {
        if (!AcceptsChoice || !Keybinds.Listening) return;
        for (int n = 1; n <= choiceCount && n <= 9; n++)
        {
            if (!Keybinds.NumberPressed(n)) continue;
            Keybinds.Consume();
            Choose(n);
            return;
        }
    }

    // 선택지 글자를 클릭하면 그 선택지를 고른다
    public void OnPointerClick(PointerEventData eventData)
    {
        if (Menus.AnyOpen) return;
        var index = TMP_TextUtilities.FindIntersectingLink(displayText, eventData.position, eventData.pressEventCamera);
        if (index < 0) return;
        var id = displayText.textInfo.linkInfo[index].GetLinkID();

        // 행동 안내의 [1] 운동 같은 글자를 누르면 그 행동을 고른다
        if (id.StartsWith(Keybinds.ActionLinkPrefix))
        {
            var bar = FindAnyObjectByType<ActionBar>();
            if (bar != null && bar.CanAct && int.TryParse(id.Substring(Keybinds.ActionLinkPrefix.Length), out var action))
                bar.Perform(action);
            return;
        }
        if (AcceptsChoice && int.TryParse(id, out var number) && number >= 1 && number <= choiceCount) Choose(number);
    }

    void SubmitText(string typed)
    {
        numberInput.text = "";
        if (typed.Length == 0) { numberInput.ActivateInputField(); return; }
        var answered = textAnswer;
        textAnswer = null;
        SetInputActive(false);
        answered(typed);
    }

    void SetInputActive(bool active)
    {
        numberInput.interactable = confirmButton.interactable = active;
        if (!active) numberInput.DeactivateInputField(); // 비활성일 때 커서가 남지 않게
    }
}
