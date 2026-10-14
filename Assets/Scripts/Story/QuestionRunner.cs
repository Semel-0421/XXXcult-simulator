using System;
using System.Linq;
using UnityEngine;

// 질문을 띄우고, 고른 선택지의 효과(기프티드 보정 포함)와 기프티드 지급을 반영한 뒤 1턴을 진행한다.
public class QuestionRunner : MonoBehaviour
{
    [SerializeField] QuestionPanel panel;
    Question current;
    public event Action<Choice> Answered;

    void OnEnable() => panel.Chosen += OnChosen;
    void OnDisable() => panel.Chosen -= OnChosen;

    // preface: 질문 앞에 흐리게 붙는 앞 상황 설명 (예: 직전 행동 결과)
    public void Run(Question question, string preface = null)
    {
        current = question;
        var text = string.IsNullOrEmpty(preface) ? question.Text : $"<color=#9A8FB0>{preface}</color>\n\n{question.Text}";
        panel.Show(text, question.Choices.Select(choice => choice.Text).ToArray());
    }

    void OnChosen(int number)
    {
        var choice = current.Choices[number - 1];
        foreach (var effect in choice.Effects)
            GiftedEffects.Apply(GameSession.Personal, effect.Stat, effect.Amount);
        foreach (var id in choice.GrantedGifted)
            GiftedGranter.Grant(GameSession.Personal.Talents, id);
        GameSession.AdvanceTurn(); // 질문에 답하면 1턴(1분기)이 지난다
        Answered?.Invoke(choice);
    }
}
