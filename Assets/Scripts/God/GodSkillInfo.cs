using System.Collections.Generic;

// 권능 노드의 효과를 화면에 보여 줄 문장으로 바꾼다
public static class GodSkillInfo
{
    public static IEnumerable<string> EffectLines(GodSkill skill)
    {
        foreach (var e in skill.Effects)
            yield return e.Kind switch
            {
                GodEffect.Type.Gain => string.Format(Lang.Get("god_eff_gain"), StatName(e), e.Amount),
                GodEffect.Type.Guard => string.Format(Lang.Get("god_eff_guard"), StatName(e), e.Amount),
                GodEffect.Type.Chance => string.Format(Lang.Get("god_eff_chance"), Lang.Get($"god_act_{e.Action}"), e.Amount),
                GodEffect.Type.Resist => string.Format(Lang.Get("god_eff_resist"), Lang.Get($"god_pressure_{e.PressureKind}"), e.Amount),
                GodEffect.Type.SeizeGuard => string.Format(Lang.Get("god_eff_seize"), e.Amount),
                GodEffect.Type.CommunionSave => string.Format(Lang.Get("god_eff_commune"), e.Amount),
                GodEffect.Type.VesselRecovery => string.Format(Lang.Get("god_eff_vessel"), e.Amount),
                GodEffect.Type.FaithPoints => string.Format(Lang.Get("god_eff_faith"), e.Amount),
                _ => string.Format(Lang.Get("god_eff_year"), StatName(e), e.Amount > 0 ? $"+{e.Amount}" : e.Amount.ToString()),
            };
    }

    static string StatName(GodEffect e) => e.CultStat is { } c ? Lang.Get($"stat_{c}") : Lang.Get($"pstat_{e.PersonalStat}");

    public static string StageName(int stage) => stage <= 0 ? Lang.Get("god_stage_none") : Lang.Get($"god_stage_{stage}");
}
