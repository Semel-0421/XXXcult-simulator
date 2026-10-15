using System.Collections.Generic;

// 엔딩 목록. 엔딩을 보면 기록에 남고, 엔딩 목록 창에서 지금까지 본 엔딩을 다시 볼 수 있다.
//  - 해체: 명망이 바닥나 국가의 명령으로 교단이 해체됨 (연수 제한 없음)
//  - 최종 스킬 엔딩 32개: 교단을 MinYears년 이상 유지하고 최종 스킬을 고른 뒤, 그 가지의 덕목/욕망 수치가
//    BranchStatAbove를 넘으면 발동. 반대쪽 평균이 OppositeAverage를 넘는지에 따라 굿/배드가 갈린다.
//  - 극단 엔딩 15개: 최종 스킬 엔딩 조건이 충족되지 않은 채(스킬이 없거나, 골랐어도 가지 수치가 75 이하)
//    ExtremeYears년이 지나면, 가장 먼저 0 또는 최대(1000)에 이른 교단 스탯에 해당하는 엔딩.
//    (명망 0은 해체 엔딩이므로 제외)
//  - 사망 2개: 연령별 사망률(Mortality)로 주인공이 세상을 떠남. 교단 창설 전이면 요절, 뒤면 교주의 죽음 (연수 제한 없음)
//  - 최종 스킬 엔딩의 가지 수치 문턱은 회차에 따라 계단식으로 낮아진다 (Progress.BranchThreshold)
// 새 엔딩은 All에 추가하고(언어 키 end_name_/end_desc_/end_text_), 도달 지점에서 Endings.Reach(id)를 부르면 된다.
public enum EndingKind { Neutral, Good, Bad }

public static class Endings
{
    public sealed class Ending
    {
        public string Id { get; }
        public EndingKind Kind { get; }
        public DoctrineSkill Final { get; } // 최종 스킬 엔딩만
        public bool IsExtreme { get; }
        public string NameKey { get; }
        public string DescriptionKey { get; }
        public string TextKey { get; } // {0} 교단 이름, {1} 교단 운영 연수, {2} 최종 스킬 이름(최종 스킬 엔딩만)

        public Ending(string id, EndingKind kind, string keySuffix, DoctrineSkill final = null, bool extreme = false)
        {
            Id = id; Kind = kind; Final = final; IsExtreme = extreme;
            NameKey = $"end_name_{keySuffix}";
            DescriptionKey = $"end_desc_{keySuffix}";
            TextKey = $"end_text_{keySuffix}";
        }
    }

    public const string Dissolved = "dissolved";
    public const string DeathYoung = "death_young";   // 교단 창설 전 사망
    public const string DeathLeader = "death_leader"; // 교단 창설 후 교주 사망
    public const int MinYears = 10;            // 해체를 뺀 모든 엔딩의 최소 교단 유지 연수 (40턴)
    public const int MinTurns = MinYears * GameClock.QuartersPerYear;
    public const int ExtremeYears = 25;        // 이 연수가 지나도 최종 스킬 엔딩이 나오지 않으면 극단 엔딩
    public const int BranchStatAbove = 75;     // 고른 가지의 수치가 이 값을 넘어야 한다 (5회차 이후의 최종 문턱)
    public const float OppositeAverage = 50f;  // 반대쪽(덕목↔욕망) 평균 기준

    // All보다 먼저 선언해야 Build()에서 쓸 수 있다
    static readonly StatId[] ExtremeStats =
    {
        StatId.LeaderFaith, StatId.FollowerFaith, StatId.Authority, StatId.Offerings,
        StatId.Followers, StatId.Doctrine, StatId.Communion, StatId.Reputation,
    };

    public static readonly IReadOnlyList<Ending> All = Build();

    static IReadOnlyList<Ending> Build()
    {
        var list = new List<Ending>
        {
            new Ending(Dissolved, EndingKind.Neutral, Dissolved),
            new Ending(DeathYoung, EndingKind.Neutral, DeathYoung),
            new Ending(DeathLeader, EndingKind.Neutral, DeathLeader),
        };
        foreach (var skill in DoctrineCatalog.All)
        {
            if (!skill.IsFinal) continue;
            foreach (var kind in new[] { EndingKind.Good, EndingKind.Bad })
                list.Add(new Ending(FinalId(skill, kind), kind, $"{skill.Branch}_{(kind == EndingKind.Good ? "good" : "bad")}", skill));
        }
        foreach (var stat in ExtremeStats)
            foreach (var high in new[] { true, false })
            {
                if (stat == StatId.Reputation && !high) continue; // 명망 0은 해체
                list.Add(new Ending(ExtremeId(stat, high), EndingKind.Neutral, $"extreme_{stat}_{(high ? "max" : "min")}", null, true));
            }
        return list;
    }

    public static string FinalId(DoctrineSkill final, EndingKind kind) =>
        $"final_{final.Id}_{(kind == EndingKind.Good ? "good" : "bad")}";

    public static string ExtremeId(StatId stat, bool high) => $"extreme_{stat}_{(high ? "max" : "min")}";

    public static Ending Find(string id)
    {
        foreach (var ending in All)
            if (ending.Id == id) return ending;
        return null;
    }

    public static Ending ForFinal(DoctrineSkill final, EndingKind kind) => Find(FinalId(final, kind));

    public static bool IsDesireBranch(DoctrineBranch branch) =>
        branch is DoctrineBranch.Lust or DoctrineBranch.Greed or DoctrineBranch.Appetite or DoctrineBranch.Ambition;

    public static PersonalStatId StatOf(DoctrineBranch branch) => branch switch
    {
        DoctrineBranch.Lust => PersonalStatId.Lust,
        DoctrineBranch.Greed => PersonalStatId.Greed,
        DoctrineBranch.Appetite => PersonalStatId.Appetite,
        DoctrineBranch.Ambition => PersonalStatId.Ambition,
        DoctrineBranch.Temperance => PersonalStatId.Temperance,
        DoctrineBranch.Charity => PersonalStatId.Charity,
        DoctrineBranch.Patience => PersonalStatId.Patience,
        _ => PersonalStatId.Integrity,
    };

    // 최종 스킬 엔딩 판정: 고른 가지의 수치가 문턱(기본 75)을 넘고, 반대쪽 평균으로 굿/배드를 가른다. 아직이면 null.
    //  - 미덕 가지: 욕망 평균이 50 이하면 굿, 넘으면 배드 (미덕을 지켜 냄 / 겉만 미덕)
    //  - 욕망 가지: 미덕 평균이 50을 넘으면 굿, 이하면 배드 (욕망을 승화함 / 욕망에 잠김)
    public static EndingKind? Decide(PersonalStatus personal, DoctrineBranch branch, int threshold = BranchStatAbove)
    {
        if (personal[StatOf(branch)].Value <= threshold) return null;
        var desire = IsDesireBranch(branch);
        var opposite = CultEvent.TraitAverage(personal, desire ? CultEvent.MoodTrait.Virtue : CultEvent.MoodTrait.Desire);
        var above = opposite > OppositeAverage;
        return desire ? (above ? EndingKind.Good : EndingKind.Bad) : (above ? EndingKind.Bad : EndingKind.Good);
    }

    // 지금 0 또는 최대(1000, 단 최대가 더 작은 스탯은 그 최대)에 이른 교단 스탯. 여럿이면 목록 순서가 앞선 것.
    // 0은 한 번이라도 올랐던 스탯(raisedMask)이 다시 바닥으로 떨어졌을 때만 센다. 시작값이 0인 스탯은 바닥이 아니다.
    public static (StatId stat, bool high)? FindExtreme(CultStatus cult, int raisedMask)
    {
        foreach (var id in ExtremeStats)
        {
            var stat = cult[id];
            if (stat.Value >= System.Math.Min(ClampedStat.DefaultMax, stat.Max)) return (id, true);
            if (id != StatId.Reputation && stat.Value <= stat.Min && (raisedMask & (1 << (int)id)) != 0) return (id, false);
        }
        return null;
    }

    // 목록에 보이는 이름: 엔딩 이름 · 최종 스킬 이름
    public static string DisplayName(Ending ending) =>
        ending.Final == null ? Lang.Get(ending.NameKey) : $"{Lang.Get(ending.NameKey)} · {ending.Final.Name}";

    static string CounterKey(string id) => $"ending_{id}";

    public static bool Seen(string id) => Profile.CountOf(CounterKey(id)) > 0;

    // 지금까지 엔딩을 본 횟수 (같은 엔딩을 여러 번 봐도 모두 센다) = 끝까지 마친 판 수
    public static int ReachedCount
    {
        get
        {
            int count = 0;
            foreach (var ending in All) count += Profile.CountOf(CounterKey(ending.Id));
            return count;
        }
    }

    public static int SeenCount
    {
        get
        {
            int count = 0;
            foreach (var ending in All)
                if (Seen(ending.Id)) count++;
            return count;
        }
    }

    // 엔딩에 도달: 이번 판을 끝난 상태로 표시하고 엔딩 기록을 남긴다
    public static void Reach(string id)
    {
        GameSession.MarkEnded();
        Profile.Count(CounterKey(id));
    }
}
