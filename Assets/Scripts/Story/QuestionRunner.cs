using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 질문을 띄우고, 고른 선택지의 결과를 반영한다(Turns.Answer). 생애 이벤트는 답하면 1턴이 지난다.
// 떠 있는 이벤트 id를 GameSession에 기록해, 이벤트 도중에 저장해도 그 이벤트부터 이어서 할 수 있다.
public class QuestionRunner : MonoBehaviour
{
    [SerializeField] QuestionPanel panel;
    Question current;
    string currentId; // 떠 있는 이벤트 id (v2 기프티드 효과용)
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
        currentId = eventId;
        this.advancesTurn = advancesTurn;
        if (eventId != null) GameSession.SetPending(eventId, advancesTurn);
        AdultArt.Show(eventId);
        if (AdultGate.Active) { AdultRecall.Mark(eventId); NormalCounters.OnAdult(eventId, question); }
        if (eventId != null) AudioManager.PlaySfx(MusicKeys.EventSfx(eventId));
        panel.Show(QuestionPanel.WithPreface(preface, question.Text), question.Choices, eventId); // 선택지 뒤 확률·결과 미리보기(v2g044, v2m086)
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
        var choice = question.Choices[number - 1];
        var before = Snapshot();
        var outcome = Turns.Answer(choice, currentId);
        var effects = ChangesSince(before, outcome);
        if (advancesTurn) GameSession.AdvanceTurn();
        else Achievements.Evaluate();
        // 스탯이 바뀌지 않았으면 결과 창 없이 다음 글 위에 흐리게만 보여 준다. 바뀌었으면 결과 창을 닫아야 넘어간다
        if (string.IsNullOrEmpty(effects)) Answered?.Invoke(outcome.Text);
        else ResultWindow.Show(outcome == choice.Success, outcome.Text, effects, () => Answered?.Invoke(outcome.Text));
    }

    static readonly PersonalStatId[] PersonalIds = (PersonalStatId[])System.Enum.GetValues(typeof(PersonalStatId));
    static readonly StatId[] CultIds = (StatId[])System.Enum.GetValues(typeof(StatId));

    // 결과 창에는 기프티드 보정·상하한까지 반영된 실제 변화량을 보여 준다
    static (int[] cult, int[] personal, int points) Snapshot() => (
        CultIds.Select(id => GameSession.Cult[id].Value).ToArray(),
        PersonalIds.Select(id => GameSession.Personal[id].Value).ToArray(),
        GameSession.God.Points.Earned);

    static string ChangesSince((int[] cult, int[] personal, int points) before, Outcome outcome)
    {
        var now = Snapshot();
        // 값을 아는 스탯은 전후 값으로, 신 모드 효과(건물 압류 등)는 변화량으로, 기프티드는 이름으로 적는다
        var changes = CultIds.Select((id, i) => (StatLabel.Of(id), before.cult[i], now.cult[i]))
            .Concat(PersonalIds.Select((id, i) => (Lang.Get($"pstat_{id}"), before.personal[i], now.personal[i])))
            .ToList();
        var extras = new List<string>();
        if (GameSession.GodMode)
        {
            changes.Add((Lang.Get("god_fx_Points"), before.points, now.points));
            foreach (var (effect, amount) in outcome.GodEffects)
                if (effect != GodEventEffect.Points && amount != 0) extras.Add(ResultWindow.Delta(Lang.Get($"god_fx_{effect}"), amount));
        }
        extras.AddRange(outcome.Grants.Select(GiftedCatalog.Find).Where(t => t != null).Select(t => string.Format(Lang.Get("result_gifted"), t.Name)));
        return ResultWindow.TransitionsLine(changes, extras);
    }
}
