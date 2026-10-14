using System;
using System.Collections.Generic;

// 교리 스킬 정의. 교리 선포가 중심이고, 이후 교리는 여기에 추가한다.
public class DoctrineSkill
{
    public const string DeclarationId = "declaration";

    public string Id { get; }
    public int MaxLevel { get; }

    public DoctrineSkill(string id, int maxLevel) { Id = id; MaxLevel = maxLevel; }

    public string Name => Lang.Get($"doctrine_{Id}");

    public static readonly DoctrineSkill Declaration = new(DeclarationId, 1);
}

// 교단이 배운 교리와 레벨.
public class DoctrineTree
{
    readonly Dictionary<string, int> levels = new();
    public event Action<DoctrineSkill> Changed;

    public IEnumerable<KeyValuePair<string, int>> Levels => levels;

    // 저장에서 불러올 때 쓴다 (변경 이벤트 없음)
    public void Restore(string skillId, int level) => levels[skillId] = level;

    public int LevelOf(DoctrineSkill skill) => levels.TryGetValue(skill.Id, out var level) ? level : 0;
    public bool IsMax(DoctrineSkill skill) => LevelOf(skill) >= skill.MaxLevel;

    public bool LevelUp(DoctrineSkill skill)
    {
        if (IsMax(skill)) return false;
        levels[skill.Id] = LevelOf(skill) + 1;
        Changed?.Invoke(skill);
        return true;
    }
}
