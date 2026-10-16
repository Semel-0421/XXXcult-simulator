using System.Collections.Generic;

// 권능 노드의 효과를 화면에 보여 줄 문장으로 바꾼다
public static class GodSkillInfo
{
    public static IEnumerable<string> EffectLines(GodSkill skill)
    {
        foreach (var e in skill.Effects)
            yield return e.Kind switch
            {
                GodEffect.Type.Gain => string.Format(Lang.Get("god_eff_gain"), StatName(e), skill.AmountOf(e)),
                GodEffect.Type.Guard => string.Format(Lang.Get("god_eff_guard"), StatName(e), skill.AmountOf(e)),
                GodEffect.Type.Chance => string.Format(Lang.Get("god_eff_chance"), Lang.Get($"god_act_{e.Action}"), skill.AmountOf(e)),
                GodEffect.Type.Resist => string.Format(Lang.Get("god_eff_resist"), Lang.Get($"god_pressure_{e.PressureKind}"), skill.AmountOf(e)),
                GodEffect.Type.SeizeGuard => string.Format(Lang.Get("god_eff_seize"), skill.AmountOf(e)),
                GodEffect.Type.CommunionSave => string.Format(Lang.Get("god_eff_commune"), skill.AmountOf(e)),
                GodEffect.Type.VesselRecovery => string.Format(Lang.Get("god_eff_vessel"), skill.AmountOf(e)),
                GodEffect.Type.FaithPoints => string.Format(Lang.Get("god_eff_faith"), skill.AmountOf(e)),
                _ => string.Format(Lang.Get("god_eff_year"), StatName(e), skill.AmountOf(e) > 0 ? $"+{skill.AmountOf(e)}" : skill.AmountOf(e).ToString()),
            };
        if (GodBuildings.UnlockNodes.TryGetValue(skill.Id, out var building))
            yield return string.Format(Lang.Get("god_eff_build"), Lang.Get($"god_building_{building}"));
        var depth = GodPressure.DepthBonus(skill);
        if (depth > 0 && skill.Branch != GodBranch.None)
            yield return string.Format(Lang.Get("god_eff_resist"), Lang.Get($"god_pressure_{GodPressure.Guards(skill)}"), depth);
    }

    static string StatName(GodEffect e) => e.CultStat is { } c ? Lang.Get($"stat_{c}") : Lang.Get($"pstat_{e.PersonalStat}");

    public static string StageName(int stage) => stage <= 0 ? Lang.Get("god_stage_none") : Lang.Get($"god_stage_{stage}");
}
