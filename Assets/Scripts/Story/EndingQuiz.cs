// 엔딩 도감의 수수께끼: 엔딩이 어떤 조건으로 나오는지를 퀴즈 문장으로 적는다. 본 적 없는 엔딩에도 보여 준다.
public static class EndingQuiz
{
    public static string Of(Endings.Ending ending)
    {
        if (ending.Id == Endings.Dissolved) return Lang.Get("quiz_dissolved");
        if (ending.Id == Endings.DeathYoung) return Lang.Get("quiz_death_young");
        if (ending.Id == Endings.DeathLeader) return Lang.Get("quiz_death_leader");
        if (GodRules.Find(ending.Id) != null) return Lang.Get($"quiz_{ending.Id}"); // 준비된 신 엔딩은 하나씩 적는다
        if (ending.Final != null)
        {
            var branch = ending.Final.Branch;
            var desire = Endings.IsDesireBranch(branch);
            var opposite = Lang.Get(desire ? "trait_virtue" : "trait_desire");
            // 욕망 가지 굿·미덕 가지 배드는 반대쪽 평균이 50을 넘을 때 (Endings.Decide와 같은 기준)
            var over = desire ? ending.Kind == EndingKind.Good : ending.Kind == EndingKind.Bad;
            return string.Format(Lang.Get("quiz_final"), Lang.Get($"pstat_{Endings.StatOf(branch)}"), Endings.BranchStatAbove,
                opposite, Lang.Get(over ? "quiz_over50" : "quiz_under50"));
        }
        // 극단 엔딩: extreme_{교단 스탯}_{max|min}
        var parts = ending.Id.Split('_');
        var high = parts[2] == "max";
        return string.Format(Lang.Get("quiz_extreme"), Lang.Get($"stat_{parts[1]}"), Lang.Get(high ? "quiz_high" : "quiz_low"), Endings.ExtremeYears);
    }
}
