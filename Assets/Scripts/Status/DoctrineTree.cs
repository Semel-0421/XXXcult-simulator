using System;
using System.Collections.Generic;
using System.Linq;

// 교리 가지: 욕망 넷(음욕·재물욕·식욕·권력욕)과 덕목 넷(절제·자애·인내·청렴). 개인 스탯 이름과 같다.
public enum DoctrineBranch { None, Lust, Greed, Appetite, Ambition, Temperance, Charity, Patience, Integrity }

public static class DoctrineBranches
{
    // 스킬트리에서 정반대 방향에 놓인 가지 (절제↔음욕, 자애↔재물욕, 인내↔식욕, 청렴↔권력욕)
    public static DoctrineBranch Opposite(this DoctrineBranch branch) => branch switch
    {
        DoctrineBranch.Lust => DoctrineBranch.Temperance,
        DoctrineBranch.Temperance => DoctrineBranch.Lust,
        DoctrineBranch.Greed => DoctrineBranch.Charity,
        DoctrineBranch.Charity => DoctrineBranch.Greed,
        DoctrineBranch.Appetite => DoctrineBranch.Patience,
        DoctrineBranch.Patience => DoctrineBranch.Appetite,
        DoctrineBranch.Ambition => DoctrineBranch.Integrity,
        DoctrineBranch.Integrity => DoctrineBranch.Ambition,
        _ => DoctrineBranch.None,
    };
}

// 교리 스킬 하나. 교리 선포가 중심이고, 나머지 120개는 DoctrineCatalog(생성 코드)에 있다.
//  - 가지마다 1~4단계 → 5단계(여러 번 올리는 스킬) → 6~10단계는 A/B 두 갈래 (한 갈래만 선택)
//  - 10단계는 최종 스킬(가지 8 x 갈래 2 = 16개). 배우면 그 문양이 중앙에 오르고, 중앙에서 최종 업그레이드(2레벨)를 한다
// 효과는 레벨당 값이며, 레벨만큼 곱해 기프티드와 같은 방식으로 적용된다.
public class DoctrineSkill
{
    public const string DeclarationId = "declaration";
    public const int FinalUpgradeCost = 6; // 최종 스킬 2레벨(최종 업그레이드) 비용

    public string Id { get; }
    public DoctrineBranch Branch { get; }
    public int Tier { get; }
    public string Path { get; }                 // "" (1~5단계) / "A" / "B"
    public int MaxLevel { get; }
    readonly string description;
    public string Description => Lang.Pick(description);
    public int ResistPercent { get; }           // 레벨당 불운 저항(%)
    public StatModifier[] Modifiers { get; }    // 레벨당 스탯 보정
    public GiftedSpecial[] Specials { get; }    // 레벨당 특수효과
    readonly string name;

    public DoctrineSkill(string id, DoctrineBranch branch, int tier, string path, string name, string description, int maxLevel,
        int resistPercent, StatModifier[] modifiers, params GiftedSpecial[] specials)
    {
        Id = id; Branch = branch; Tier = tier; Path = path; this.name = name; this.description = description; MaxLevel = maxLevel;
        ResistPercent = resistPercent; Modifiers = modifiers; Specials = specials;
    }

    DoctrineSkill(string id, int maxLevel) : this(id, DoctrineBranch.None, 0, "", null, "", maxLevel, 0, Array.Empty<StatModifier>()) { }

    public string Name => name != null ? Lang.Pick(name) : Lang.Get($"doctrine_{Id}");
    public bool IsDesire => Branch is DoctrineBranch.Lust or DoctrineBranch.Greed or DoctrineBranch.Appetite or DoctrineBranch.Ambition;
    public bool IsFinal => Tier == 10;
    public bool Repeatable => MaxLevel > 1 && !IsFinal;

    // 한 레벨을 올리는 데 드는 포인트: 1~3단계 1, 4~6단계 2, 7~9단계 3, 10단계 4 (교리 선포는 0)
    public int Cost => Tier <= 0 ? 0 : Tier >= 10 ? 4 : 1 + (Tier - 1) / 3;

    // level번째 레벨을 올리는 비용 (최종 스킬의 2레벨은 최종 업그레이드 비용)
    public int CostForLevel(int level) => IsFinal && level >= 2 ? FinalUpgradeCost : Cost;

    public static readonly DoctrineSkill Declaration = new(DeclarationId, 1);
}

// 교단이 배운 교리와 레벨. 배운 교리는 기프티드처럼 스탯 보정·특수효과를 준다(EffectTalents).
public class DoctrineTree
{
    readonly Dictionary<string, int> levels = new();
    List<Talent> effectCache;
    public event Action<DoctrineSkill> Changed;

    // 중앙 교리 선포 자리에 오른 최종 스킬 (가장 최근에 배운 최종 스킬). 없으면 null
    public string EmblemId { get; private set; }
    public DoctrineSkill Emblem => EmblemId == null ? null : DoctrineCatalog.Find(EmblemId);

    public void RestoreEmblem(string id) => EmblemId = string.IsNullOrEmpty(id) ? null : id;

    public IEnumerable<KeyValuePair<string, int>> Levels => levels;

    // 저장에서 불러올 때 쓴다 (변경 이벤트 없음)
    public void Restore(string skillId, int level)
    {
        levels[skillId] = level;
        effectCache = null;
    }

    public int LevelOf(DoctrineSkill skill) => levels.TryGetValue(skill.Id, out var level) ? level : 0;
    public bool IsMax(DoctrineSkill skill) => LevelOf(skill) >= skill.MaxLevel;

    // 교리 선포를 익혔고, 같은 가지(갈래)의 앞 단계를 배웠어야 한다. 6단계에서 다른 갈래를 골랐으면 막힌다.
    public bool PrerequisitesMet(DoctrineSkill skill)
    {
        if (skill == DoctrineSkill.Declaration) return true;
        if (!IsMax(DoctrineSkill.Declaration)) return false;
        if (skill.Tier <= 1) return true;
        if (IsPathBlocked(skill) || IsOppositeLocked(skill)) return false;
        var previous = DoctrineCatalog.At(skill.Branch, skill.Tier - 1, skill.Path);
        return previous != null && LevelOf(previous) > 0;
    }

    // 한 가지에서 6단계를 찍었다면, 그 정반대 가지의 5단계 이상은 새로 배울 수 없다
    public const int OppositeLockTier = 5;

    public bool IsOppositeLocked(DoctrineSkill skill)
    {
        if (skill.Tier < OppositeLockTier || skill.Branch == DoctrineBranch.None) return false;
        var opposite = skill.Branch.Opposite();
        for (int tier = 6; tier <= 10; tier++)
        {
            foreach (var path in new[] { "A", "B" })
            {
                var chosen = DoctrineCatalog.At(opposite, tier, path);
                if (chosen != null && LevelOf(chosen) > 0) return true;
            }
        }
        return false;
    }

    // 같은 가지의 다른 갈래를 이미 골랐는가
    public bool IsPathBlocked(DoctrineSkill skill)
    {
        if (skill.Path == "") return false;
        var other = DoctrineCatalog.At(skill.Branch, 6, skill.Path == "A" ? "B" : "A");
        return other != null && LevelOf(other) > 0;
    }

    // 회차가 낮아 최종 스킬(10단계)을 아직 배울 수 없음 (Progress.ApplyRun이 정한다)
    public bool FinalLocked { get; set; }

    public bool IsFinalLocked(DoctrineSkill skill) => skill.IsFinal && FinalLocked;

    // 스킬트리 노드에서 배우기. 최종 스킬은 1레벨까지만 (2레벨은 중앙 최종 업그레이드)
    public bool CanLearn(DoctrineSkill skill, int availablePoints)
    {
        var level = LevelOf(skill);
        if (skill.IsFinal ? level >= 1 : IsMax(skill)) return false;
        if (IsFinalTaken(skill) || IsFinalLocked(skill)) return false;
        return PrerequisitesMet(skill) && availablePoints >= skill.CostForLevel(level + 1);
    }

    public bool CanUpgradeFinal(int availablePoints)
    {
        var emblem = Emblem;
        return emblem != null && LevelOf(emblem) == 1 && availablePoints >= emblem.CostForLevel(2);
    }

    public bool UpgradeFinal(int availablePoints) => CanUpgradeFinal(availablePoints) && LevelUp(Emblem);

    public int SpentPoints
    {
        get
        {
            int total = 0;
            foreach (var pair in levels)
            {
                var skill = DoctrineCatalog.Find(pair.Key);
                if (skill == null) continue;
                for (int level = 1; level <= pair.Value; level++) total += skill.CostForLevel(level);
            }
            return total;
        }
    }

    // 최종 스킬은 한 판에 하나만 고를 수 있다: 이미 다른 최종 스킬을 골랐는가
    public bool IsFinalTaken(DoctrineSkill skill) => skill.IsFinal && EmblemId != null && EmblemId != skill.Id;

    public bool LevelUp(DoctrineSkill skill)
    {
        if (IsMax(skill) || IsFinalTaken(skill)) return false;
        levels[skill.Id] = LevelOf(skill) + 1;
        if (skill.IsFinal && levels[skill.Id] == 1) EmblemId = skill.Id; // 새 최종 스킬이 중앙 문양을 대체
        effectCache = null;
        Changed?.Invoke(skill);
        return true;
    }

    // 배운 교리 효과 합계 (불운 저항, 0~1)
    public float Resistance
    {
        get
        {
            int percent = 0;
            foreach (var pair in levels)
                percent += (DoctrineCatalog.Find(pair.Key)?.ResistPercent ?? 0) * pair.Value;
            return percent / 100f;
        }
    }

    // 배운 교리를 레벨만큼 곱한 효과 묶음으로 만든다. 기프티드 효과 계산이 이 목록도 함께 본다.
    public IReadOnlyList<Talent> EffectTalents => effectCache ??= BuildEffects();

    List<Talent> BuildEffects()
    {
        var list = new List<Talent>();
        foreach (var pair in levels)
        {
            var skill = DoctrineCatalog.Find(pair.Key);
            if (skill == null || skill == DoctrineSkill.Declaration || pair.Value <= 0) continue;
            list.Add(ToTalent(skill, pair.Value));
        }
        return list;
    }

    public static Talent ToTalent(DoctrineSkill skill, int level)
    {
        var modifiers = skill.Modifiers.Select(m => m.PersonalStat is { } p
            ? new StatModifier(p, m.Percent * level)
            : new StatModifier(m.CultStat.Value, m.Percent * level)).ToArray();
        var specials = skill.Specials.Select(s => Scale(s, level)).ToArray();
        return new Talent(skill.Id, skill.Name, "교리", skill.Description, "", modifiers, specials);
    }

    static GiftedSpecial Scale(GiftedSpecial s, int level) => s.Kind switch
    {
        GiftedSpecial.Type.Chance => GiftedSpecial.Chance(s.Action, s.Amount * level),
        GiftedSpecial.Type.Yearly when s.PersonalStat is { } p => GiftedSpecial.Yearly(p, s.Amount * level),
        GiftedSpecial.Type.Yearly when s.CultStat is { } c => GiftedSpecial.Yearly(c, s.Amount * level),
        _ => s,
    };
}

// 교리 포인트: 교단 운영 1년마다 1, 달성한 업적 2개마다 1, 교리 레벨마다 2. 교리를 배우면 단계에 따라 소모된다.
public static class DoctrinePoints
{
    public const int PerCultYear = 1, AchievementsPerPoint = 2, PerDoctrineLevel = 2;
    public const int MirrorPerCultYear = 2; // 거울 세계: 최종 스킬(총 22점)에 닿도록 해마다 2점

    public static int Earned(int cultYears, int achievements, int doctrineLevel) =>
        cultYears * (GameSession.MirrorMode ? MirrorPerCultYear : PerCultYear) + achievements / AchievementsPerPoint + doctrineLevel * PerDoctrineLevel;

    public static int Earned(CultStatus cult, GameClock cultTime, int achievements) =>
        Earned(cultTime.Years, achievements, DoctrineLevel.Of(cult[StatId.Doctrine].Value));

    public static int Available(CultStatus cult, GameClock cultTime, int achievements) =>
        Math.Max(0, Earned(cult, cultTime, achievements) - cult.Doctrines.SpentPoints);

    public static int Available() => Available(GameSession.Cult, GameSession.CultTime, Achievements.CountedUnlocked());
}
