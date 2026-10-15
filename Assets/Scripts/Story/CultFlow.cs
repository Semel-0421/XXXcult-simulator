using UnityEngine;

// 교단 창설 후 진행: 교단 행동을 고르면 효과 반영 후 1턴(1분기)이 지나고,
// 조건 이벤트가 있으면 먼저, 없으면 60% 확률로 이벤트가 이어진다. 이벤트는 턴을 쓰지 않는다.
public class CultFlow : MonoBehaviour
{
    [SerializeField] QuestionRunner questionRunner;
    [SerializeField] QuestionPanel questionPanel;
    [SerializeField] ActionBar actionBar;
    [SerializeField] ActionResultView resultView;

    DoctrineTree tree;

    void Start()
    {
        actionBar.CultChosen += OnActionChosen;
        tree = GameSession.Cult.Doctrines;
        tree.Changed += OnDoctrineChanged;
        if (GameSession.CultFounded && tree.IsMax(DoctrineSkill.Declaration)) ShowPrompt(null); // 불러온 경우
    }

    void OnDestroy()
    {
        actionBar.CultChosen -= OnActionChosen;
        questionRunner.Answered -= OnEventAnswered;
        if (tree != null) tree.Changed -= OnDoctrineChanged;
    }

    void OnDoctrineChanged(DoctrineSkill skill)
    {
        if (skill != DoctrineSkill.Declaration) return;
        Achievements.Evaluate();
        ShowPrompt(null);
    }

    void OnActionChosen(CultAction action)
    {
        actionBar.SetInteractable(false);
        var cult = GameSession.Cult;
        var success = CultActions.Roll(cult, GameSession.Personal.Talents, action, GameSession.Rng);
        CultActions.Apply(cult, GameSession.Personal, action, success);
        GameSession.Count($"act_{CultActions.Key(action)}");
        GameSession.Count("cult_acts");
        GameSession.Count(success ? "act_ok" : "act_fail");
        SocialStanding.ApplyTurn(cult); // 사회적 시선에 따른 견제/지원
        Misfortune.ApplyTurn(cult, GameSession.Misfortune, Misfortune.TotalResistance(cult)); // 쌓인 불운만큼 쇠퇴
        GameSession.AdvanceTurn();
        var summary = Lang.Get($"cult_{(success ? "done" : "fail")}_{action}");

        resultView.Play(CultActions.Key(action), success, () =>
        {
            if (TryDissolve()) return;
            var next = CultEvent.FindTriggered(CultEvents.All, cult, GameSession.UsedEvents)
                       ?? CultEvent.RollNegative(CultEvents.All, cult, GameSession.UsedEvents, GameSession.Rng, GameSession.CultTime.Years + 1)
                       ?? CultEvent.RollRandom(CultEvents.All, cult, GameSession.UsedEvents, GameSession.Rng);
            if (next == null) { actionBar.SetInteractable(true); ShowPrompt(summary); return; }

            var preface = summary;
            if (next.IsNegative)
            {
                GameSession.AddMisfortune();
                preface = $"{summary}\n<color=#DC5050>{string.Format(Lang.Get("misfortune_struck"), GameSession.Misfortune)}</color>";
            }
            questionRunner.Answered += OnEventAnswered;
            questionRunner.Run(next.Question, preface, advancesTurn: false);
        });
    }

    void OnEventAnswered(string resultText)
    {
        questionRunner.Answered -= OnEventAnswered;
        if (TryDissolve()) return;
        actionBar.SetInteractable(true);
        ShowPrompt(resultText);
    }

    // 명망이 바닥나면 국가의 명령으로 교단이 해체되며 게임이 끝난다
    bool TryDissolve()
    {
        if (SocialStanding.Of(GameSession.Cult) != Standing.Dissolved) return false;
        actionBar.CultChosen -= OnActionChosen;
        actionBar.Show(ActionBar.Mode.Hidden);
        Profile.Count("dissolved");
        Achievements.Evaluate();
        var ending = string.Format(Lang.Get("ending_dissolved"), GameSession.Cult.Name, GameSession.CultTime.Years);
        questionPanel.Show($"{ending}\n\n<color=#F2D978>{Lang.Get("ending_legacy")}\n{Legacy.Describe()}</color>");
        return true;
    }

    void ShowPrompt(string preface)
    {
        var clock = GameSession.CultTime;
        var prompt = string.Format(Lang.Get("cult_action_prompt"), clock.Years, clock.Quarter);
        questionPanel.Show(string.IsNullOrEmpty(preface) ? prompt : $"<color=#9A8FB0>{preface}</color>\n\n{prompt}");
    }
}
