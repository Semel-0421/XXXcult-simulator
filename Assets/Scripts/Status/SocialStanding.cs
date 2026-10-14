// 명망 = 교단을 바라보는 사회적 시선. 낮으면 지역 사회·타 종교·국가의 견제를 받다 해체되고, 높으면 공인된 종교로 대우받는다.
public enum Standing { Dissolved, Persecuted, Watched, Neutral, Respected, Recognized }

public static class SocialStanding
{
    public const int PersecutedBelow = 150, WatchedBelow = 300, RespectedFrom = 700, RecognizedFrom = 900;

    public static Standing Of(int reputation) => reputation switch
    {
        <= 0 => Standing.Dissolved,
        < PersecutedBelow => Standing.Persecuted,
        < WatchedBelow => Standing.Watched,
        < RespectedFrom => Standing.Neutral,
        < RecognizedFrom => Standing.Respected,
        _ => Standing.Recognized,
    };

    public static Standing Of(CultStatus cult) => Of(cult[StatId.Reputation].Value);

    // 교단 턴마다 붙는 효과. 높은 명망은 주목받는 만큼 조금씩 깎인다.
    public static CultEffect[] TurnEffects(Standing standing) => standing switch
    {
        Standing.Persecuted => new[] { new CultEffect(StatId.Followers, -3), new CultEffect(StatId.FollowerFaith, -10), new CultEffect(StatId.Offerings, -10) },
        Standing.Watched => new[] { new CultEffect(StatId.Followers, -1), new CultEffect(StatId.Offerings, -5) },
        Standing.Respected => new[] { new CultEffect(StatId.Followers, 1), new CultEffect(StatId.Offerings, 5), new CultEffect(StatId.Reputation, -1) },
        Standing.Recognized => new[] { new CultEffect(StatId.Followers, 2), new CultEffect(StatId.Offerings, 10), new CultEffect(StatId.Reputation, -3) },
        _ => System.Array.Empty<CultEffect>(),
    };

    public static void ApplyTurn(CultStatus cult)
    {
        foreach (var effect in TurnEffects(Of(cult)))
            cult[effect.Stat].Add(effect.Amount);
    }
}
