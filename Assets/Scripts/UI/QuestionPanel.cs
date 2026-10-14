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
    public event Action<int> Chosen; // 1부터 시작하는 선택 번호

    void Awake()
    {
        numberInput.onSubmit.AddListener(Submit);
        confirmButton.onClick.AddListener(() => Submit(numberInput.text));
        SetInputActive(false);
    }

    public void Show(string question, params string[] choices)
    {
        choiceCount = choices.Length;
        var numbered = choices.Select((choice, i) => $"{i + 1}. {choice}");
        displayText.text = choiceCount == 0 ? question : $"{question}\n\n{string.Join("\n", numbered)}";
        numberInput.text = "";
        SetInputActive(choiceCount > 0);
        if (choiceCount > 0) numberInput.ActivateInputField();
    }

    void Submit(string typed)
    {
        if (choiceCount == 0) return;
        if (!int.TryParse(typed.Trim(), out var number) || number < 1 || number > choiceCount)
        {
            numberInput.text = "";
            numberInput.ActivateInputField();
            return;
        }
        numberInput.text = "";
        SetInputActive(false);
        Chosen?.Invoke(number);
    }

    void SetInputActive(bool active) => numberInput.interactable = confirmButton.interactable = active;
}
