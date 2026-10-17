using System;
using System.Linq;
using UnityEngine;

// 질문을 띄우고, 고른 선택지의 결과를 반영한다(Turns.Answer). 생애 이벤트는 답하면 1턴이 지난다.
// 떠 있는 이벤트 id를 GameSession에 기록해, 이벤트 도중에 저장해도 그 이벤트부터 이어서 할 수 있다.
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
    // eventId: 저장·이어하기용 이벤트 id
    public void Run(Question question, string preface = null, bool advancesTurn = true, string eventId = null)
    {
        current = question;
        this.advancesTurn = advancesTurn;
        if (eventId != null) GameSession.SetPending(eventId, advancesTurn);
        AdultArt.Show(eventId);
        panel.Show(QuestionPanel.WithPreface(preface, question.Text), question.Choices.Select(choice => choice.Text).ToArray());
    }

    // 엔딩 등으로 떠 있던 이벤트를 더 이어가지 않을 때
    public void Cancel()
    {
        current = null;
        GameSession.ClearPending();
        AdultArt.Hide();
    }

    void OnChosen(int number)
    {
        if (current == null) return; // 이 질문이 아닌 곳(엔딩 선택 등)에서 온 선택
        var question = current;
        current = null;
        GameSession.ClearPending();
        AdultArt.Hide();
        var outcome = Turns.Answer(question.Choices[number - 1]);
        if (advancesTurn) GameSession.AdvanceTurn();
        else Achievements.Evaluate();
        Answered?.Invoke(outcome.Text);
    }
}
