using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 질문 표시창 + 번호 입력창. 선택지는 1, 2, 3... 번호가 붙어 표시되고 엔터/확인 버튼으로 고른다.
public class QuestionPanel : MonoBehaviour
{
    [SerializeField] TMP_Text displayText;
    [SerializeField] TMP_InputField numberInput;
    [SerializeField] Button confirmButton;

    int choiceCount;
    Action<string> textAnswer;
    public event Action<int> Chosen; // 1부터 시작하는 선택 번호

    void Awake()
    {
        numberInput.onSubmit.AddListener(Submit);
        confirmButton.onClick.AddListener(() => Submit(numberInput.text));
        SetInputActive(false);
    }

    public void Show(string question, params string[] choices)
    {
        textAnswer = null;
        SetInputMode(TMP_InputField.ContentType.IntegerNumber, 3, "choice_hint");
        choiceCount = choices.Length;
        var numbered = choices.Select((choice, i) => $"{i + 1}. {choice}");
        displayText.text = choiceCount == 0 ? question : $"{question}\n\n{string.Join("\n", numbered)}";
        numberInput.text = "";
        SetInputActive(choiceCount > 0);
        if (choiceCount > 0) numberInput.ActivateInputField();
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
        numberInput.text = "";
        SetInputActive(false);
        AudioManager.PlaySfx("click");
        Chosen?.Invoke(number);
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
