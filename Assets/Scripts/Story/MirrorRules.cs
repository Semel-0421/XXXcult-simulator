using System.Linq;

// 거울 세계 모드의 엔딩 판정 (Docs/GDD/systems/SYS_INVERTED.md, DEC-004~008, DEC-020~021). 엔딩 목록은 MirrorEndings.
//  - 최종 스킬: 본편과 같은 시점(교단 10년 뒤, 고른 가지 수치가 회차 문턱을 넘음)에 그 쪽 평균이 반대쪽보다 20 이상 높으면 굿, 아니면 배드
//  - 교단 25년에 최종 스킬이 없으면 평균이 높은 쪽(욕망/미덕) 엔딩, 차이 20 이상 굿. 같으면 2년마다 차이 10 이상인지 보고,
//    벌어지면 3년 뒤 그때 수치로 판정. 40년까지 안 벌어지면 갈림길 이벤트(me03): 물려주기 → 히든 엔딩 즉시 / 보유 → 계속 진행, 사망하면 히든 엔딩
//  - 평균은 소수 첫째 자리로 반올림한 값으로 비교한다 (동률·차이 10·차이 20 모두). 보유를 고른 뒤에도 다른 엔딩은 평소대로 나고, 사망하면 교단의 주인 엔딩이 우선한다
//  - 신도 0: 교단 3년부터. 8턴 유예 뒤에도 0이면 엔딩, 유예 중 회복하면 계속하되 이후 다시 0이면 즉시 엔딩
//  - 명망 0 → 흡수, 노화 사망 → 노화 엔딩 (창설 전 사망은 본편 요절)
// 진행 상태는 판의 카운터(GameSession.Count, 저장 파일에 남는다)에 둔다.
public static class MirrorRules
{
    public const int JudgeYears = 25, BranchYears = 40, CheckEveryYears = 2, DelayYears = 3;
    public const float GoodGap = 20f, TieBreakGap = 10f;
    public const int FollowersFromYears = 5, GraceTurns = 16; // 신도 붕괴 완화: 3년차→5년차부터, 유예 8턴→16턴
    public const string FinalPrefix = "inv_end_final_";

    // 카운터 키
    const string GraceStart = "mirror_grace_start"; // 유예를 시작한 교단 턴 + 1 (0 = 유예 중 아님)
    const string GraceUsed = "mirror_grace_used";   // 유예를 한 번 써서 회복했음
    const string TieChecked = "mirror_tie_year";    // 동률 판정 중: 마지막으로 확인한 교단 연수 (0 = 동률 아님)
    const string JudgeAt = "mirror_judge_turn";     // 동률이 깨져 판정할 교단 턴 + 1
    const string Branch = "mirror_branch";          // 1 = 갈림길 대기, 2 = 물려줌, 3 = 끝까지 보유
    const int BranchPending = 1, BranchHandover = 2, BranchKeep = 3;

    public static Endings.Ending Find(string id) => MirrorEndings.All.FirstOrDefault(e => e.Id == id);

    // 최종 스킬 엔딩이면 그 스킬 (삽화용)
    public static DoctrineSkill FinalSkillOf(Endings.Ending ending)
    {
        if (!ending.Id.StartsWith(FinalPrefix)) return null;
        var id = ending.Id.Substring(FinalPrefix.Length);
        return MirrorDoctrineCatalog.Find(id.Substring(0, id.LastIndexOf('_')));
    }

    // 갈림길 이벤트(me03)가 나올 차례인지
    public static bool BranchDue => GameSession.MirrorMode && GameSession.CountOf(Branch) == BranchPending;

    static float Desire => CultEvent.TraitAverage(GameSession.Personal, CultEvent.MoodTrait.Desire);
    static float Virtue => CultEvent.TraitAverage(GameSession.Personal, CultEvent.MoodTrait.Virtue);
    // 소수 첫째 자리 반올림을 0.1 단위 정수로 (부동소수 비교 오차를 피한다)
    public static int Tenths(float average) => (int)System.Math.Round(average * 10.0, System.MidpointRounding.AwayFromZero);
    static int GapTenths(float a, float b) => Tenths(a) - Tenths(b);
    static void SetCount(string key, int value) => GameSession.Count(key, value - GameSession.CountOf(key));
    static string Grade(int gapTenths) => gapTenths >= GoodGap * 10 ? "good" : "bad";

    // 판정식 (스탯만 보고 id를 정한다)
    public static string FinalId(DoctrineSkill final, float desire, float virtue)
    {
        var gap = Endings.IsDesireBranch(final.Branch) ? GapTenths(desire, virtue) : GapTenths(virtue, desire);
        return $"{FinalPrefix}{final.Id}_{Grade(gap)}";
    }

    public static string SideId(float desire, float virtue)
    {
        var gap = GapTenths(desire, virtue);
        return gap > 0 ? $"inv_end_desire_{Grade(gap)}" : $"inv_end_virtue_{Grade(-gap)}";
    }

    // 곧바로 끝나는 엔딩: (보유 중 사망) → 사망, 물려주기, 명망 0, 신도 0 (유예 포함). 행동·이벤트 뒤에 확인한다
    public static Endings.Ending ForcedEnding()
    {
        var branch = GameSession.CountOf(Branch);
        if (branch == BranchKeep && GameSession.Dead) return Find("inv_end_master_death"); // 보유를 골랐으면 무엇보다 먼저
        if (GameSession.Dead)
            return GameSession.CultFounded ? Find("inv_end_old_age") : Endings.Find(Endings.DeathYoung);
        if (!GameSession.CultFounded) return null;
        if (branch == BranchHandover) return Find("inv_end_handover");
        if (Turns.CultDissolved) return Find("inv_end_absorbed");
        return FollowersGone() ? Find("inv_end_followers") : null;
    }

    static bool FollowersGone()
    {
        var turns = GameSession.CultTime.Turns;
        if (GameSession.CultTime.Years < FollowersFromYears) return false;
        var start = GameSession.CountOf(GraceStart);
        if (GameSession.Cult[StatId.Followers].Value > 0)
        {
            if (start > 0) { SetCount(GraceStart, 0); SetCount(GraceUsed, 1); }
            return false;
        }
        if (GameSession.CountOf(GraceUsed) > 0) return true;
        if (start == 0) { SetCount(GraceStart, turns + 1); return false; }
        return turns - (start - 1) >= GraceTurns;
    }

    // 행동을 고르는 시점의 엔딩: 최종 스킬 → 25년 판정 (동률이면 2년마다 확인 → 3년 뒤 판정 → 40년 갈림길)
    public static Endings.Ending DueEnding()
    {
        if (!GameSession.CultFounded) return null;
        var cultTime = GameSession.CultTime;
        float desire = Desire, virtue = Virtue;
        var final = GameSession.Cult.Doctrines.Emblem;
        if (final != null && cultTime.Turns >= Endings.MinTurns
            && GameSession.Personal[Endings.StatOf(final.Branch)].Value > Progress.BranchThreshold(GameSession.Run))
            return Find(FinalId(final, desire, virtue));
        if (cultTime.Years < JudgeYears || GameSession.CountOf(Branch) != 0) return null; // 갈림길 뒤에는 연수 판정이 없다

        var judgeAt = GameSession.CountOf(JudgeAt);
        if (judgeAt > 0) return cultTime.Turns >= judgeAt - 1 ? Find(SideId(desire, virtue)) : null;
        var checkedYear = GameSession.CountOf(TieChecked);
        if (checkedYear == 0)
        {
            if (GapTenths(desire, virtue) != 0) return Find(SideId(desire, virtue));
            SetCount(TieChecked, cultTime.Years);
            return null;
        }
        if (cultTime.Years >= checkedYear + CheckEveryYears && cultTime.Years < BranchYears)
        {
            SetCount(TieChecked, cultTime.Years);
            if (System.Math.Abs(GapTenths(desire, virtue)) >= TieBreakGap * 10)
                SetCount(JudgeAt, cultTime.Turns + DelayYears * GameClock.QuartersPerYear + 1);
            return null;
        }
        if (cultTime.Years >= BranchYears) SetCount(Branch, BranchPending); // 다음 이벤트로 갈림길이 나온다
        return null;
    }

    // 갈림길 이벤트의 선택: 1 물려주기, 2 끝까지 보유
    public static void AfterChoice(Choice choice)
    {
        if (!BranchDue) return;
        var choices = MirrorEvents.Ending[0].Question.Choices;
        if (choice == choices[0]) SetCount(Branch, BranchHandover);
        else if (choice == choices[1]) SetCount(Branch, BranchKeep);
    }
}
