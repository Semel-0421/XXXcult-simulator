using System;
using System.Collections.Generic;
using System.Linq;

// 아이온 트리 진행 상태 (SYS_DEMIURGE 6-2·6-3·6-5, DEC-026~028).
//  - 시작: 첫 질문에서 고른 섹터의 0단계 선·악 두 노드를 무료로 얻는다 (Grant).
//  - 교리 단계(DoctrineLevel)가 노드의 단계 이상이어야 익힐 수 있다. 단계가 떨어져도 익힌 노드는 남는다.
//  - 선·악은 같은 줄의 바로 아래 단계를 익혀야 열린다(0단계는 조건 없음). 그노시스(5)는 그 섹터 4단계 선·악을 둘 다, 6~9는 바로 아래 그노시스 단계를 요구한다.
//  - 9단계(플레로마의 문턱)를 먼저 익힌 섹터만 승천(10)이 열린다. 승천은 교리 10단계여야 익힐 수 있다.
public class AeonTree
{
    readonly HashSet<string> learned = new();
    readonly List<EyeSector> thresholdOrder = new(); // 9단계를 익힌 섹터 순서

    public event Action<AeonSkill> Changed;

    public EyeSector StartSector { get; private set; }
    public IEnumerable<string> Learned => learned;
    public IReadOnlyList<EyeSector> ThresholdOrder => thresholdOrder;
    public int Version { get; private set; }

    public bool IsLearned(AeonSkill skill) => skill != null && learned.Contains(skill.Id);
    public bool IsFree(AeonSkill skill) => skill != null && skill.Level == 0 && skill.Sector == StartSector && StartSector != EyeSector.None;

    // 승천할 섹터: 가장 먼저 9단계에 이른 섹터 (DEC-028 B)
    public EyeSector AscensionSector => thresholdOrder.Count > 0 ? thresholdOrder[0] : EyeSector.None;
    public bool Ascended => learned.Any(id => AeonSkillCatalog.Find(id)?.Kind == AeonKind.Ascension);

    public void Grant(EyeSector sector)
    {
        if (sector == EyeSector.None || StartSector != EyeSector.None) return;
        StartSector = sector;
        Add(AeonSkillCatalog.Of(sector, 0, AeonKind.Good));
        Add(AeonSkillCatalog.Of(sector, 0, AeonKind.Evil));
    }

    public bool PrerequisitesMet(AeonSkill skill)
    {
        if (skill == null) return false;
        switch (skill.Kind)
        {
            case AeonKind.Good:
            case AeonKind.Evil:
                return skill.Level == 0 || IsLearned(AeonSkillCatalog.Of(skill.Sector, skill.Level - 1, skill.Kind));
            case AeonKind.Gnosis:
                return skill.Level == AeonSkill.GnosisLevel
                    ? IsLearned(AeonSkillCatalog.Of(skill.Sector, 4, AeonKind.Good)) && IsLearned(AeonSkillCatalog.Of(skill.Sector, 4, AeonKind.Evil))
                    : IsLearned(AeonSkillCatalog.Stage(skill.Sector, skill.Level - 1));
            default:
                return AscensionSector == skill.Sector && IsLearned(AeonSkillCatalog.Stage(skill.Sector, AeonSkill.ThresholdLevel));
        }
    }

    // 익힐 수 없는 이유 (익힐 수 있으면 null): learned / start / prereq / ascension / doctrine / points
    public string Blocker(AeonSkill skill, int points, int doctrineLevel)
    {
        if (skill == null) return "prereq";
        if (IsLearned(skill)) return "learned";
        if (StartSector == EyeSector.None) return "start";
        if (skill.Kind == AeonKind.Ascension && AscensionSector != EyeSector.None && AscensionSector != skill.Sector) return "ascension";
        if (!PrerequisitesMet(skill)) return "prereq";
        if (doctrineLevel < skill.DoctrineNeed) return "doctrine";
        if (points < skill.Cost) return "points";
        return null;
    }

    public bool CanLearn(AeonSkill skill, int points, int doctrineLevel) => Blocker(skill, points, doctrineLevel) == null;

    public bool Learn(AeonSkill skill, int points, int doctrineLevel)
    {
        if (!CanLearn(skill, points, doctrineLevel)) return false;
        Add(skill);
        return true;
    }

    void Add(AeonSkill skill)
    {
        if (skill == null || !learned.Add(skill.Id)) return;
        if (skill.Level == AeonSkill.ThresholdLevel && !thresholdOrder.Contains(skill.Sector)) thresholdOrder.Add(skill.Sector);
        Version++;
        Changed?.Invoke(skill);
    }

    IEnumerable<AeonSkill> Skills => learned.Select(AeonSkillCatalog.Find).Where(s => s != null);

    // 시작 섹터의 0단계 두 노드는 무료
    public int SpentPoints => Skills.Where(s => !IsFree(s)).Sum(s => s.Cost);

    public int HighestLevel => learned.Count == 0 ? -1 : Skills.Max(s => s.Level);

    // 권능 트리 단계(1~5)에 대응: 건물 레벨 상한 등에 쓴다. 0단계 → 1, 2 → 2, 4 → 3, 6 → 4, 8 이상 → 5
    public int Stage => learned.Count == 0 ? 0 : Math.Min(5, 1 + HighestLevel / 2);

    public float PressureWeight => Skills.Sum(s => s.PressureWeight);

    // 분기마다 쌓는 재앙 카운터와 재앙 피해 감소 % (재앙 처리는 4단계)
    public int CalamityPerQuarter => Skills.Sum(s => s.Calamity);
    public int CalamityGuard => Skills.Sum(s => s.CalamityGuard);

    public bool UnlocksBuilding(GodBuildingType type) => Skills.Any(s => s.Build == type);

    public IEnumerable<(GodEffect effect, int amount)> Effects => Skills.SelectMany(s => s.Effects.Select(e => (e, e.Amount)));

    // ---- 저장 ----

    public void Restore(IEnumerable<string> learnedIds, EyeSector startSector, IEnumerable<int> order)
    {
        learned.Clear();
        thresholdOrder.Clear();
        StartSector = startSector;
        foreach (var id in learnedIds ?? Enumerable.Empty<string>())
            if (AeonSkillCatalog.Find(id) != null) learned.Add(id);
        foreach (var sector in (order ?? Enumerable.Empty<int>()).Select(n => (EyeSector)n))
            if (!thresholdOrder.Contains(sector) && IsLearned(AeonSkillCatalog.Stage(sector, AeonSkill.ThresholdLevel))) thresholdOrder.Add(sector);
        Version++;
        Changed?.Invoke(null);
    }
}
