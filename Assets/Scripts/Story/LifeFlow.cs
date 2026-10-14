using UnityEngine;

// 교단 창설 전 생애 진행: 이벤트(답하면 1턴) → 행동(고르면 1턴)을 번갈아 진행하다가 18세 4분기에 교단을 창설한다.
public class LifeFlow : MonoBehaviour
{
    public const int FoundingTurn = 18 * GameClock.QuartersPerYear + 3; // 18세 4분기

    [SerializeField] QuestionRunner questionRunner;
    [SerializeField] QuestionPanel questionPanel;
    [SerializeField] ActionBar actionBar;

    public void Begin()
    {
        questionRunner.Answered += OnEventAnswered;
        actionBar.LifeChosen += OnActionChosen;
        questionRunner.Run(IntroQuestions.First);
    }

    void OnDestroy()
    {
        questionRunner.Answered -= OnEventAnswered;
        actionBar.LifeChosen -= OnActionChosen;
    }

    void OnEventAnswered(Choice _)
    {
        if (TryFoundCult()) return;
        questionPanel.Show(string.Format(Lang.Get("life_action_prompt"), GameSession.Age.Years));
        actionBar.Show(ActionBar.Mode.Life);
    }

    void OnActionChosen(LifeAction action)
    {
        actionBar.SetInteractable(false);
        LifeActions.Apply(GameSession.Personal, action);
        GameSession.AdvanceTurn();
        if (TryFoundCult()) return;

        var next = LifeEvent.Pick(LifeEvents.All, GameSession.Age.Years, GameSession.UsedEvents, GameSession.Rng);
        var summary = string.Format(Lang.Get("life_action_done"), Lang.Get($"life_{action}"));
        questionRunner.Run(next.Question, summary);
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
