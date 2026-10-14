using System.Linq;
using UnityEngine;

// 질문을 띄우고, 고른 선택지의 효과를 개인 스탯에 반영한다.
public class QuestionRunner : MonoBehaviour
{
    [SerializeField] QuestionPanel panel;
    Question current;

    void OnEnable() => panel.Chosen += OnChosen;
    void OnDisable() => panel.Chosen -= OnChosen;

    public void Run(Question question)
    {
        current = question;
        panel.Show(question.Text, question.Choices.Select(choice => choice.Text).ToArray());
    }

    void OnChosen(int number)
    {
        foreach (var effect in current.Choices[number - 1].Effects)
            GameSession.Personal[effect.Stat].Add(effect.Amount);
    }
}
