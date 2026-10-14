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
        questionRunner.Run(IntroQuestions.First);
    }

    // 불러오기: 저장은 행동 선택 시점에만 되므로 행동 안내부터 이어진다
    public void Resume()
    {
        Subscribe();
        ShowActionPrompt();
    }

    void Subscribe()
    {
        questionRunner.Answered += OnEventAnswered;
        actionBar.LifeChosen += OnActionChosen;
    }

    void ShowActionPrompt()
    {
        questionPanel.Show(string.Format(Lang.Get("life_action_prompt"), GameSession.Age.Years));
        actionBar.Show(ActionBar.Mode.Life);
    }

    void OnDestroy()
    {
        questionRunner.Answered -= OnEventAnswered;
        actionBar.LifeChosen -= OnActionChosen;
    }

    void OnEventAnswered(Choice _)
    {
        if (TryFoundCult()) return;
        ShowActionPrompt();
    }

    void OnActionChosen(LifeAction action)
    {
        actionBar.SetInteractable(false);
        var success = LifeActions.Roll(GameSession.Personal, action, GameSession.Rng);
        LifeActions.Apply(GameSession.Personal, action, success);
        GameSession.AdvanceTurn();
        resultView.Play(action.ToString(), success, () =>
        {
            if (TryFoundCult()) return;
            var next = LifeEvent.Pick(LifeEvents.All, GameSession.Age.Years, GameSession.UsedEvents, GameSession.Rng);
            questionRunner.Run(next.Question, Lang.Get($"life_{(success ? "ok" : "fail")}_{action}"));
        });
    }

    bool TryFoundCult()
    {
        if (GameSession.Age.Turns < FoundingTurn) return false;
        questionRunner.Answered -= OnEventAnswered;
        actionBar.LifeChosen -= OnActionChosen;
        actionBar.Show(ActionBar.Mode.Hidden);
        questionPanel.AskText(Lang.Get("cult_name_prompt"), "cult_name_hint", cultName =>
        {
            GameSession.FoundCult(cultName);
            questionPanel.Show(string.Format(Lang.Get("cult_founded"), cultName));
        });
        return true;
    }
}
