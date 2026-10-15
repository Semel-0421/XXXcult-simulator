using System;
using System.Linq;
using UnityEngine;

// 질문을 띄우고, 고른 선택지의 성공/실패를 판정해 해당 결과의 효과(기프티드 보정 포함)와 기프티드 지급을 반영한다.
// 생애 이벤트는 답하면 1턴이 지난다.
public class QuestionRunner : MonoBehaviour
{
    [SerializeField] QuestionPanel panel;
    Question current;
    bool advancesTurn;
    public event Action<string> Answered; // 고른 결과의 결과문 (없으면 빈 문자열)

    void OnEnable() => panel.Chosen += OnChosen;
    void OnDisable() => panel.Chosen -= OnChosen;

    // preface: 질문 앞에 흐리게 붙는 앞 상황 설명 (예: 직전 행동 결과)
    // advancesTurn: 교단 창설 후 이벤트는 행동 턴 안에서 일어나므로 false
    public void Run(Question question, string preface = null, bool advancesTurn = true)
    {
        current = question;
        this.advancesTurn = advancesTurn;
        var text = string.IsNullOrEmpty(preface) ? question.Text : $"<color=#9A8FB0>{preface}</color>\n\n{question.Text}";
        panel.Show(text, question.Choices.Select(choice => choice.Text).ToArray());
    }

    void OnChosen(int number)
    {
        var choice = current.Choices[number - 1];
        var success = choice.Roll(GameSession.Rng);
        var outcome = choice.Resolve(success);
        GameSession.Count("events");
        GameSession.Count(success ? "choice_ok" : "choice_fail");
        foreach (var effect in outcome.Effects)
            GiftedEffects.Apply(GameSession.Personal, effect.Stat, effect.Amount);
        foreach (var effect in outcome.CultEffects)
            GiftedEffects.Apply(GameSession.Cult, GameSession.Personal.Talents, effect.Stat, effect.Amount);
        foreach (var id in outcome.Grants)
            GiftedGranter.Grant(GameSession.Personal.Talents, id);
        if (advancesTurn) GameSession.AdvanceTurn();
        else Achievements.Evaluate();
        Answered?.Invoke(outcome.Text);
    }
}
