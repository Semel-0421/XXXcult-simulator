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

    public void Run(Question question)
    {
        current = question;
        panel.Show(question.Text, question.Choices.Select(choice => choice.Text).ToArray());
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
