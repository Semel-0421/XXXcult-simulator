using System;
using System.Collections.Generic;
using System.Linq;

// 준비된 신 모드의 권능 트리. 바깥 고리(1)에서 시작해 안쪽(5: 핵)으로 힘을 모으고, 마지막에 중심(0: 승천)에 이른다.
// 가지 넷: 신격(기적·예언)과 인격(자비·욕망). 노드 목록은 Docs/god_skills.csv → GodSkillCatalog.Data.cs.
public enum GodBranch { None, Miracle, Prophecy, Mercy, Desire }
public enum GodSkillKind { Center, Core, Group, Stat, Attack, Defense }
public enum Pressure { Heaven, Earth }

// 이 모드의 행동. 성공률 효과(chance)는 이 이름이나 All로 대상을 정한다.
public enum GodAction { Wonder, Revelation, Healing, Feast, Commune, Consecrate }

public readonly struct GodEffect
{
    // Gain: 증가량 +% (공격형) / Guard: 감소량 -% (방어형) / Chance: 행동 성공률 +%p / Resist: 압박 가산치 상승 -%
    // SeizeGuard: 압류 방어 확률 % / CommunionSave: 기적의 접신 소모 -% / VesselRecovery: 향연의 그릇 회복 +%
    // FaithPoints: 신앙 비례 포인트 +% / Yearly: 해마다 스탯 변화
    public enum Type { Gain, Guard, Chance, Resist, SeizeGuard, CommunionSave, VesselRecovery, FaithPoints, Yearly }
    public const string AllActions = "All";

    public Type Kind { get; }
    public int Amount { get; }
    public StatId? CultStat { get; }
    public PersonalStatId? PersonalStat { get; }
    public string Action { get; }
    public Pressure? PressureKind { get; }

    GodEffect(Type kind, int amount, StatId? cult = null, PersonalStatId? personal = null, string action = null, Pressure? pressure = null)
    {
        Kind = kind; Amount = amount; CultStat = cult; PersonalStat = personal; Action = action; PressureKind = pressure;
    }

    public static GodEffect Gain(StatId stat, int percent) => new(Type.Gain, percent, cult: stat);
    public static GodEffect Gain(PersonalStatId stat, int percent) => new(Type.Gain, percent, personal: stat);
    public static GodEffect Guard(StatId stat, int percent) => new(Type.Guard, percent, cult: stat);
    public static GodEffect Guard(PersonalStatId stat, int percent) => new(Type.Guard, percent, personal: stat);
    public static GodEffect Chance(string action, int percentPoints) => new(Type.Chance, percentPoints, action: action);
    public static GodEffect Resist(Pressure pressure, int percent) => new(Type.Resist, percent, pressure: pressure);
    public static GodEffect Yearly(StatId stat, int amount) => new(Type.Yearly, amount, cult: stat);
    public static GodEffect Yearly(PersonalStatId stat, int amount) => new(Type.Yearly, amount, personal: stat);
    public static GodEffect Of(Type kind, int amount) => new(kind, amount);

    public bool Covers(GodAction action) => Action == AllActions || Action == action.ToString();
}

public class GodSkill
{
    public const string AscensionId = "ascension";

    public string Id { get; }
    public int Ring { get; }               // 0 중심, 1 가장 바깥 ~ 5 핵
    public string ParentId { get; }
    public GodSkillKind Kind { get; }
    public string Name { get; }
    public string Description { get; }
    public GodEffect[] Effects { get; }

    // GodSkillCatalog가 목록을 만들 때 채운다
    public GodSkill Parent { get; internal set; }
    public IReadOnlyList<GodSkill> Children { get; internal set; } = Array.Empty<GodSkill>();
    public GodBranch Branch { get; internal set; }

    public GodSkill(string id, int ring, string parentId, GodSkillKind kind, string name, string description, GodEffect[] effects)
    {
        Id = id; Ring = ring; ParentId = parentId; Kind = kind; Name = name; Description = description; Effects = effects;
    }

    public bool IsCenter => Kind == GodSkillKind.Center;
    public bool IsCore => Kind == GodSkillKind.Core;
    public bool IsOuter => Ring == 1;
    public bool IsDivine => Branch is GodBranch.Miracle or GodBranch.Prophecy;

    // 고리 단계: 1 그릇, 2 잔영, 3 화신, 4 반신, 5 신, 6 승천(중심)
    public int Stage => IsCenter ? 6 : Ring;

    // 노드 하나를 익히는 비용: 일반 노드 1, 핵 2, 승천 3
    public int Cost => IsCenter ? 3 : IsCore ? 2 : 1;
}

public static partial class GodSkillCatalog
{
    public static readonly IReadOnlyList<GodSkill> All = Link(Data());
    static Dictionary<string, GodSkill> byId;

    public static GodSkill Ascension => Find(GodSkill.AscensionId);

    public static GodSkill Find(string id) => id != null && byId.TryGetValue(id, out var skill) ? skill : null;

    public static IEnumerable<GodSkill> Cores => All.Where(s => s.IsCore);

    // 부모·자식·가지를 연결한다. 핵의 id(c_miracle 등)가 그 아래 모든 노드의 가지를 정한다.
    static IReadOnlyList<GodSkill> Link(GodSkill[] skills)
    {
        byId = skills.ToDictionary(s => s.Id);
        foreach (var skill in skills)
        {
            skill.Parent = string.IsNullOrEmpty(skill.ParentId) ? null : byId[skill.ParentId];
            skill.Children = skills.Where(s => s.ParentId == skill.Id).ToArray();
        }
        foreach (var skill in skills)
        {
            var node = skill;
            while (node != null && !node.IsCore) node = node.Parent;
            skill.Branch = node == null ? GodBranch.None : BranchOf(node.Id);
        }
        return skills;
    }

    static GodBranch BranchOf(string coreId) => coreId switch
    {
        "c_miracle" => GodBranch.Miracle,
        "c_prophecy" => GodBranch.Prophecy,
        "c_mercy" => GodBranch.Mercy,
        "c_desire" => GodBranch.Desire,
        _ => GodBranch.None,
    };
}
