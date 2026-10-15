using UnityEngine;

// 교단 창설 전 생애 진행: 이벤트(답하면 1턴) → 행동(고르면 1턴)을 번갈아 진행하다가 18세 4분기에 교단을 창설한다.
public class LifeFlow : MonoBehaviour
{
    public const int FoundingTurn = 18 * GameClock.QuartersPerYear + 3; // 18세 4분기

    [SerializeField] QuestionRunner questionRunner;
    [SerializeField] QuestionPanel questionPanel;
    [SerializeField] ActionBar actionBar;
    [SerializeField] ActionResultView resultView;

    public void Begin()
    {
        Subscribe();
        var first = Turns.NextLifeEvent();
        questionRunner.Run(first.Question, null, true, first.Id);
    }

    // 불러오기: 저장한 시점에 이벤트가 떠 있었다면 그 이벤트부터, 아니면 행동 안내부터 이어진다
    public void Resume()
    {
        Subscribe();
        if (TryDie() || TryFoundCult()) return; // 창설 시점에서 저장한 경우 교단 이름 입력부터
        if (ResumePending()) return;
        ShowActionPrompt();
    }

    bool ResumePending()
    {
        var id = GameSession.PendingEventId;
        if (string.IsNullOrEmpty(id)) return false;
        foreach (var e in LifeEvents.All)
        {
            if (e.Id != id) continue;
            actionBar.Show(ActionBar.Mode.Life);
            actionBar.SetInteractable(false);
            questionRunner.Run(e.Question, null, GameSession.PendingAdvancesTurn, id);
            return true;
        }
        GameSession.ClearPending();
        return false;
    }

    void Subscribe()
    {
        questionRunner.Answered += OnEventAnswered;
        actionBar.LifeChosen += OnActionChosen;
    }

    void Unsubscribe()
    {
        questionRunner.Answered -= OnEventAnswered;
        actionBar.LifeChosen -= OnActionChosen;
    }

    void OnDestroy() => Unsubscribe();

    void ShowActionPrompt(string preface = null)
    {
        var prompt = $"{string.Format(Lang.Get("life_action_prompt"), GameSession.Age.Years)}\n{ActionBar.Legend(ActionBar.Mode.Life)}";
        questionPanel.Show(QuestionPanel.WithPreface(preface, prompt));
        actionBar.Show(ActionBar.Mode.Life);
    }

    void OnEventAnswered(string resultText)
    {
        if (TryDie() || TryFoundCult()) return;
        ShowActionPrompt(resultText);
    }

    void OnActionChosen(LifeAction action)
    {
        actionBar.SetInteractable(false);
        var success = Turns.Life(action);
        resultView.Play(action.ToString(), success, () =>
        {
            if (TryDie() || TryFoundCult()) return;
            var next = Turns.NextLifeEvent();
            questionRunner.Run(next.Question, Lang.Get($"life_{(success ? "ok" : "fail")}_{action}"), true, next.Id);
        });
    }

    // 연령별 사망률로 교단을 세우기 전에 세상을 떠나면 요절 엔딩
    bool TryDie()
    {
        var forced = Progress.ForcedEnding();
        if (forced == null) return false;
        Unsubscribe();
        actionBar.Show(ActionBar.Mode.Hidden);
        questionRunner.Cancel();
        Progress.Reach(forced);
        EndingScreen.Show(questionPanel, Progress.TextOf(forced));
        return true;
    }

    bool TryFoundCult()
    {
        if (GameSession.Age.Turns < FoundingTurn) return false;
        Unsubscribe();
        actionBar.Show(ActionBar.Mode.Hidden);
        questionPanel.AskText(Lang.Get("cult_name_prompt"), "cult_name_hint", cultName =>
        {
            GameSession.FoundCult(cultName);
            questionPanel.Show(string.Format(Lang.Get("cult_founded"), cultName));
        });
        return true;
    }
}
