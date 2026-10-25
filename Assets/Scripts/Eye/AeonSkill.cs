using System;
using System.Collections.Generic;
using System.Linq;

// 황금의 눈동자 모드의 아이온 트리 노드 (SYS_DEMIURGE 6절). 섹터 4개 × (0~4단계 선·악 하나씩 + 5~9단계 그노시스 + 10단계 승천) = 64개.
// 노드 목록은 Docs/eye/aeon_skills.csv → python -I Docs/gen_aeon_skills.py → AeonSkillCatalog.Data.cs.
// 효과는 권능 트리와 같은 GodEffect를 써서 신 모드 계산(증감 보정·성공률·저항·압류 방어 등)에 그대로 더해진다 (GodTree.Aeons).
public enum AeonKind { Good, Evil, Gnosis, Ascension }

public class AeonSkill
{
    public const int GnosisLevel = 5, ThresholdLevel = 9, AscensionLevel = 10;

    public string Id { get; }
    public EyeSector Sector { get; }
    public int Level { get; }            // 열리는 교리 단계 (0~10)
    public AeonKind Kind { get; }
    readonly string name, description;
    public string Name => Lang.Pick(name);
    public string Description => Lang.Pick(description);
    public GodEffect[] Effects { get; }
    public int Calamity { get; }          // 익혀 두면 분기마다 쌓는 재앙 카운터
    public int CalamityGuard { get; }     // 재앙 피해 감소 %
    public GodBuildingType? Build { get; } // 익히면 지을 수 있는 건물

    public AeonSkill(string id, EyeSector sector, int level, AeonKind kind, string name, string description, GodEffect[] effects,
        int calamity, int calamityGuard, GodBuildingType? build)
    {
        Id = id; Sector = sector; Level = level; Kind = kind; this.name = name; this.description = description;
        Effects = effects; Calamity = calamity; CalamityGuard = calamityGuard; Build = build;
    }

    public bool IsAeon => Kind is AeonKind.Good or AeonKind.Evil;

    // 비용 (가안): 아이온 1, 그노시스 단계 2, 승천 3
    public int Cost => Kind switch { AeonKind.Ascension => 3, AeonKind.Gnosis => 2, _ => 1 };

    // 압박 규모에 더하는 가중치: 아이온은 권능 트리 바깥 노드처럼 0.75, 그노시스 이상은 1
    public float PressureWeight => IsAeon ? 0.75f : 1f;
}

public static partial class AeonSkillCatalog
{
    public static readonly IReadOnlyList<AeonSkill> All = Data();
    static readonly Dictionary<string, AeonSkill> byId = All.ToDictionary(s => s.Id);

    public static AeonSkill Find(string id) => id != null && byId.TryGetValue(id, out var s) ? s : null;

    public static AeonSkill Of(EyeSector sector, int level, AeonKind kind) =>
        All.FirstOrDefault(s => s.Sector == sector && s.Level == level && s.Kind == kind);

    // 그 섹터의 단계 노드 (5~10은 하나뿐이라 kind 없이 찾는다)
    public static AeonSkill Stage(EyeSector sector, int level) =>
        All.FirstOrDefault(s => s.Sector == sector && s.Level == level && !s.IsAeon);

    public static IEnumerable<AeonSkill> OfSector(EyeSector sector) => All.Where(s => s.Sector == sector);
}
