using System.Collections.Generic;

// 권능 노드의 효과를 화면에 보여 줄 문장으로 바꾼다
public static class GodSkillInfo
{
    public static IEnumerable<string> EffectLines(GodSkill skill)
    {
        foreach (var e in skill.Effects) yield return Line(e, skill.AmountOf(e));
        if (GodBuildings.UnlockNodes.TryGetValue(skill.Id, out var building))
            yield return string.Format(Lang.Get("god_eff_build"), Lang.Get($"god_building_{building}"));
        var depth = GodPressure.DepthBonus(skill);
        if (depth > 0 && skill.Branch != GodBranch.None)
            yield return string.Format(Lang.Get("god_eff_resist"), Lang.Get($"god_pressure_{GodPressure.Guards(skill)}"), depth);
    }

    // 효과 한 줄 (아이온 트리도 같이 쓴다: 황금의 눈동자에서는 행동 이름이 eye_act_*)
    public static string Line(GodEffect e, int amount) => e.Kind switch
    {
        GodEffect.Type.Gain => string.Format(Lang.Get("god_eff_gain"), StatName(e), amount),
        GodEffect.Type.Guard => string.Format(Lang.Get("god_eff_guard"), StatName(e), amount),
        GodEffect.Type.Chance => string.Format(Lang.Get("god_eff_chance"), Lang.Get($"{(GameSession.EyeMode ? "eye" : "god")}_act_{e.Action}"), amount),
        GodEffect.Type.Resist => string.Format(Lang.Get("god_eff_resist"), Lang.Get($"god_pressure_{e.PressureKind}"), amount),
        GodEffect.Type.SeizeGuard => string.Format(Lang.Get("god_eff_seize"), amount),
        GodEffect.Type.CommunionSave => string.Format(Lang.Get("god_eff_commune"), amount),
        GodEffect.Type.VesselRecovery => string.Format(Lang.Get("god_eff_vessel"), amount),
        GodEffect.Type.FaithPoints => string.Format(Lang.Get("god_eff_faith"), amount),
        _ => string.Format(Lang.Get("god_eff_year"), StatName(e), amount > 0 ? $"+{amount}" : amount.ToString()),
    };

    static string StatName(GodEffect e) => e.CultStat is { } c ? Lang.Get($"stat_{c}") : Lang.Get($"pstat_{e.PersonalStat}");

    public static string StageName(int stage) => stage <= 0 ? Lang.Get("god_stage_none") : Lang.Get($"god_stage_{stage}");
}
