using System.Collections;
using System.Linq;
using UnityEngine;

// 교단 창설 후 진행: 교단 행동을 고르면 효과 반영 후 1턴(1분기)이 지나고,
// 조건 이벤트가 있으면 먼저, 없으면 부정적·확률 이벤트가 이어질 수 있다. 이벤트는 턴을 쓰지 않는다.
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
        ConsecrateWindow.Chosen += OnConsecrate;
        ConsecrateWindow.CanAct = () => actionBar.CanAct;
        tree = GameSession.Cult.Doctrines;
        tree.Changed += OnDoctrineChanged;
        if (GameSession.CultFounded && TryDissolve()) return; // 엔딩 직전에 저장한 판을 불러온 경우
        if (GameSession.CultFounded && tree.IsMax(DoctrineSkill.Declaration) && !ResumePending()) PromptOrRest(null); // 불러온 경우
    }

    // 저장한 시점에 이벤트가 떠 있었다면 그 이벤트부터 이어서 한다
    bool ResumePending()
    {
        var id = GameSession.PendingEventId;
        if (string.IsNullOrEmpty(id)) return false;
        foreach (var e in AdultGate.Active ? Turns.CultPool.Concat(AdultEvents.All) : Turns.CultPool)
        {
            if (e.Id != id) continue;
            questionRunner.Answered += OnEventAnswered;
            questionRunner.Run(e.Question, null, false, id);
            return true;
        }
        GameSession.ClearPending();
        return false;
    }

    void OnDestroy()
    {
        actionBar.CultChosen -= OnActionChosen;
        ConsecrateWindow.Chosen -= OnConsecrate;
        ConsecrateWindow.CanAct = () => false;
        questionRunner.Answered -= OnEventAnswered;
        if (restRoutine != null) StopCoroutine(restRoutine);
        if (tree != null) tree.Changed -= OnDoctrineChanged;
    }

    void OnDoctrineChanged(DoctrineSkill skill)
    {
        NormalCounters.OnDoctrine(skill);
        if (skill != DoctrineSkill.Declaration) return;
        Achievements.Evaluate();
        PromptOrRest(null);
    }

    // 엔딩 판정은 행동을 고르는 시점에 확인한다 (규칙은 Progress.DueEnding)
    void Update()
    {
        if (tree == null || GameSession.Ended || !GameSession.CultFounded) return;
        Progress.Track();
        if (Menus.AnyOpen || !actionBar.CanAct) return;
        var due = Progress.DueEnding();
        if (due != null) BeginEnding(due);
    }

    void OnActionChosen(CultAction action)
    {
        actionBar.SetInteractable(false);
        var success = Turns.Cult(action);
        var summary = Lang.Get($"cult_{(success ? "done" : "fail")}_{action}");

        resultView.Play(CultActions.Key(action), success, () => AfterTurn(summary));
    }

    // 축성(봉헌 건축)도 행동 턴을 쓴다
    void OnConsecrate(GodBuildingType type)
    {
        if (!actionBar.CanAct) return;
        actionBar.SetInteractable(false);
        if (!Turns.Consecrate(type)) { actionBar.SetInteractable(true); return; }
        var summary = string.Format(Lang.Get("god_consecrated"), Lang.Get($"god_building_{type}"), GameSession.God.Buildings.LevelOf(type));
        resultView.Play("Consecrate", true, () => AfterTurn(summary));
    }

    // 행동 턴이 끝난 뒤: 엔딩 확인 → 이어질 이벤트 → 다음 행동 안내
    void AfterTurn(string summary)
    {
        if (TryDissolve()) return;
        var next = Turns.NextCultEvent();
        if (next == null) { actionBar.SetInteractable(true); PromptOrRest(summary); return; }

        var preface = next.IsNegative && !GameSession.GodMode
            ? $"{summary}\n<color=#DC5050>{string.Format(Lang.Get("misfortune_struck"), GameSession.Misfortune)}</color>"
            : summary;
        questionRunner.Answered += OnEventAnswered;
        questionRunner.Run(next.IsNegative ? GiftedV2Effects.WithRescueChoiceNormal(next.Question) : next.Question, preface, advancesTurn: false, eventId: next.Id);
    }

    void OnEventAnswered(string resultText)
    {
        questionRunner.Answered -= OnEventAnswered;
        if (TryDissolve()) return;
        actionBar.SetInteractable(true);
        PromptOrRest(resultText);
    }

    // 곧바로 판이 끝나는 엔딩(명망이 바닥나 국가의 명령으로 해체)이면 엔딩으로 넘어간다
    bool TryDissolve()
    {
        var forced = Progress.ForcedEnding();
        if (forced == null) return false;
        BeginEnding(forced);
        return true;
    }

    // 엔딩에 도달: 진행을 멈추고 기록을 남긴 뒤 엔딩 화면을 띄운다
    void BeginEnding(Endings.Ending ending)
    {
        actionBar.CultChosen -= OnActionChosen;
        questionRunner.Answered -= OnEventAnswered;
        questionRunner.Cancel();
        actionBar.Show(ActionBar.Mode.Hidden);
        Progress.Reach(ending);
        EndingScreen.Show(questionPanel, Progress.TextOf(ending));
    }

    // 행동 안내를 보여 주기 전에 하한(0)에 이른 개인 스탯이 있으면 먼저 휴식한다
    void PromptOrRest(string preface)
    {
        if (restRoutine != null) return;
        if (!Rest.NeedsRest(GameSession.Personal)) { ShowPrompt(preface); return; }
        restRoutine = StartCoroutine(RestRoutine(preface));
    }

    Coroutine restRoutine;

    // 1년(4분기) 휴식: 0이 된 스탯은 분기마다 +3, 나머지 개인 스탯은 -1. 다른 이벤트는 일어나지 않는다.
    // 끝난 뒤 0인 스탯이 남아 있으면(휴식 중 새로 0이 된 것 포함) 모두 0이 아닐 때까지 반복한다.
    IEnumerator RestRoutine(string preface)
    {
        actionBar.SetInteractable(false);
        while (true)
        {
            var resting = Rest.ZeroStats(GameSession.Personal);
            if (resting.Count == 0 || GameSession.Dead) break;
            GameSession.Count("rests");
            var names = string.Join(", ", resting.Select(id => Lang.Get($"pstat_{id}")));
            for (int turn = 1; turn <= Rest.Turns; turn++)
            {
                Rest.ApplyTurn(GameSession.Personal, GameSession.Cult, resting);
                GameSession.AdvanceTurn();
                questionPanel.Show(string.Format(Lang.Get("rest_progress"), names, turn, Rest.Turns));
                yield return new WaitForSecondsRealtime(0.8f);
            }
            GiftedV2GodHooks.OnRestYear();
        }
        restRoutine = null;
        if (TryDissolve()) yield break; // 쉬는 동안 명망이 바닥났다면 해체
        actionBar.SetInteractable(true);
        ShowPrompt(Lang.Get("rest_done"));
    }

    void ShowPrompt(string preface)
    {
        var clock = GameSession.CultTime;
        var prompt = $"{string.Format(Lang.Get("cult_action_prompt"), clock.Years, clock.Quarter)}\n{ActionBar.Legend(ActionBar.Mode.Cult)}";
        questionPanel.Show(QuestionPanel.WithPreface(preface, prompt));
    }
}
