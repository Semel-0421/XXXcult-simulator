// 한 판의 흐름 규칙: 회차, 곧바로 끝나는 엔딩(사망·해체), 행동 시점의 엔딩 판정.
// 화면 흐름(LifeFlow/CultFlow)과 밸런스 시뮬레이션이 같은 규칙을 쓰도록 한곳에 모은다.
//
// 회차 = 지금까지 엔딩을 본 횟수 + 1. 회차가 오를수록 최종 스킬 엔딩이 계단식으로 풀린다.
//   1~2회차: 최종 스킬(10단계)을 배울 수 없다 → 해체·극단·사망 엔딩으로 시스템을 익힌다
//   3회차: 최종 스킬 해금, 가지 수치 문턱 95 (반대쪽 평균과 맞물리는 쉬운 쪽만 가끔 나온다)
//   4회차: 문턱 90, 엔딩 목록에 본 최종 엔딩의 판정 조건이 공개된다
//   5회차~: 문턱 75, 상태창에 덕목·욕망 평균이 보인다 → 굿/배드를 골라서 볼 수 있다
//   그리고 마친 판마다 덕목·욕망 8개 스탯이 TraitBonusPerRun씩 더 높게 시작한다 (5회차에 최대 MaxTraitBonus)
//   수치는 BalanceSimulation(목표 엔딩을 노리는 봇 200명 × 12회차)으로 맞췄다: 목표 엔딩 첫 달성 중앙값 5회차.
public static class Progress
{
    public const int FinalSkillRun = 3;
    public const int ConditionHintRun = 4;
    public const int AverageRun = 5;
    public const int TraitBonusPerRun = 8;
    public const int MaxTraitBonus = 32;

    static readonly PersonalStatId[] TraitStats =
    {
        PersonalStatId.Lust, PersonalStatId.Greed, PersonalStatId.Appetite, PersonalStatId.Ambition,
        PersonalStatId.Temperance, PersonalStatId.Charity, PersonalStatId.Patience, PersonalStatId.Integrity,
    };

    // 끝까지 마친 판 수와 다음에 시작할 회차 (판을 넘어 남는 기록 기준)
    public static int CompletedRuns => Endings.ReachedCount;
    public static int NextRun => CompletedRuns + 1;

    // 지금 보고 있는 회차: 판 안에서는 그 판의 회차, 회차 기록이 없으면(옛 저장·타이틀) 다음 회차
    public static int CurrentRun => GameSession.Run > 0 ? GameSession.Run : NextRun;

    // 회차별 최종 스킬 엔딩의 가지 수치 문턱 (0 = 회차 제한 없음)
    public static int BranchThreshold(int run) => run switch
    {
        <= 0 => Endings.BranchStatAbove,
        <= 3 => 95,
        4 => 90,
        _ => Endings.BranchStatAbove,
    };

    public static bool FinalSkillUnlocked(int run) => run <= 0 || run >= FinalSkillRun;
    public static int TraitBonus(int run) => run <= 1 ? 0 : System.Math.Min(MaxTraitBonus, (run - 1) * TraitBonusPerRun);
    public static bool ShowsConditions => CurrentRun >= ConditionHintRun;
    public static bool ShowsAverages => CurrentRun >= AverageRun;

    // 새 판을 시작할 때: 회차를 정하고 그 회차의 시작 보너스를 준다
    public static void BeginRun(int run)
    {
        ApplyRun(run);
        var bonus = TraitBonus(run);
        if (bonus <= 0) return;
        foreach (var id in TraitStats) GameSession.Personal[id].Add(bonus);
    }

    // 회차에 따른 잠금만 다시 맞춘다 (불러오기)
    public static void ApplyRun(int run)
    {
        GameSession.SetRun(run);
        GameSession.Cult.Doctrines.FinalLocked = !FinalSkillUnlocked(run);
    }

    // 가장 먼저 0/최대에 이른 교단 스탯을 기록해 둔다 (언제든)
    public static void Track()
    {
        if (!GameSession.CultFounded) return;
        foreach (StatId id in System.Enum.GetValues(typeof(StatId)))
            if (GameSession.Cult[id].Value > 0) GameSession.MarkRaised(id);
        if (GameSession.FirstExtremeId != null) return;
        var extreme = Endings.FindExtreme(GameSession.Cult, GameSession.RaisedMask);
        if (extreme != null) GameSession.RecordExtreme(Endings.ExtremeId(extreme.Value.stat, extreme.Value.high));
    }

    // 곧바로 판이 끝나는 엔딩: 주인공 사망, 또는 명망이 바닥나 해체
    public static Endings.Ending ForcedEnding()
    {
        if (GameSession.GodMode) return GodRules.ForcedEnding();
        if (GameSession.Dead) return Endings.Find(GameSession.CultFounded ? Endings.DeathLeader : Endings.DeathYoung);
        return GameSession.CultFounded && Turns.CultDissolved ? Endings.Find(Endings.Dissolved) : null;
    }

    public static bool IsDeath(Endings.Ending ending) => ending.Id is Endings.DeathYoung or Endings.DeathLeader;

    // 행동을 고르는 시점의 엔딩 판정
    //  - 교단 10년(40턴) 이후부터: 최종 스킬을 고르고 그 가지 수치가 회차 문턱을 넘으면 굿/배드 엔딩.
    //  - 그 조건이 아직이면 25년이 지났을 때 가장 먼저 극단에 이른 교단 스탯의 엔딩.
    public static Endings.Ending DueEnding()
    {
        if (GameSession.GodMode) return GodRules.DueEnding();
        Track();
        if (!GameSession.CultFounded || GameSession.CultTime.Turns < Endings.MinTurns) return null;
        var final = GameSession.Cult.Doctrines.Emblem;
        if (final != null)
        {
            var kind = Endings.Decide(GameSession.Personal, final.Branch, BranchThreshold(GameSession.Run));
            var ending = kind == null ? null : Endings.ForFinal(final, kind.Value);
            if (ending != null) return ending;
        }
        if (GameSession.CultTime.Years >= Endings.ExtremeYears && GameSession.FirstExtremeId != null)
            return Endings.Find(GameSession.FirstExtremeId);
        return null;
    }

    // 엔딩에 도달: 기록을 남기고 업적을 확인한다
    public static void Reach(Endings.Ending ending)
    {
        if (ending.Id == Endings.Dissolved) Profile.Count("dissolved");
        Endings.Reach(ending.Id);
        Achievements.Evaluate();
    }

    // 엔딩 화면에 보여 줄 글 ({0} 교단 이름, {1} 교단 운영 연수, {2} 최종 스킬 이름 또는 사망 나이)
    public static string TextOf(Endings.Ending ending)
    {
        var cult = GameSession.Cult.Name;
        var years = GameSession.CultTime.Years;
        if (GameSession.GodMode) return string.Format(Lang.Get(ending.TextKey), cult, years); // 회차·유산이 없다
        string text;
        if (ending.Id == Endings.Dissolved) text = string.Format(Lang.Get("ending_dissolved"), cult, years);
        else if (IsDeath(ending)) text = string.Format(Lang.Get(ending.TextKey), cult, years, GameSession.Age.Years);
        else if (ending.Final != null) text = string.Format(Lang.Get(ending.TextKey), cult, years, ending.Final.Name);
        else text = string.Format(Lang.Get(ending.TextKey), cult, years);
        return $"{text}\n\n<color=#F2D978>{Lang.Get("ending_legacy")}\n{Legacy.Describe()}\n{string.Format(Lang.Get("run_next"), NextRun)}</color>";
    }

    // 엔딩 목록에 보여 줄 판정 조건 (4회차부터, 본 최종 스킬 엔딩만)
    public static string ConditionOf(Endings.Ending ending)
    {
        if (ending.Final == null) return "";
        var branch = ending.Final.Branch;
        var desire = Endings.IsDesireBranch(branch);
        var opposite = Lang.Get(desire ? "trait_virtue" : "trait_desire");
        var above = desire ? ending.Kind == EndingKind.Good : ending.Kind == EndingKind.Bad;
        return string.Format(Lang.Get(above ? "end_cond_above" : "end_cond_below"),
            Lang.Get($"pstat_{Endings.StatOf(branch)}"), BranchThreshold(CurrentRun), opposite, (int)Endings.OppositeAverage);
    }
}
