using System;
using System.Collections.Generic;
using System.Linq;

// 선택된 데미우르고스의 스탯표와 재앙 카운터 (SYS_DEMIURGE 4·5절, DEC-027·028 J·K·N).
//  - 첫 질문에 답한 순간 교주의 교단·개인 스탯을 그대로 복사한다 (접신·기적은 뺀다).
//  - 재앙으로 교주에게서 줄어든 만큼 같은 스탯에 더해진다. 그 밖에 성장 A·B·C(악한 아이온 습득·동거·재앙, 9단계 결정)로 섹터 주력 스탯이 자란다(EyeRules.Grow). 교단 스탯은 1000(신도도 1000명), 개인 스탯은 100에서 멈춘다.
//    그래서 데미우르고스가 모든 스탯을 채워도 교주가 모두 절반(500·50) 이상이면 비율이 2배를 넘지 않는다 (2026-10-26 결정).
//  - 주도권 비율 = 데미우르고스 비율 합 ÷ 교주 비율 합. 스탯마다 값 ÷ 최대치(신도는 1000명 기준)를 0~1로 잘라 더한다.
public class DemiurgeState
{
    public static readonly StatId[] CultStats =
        { StatId.LeaderFaith, StatId.FollowerFaith, StatId.Authority, StatId.Offerings, StatId.Followers, StatId.Doctrine, StatId.Reputation };
    public static readonly PersonalStatId[] PersonalStats =
        ((PersonalStatId[])Enum.GetValues(typeof(PersonalStatId))).Where(id => id != PersonalStatId.Miracle).ToArray();

    public const int FollowerScale = 1000; // 신도 수는 상한이 없어 CultStatus.Ratio와 같은 1000명 기준
    public const int CultCap = ClampedStat.DefaultMax, PersonalCap = PersonalStatus.MaxValue;

    readonly Dictionary<StatId, int> cult = new();
    readonly Dictionary<PersonalStatId, int> personal = new();

    public bool Copied { get; private set; }
    public int Counter { get; private set; }     // 재앙 카운터 (문턱에 이르면 재앙, 일어난 뒤 0)
    public int Calamities { get; private set; }  // 이번 판에 일어난 재앙 수

    public int this[StatId id] => cult.TryGetValue(id, out var v) ? v : 0;
    public int this[PersonalStatId id] => personal.TryGetValue(id, out var v) ? v : 0;

    public void Copy(CultStatus from, PersonalStatus fromPersonal)
    {
        foreach (var id in CultStats) cult[id] = Math.Min(CultCap, from[id].Value);
        foreach (var id in PersonalStats) personal[id] = Math.Min(PersonalCap, fromPersonal[id].Value);
        Copied = true;
    }

    public void Absorb(StatId id, int amount) { if (amount > 0) cult[id] = Math.Min(CultCap, this[id] + amount); }
    public void Absorb(PersonalStatId id, int amount) { if (amount > 0) personal[id] = Math.Min(PersonalCap, this[id] + amount); }

    // 성장: 소수는 쌓았다가 정수가 되면 더한다 (나머지는 저장하지 않는다)
    readonly Dictionary<StatId, float> cultRemainder = new();
    readonly Dictionary<PersonalStatId, float> personalRemainder = new();

    public void Grow(StatId id, float amount)
    {
        var total = (cultRemainder.TryGetValue(id, out var r) ? r : 0f) + amount;
        var whole = (int)total;
        cultRemainder[id] = total - whole;
        Absorb(id, whole);
    }

    public void Grow(PersonalStatId id, float amount)
    {
        var total = (personalRemainder.TryGetValue(id, out var r) ? r : 0f) + amount;
        var whole = (int)total;
        personalRemainder[id] = total - whole;
        Absorb(id, whole);
    }

    public void AddCounter(int amount) => Counter = Math.Max(0, Counter + amount);

    public void ResetCounter()
    {
        Counter = 0;
        Calamities++;
    }

    // ---- 주도권 ----

    static float Part(StatId id, int value) =>
        Math.Clamp(id == StatId.Followers ? value / (float)FollowerScale : value / (float)ClampedStat.DefaultMax, 0f, 1f);

    static float Part(int personalValue) => Math.Clamp(personalValue / (float)PersonalStatus.MaxValue, 0f, 1f);

    public float Weight => CultStats.Sum(id => Part(id, this[id])) + PersonalStats.Sum(id => Part(this[id]));

    public static float WeightOf(CultStatus cult, PersonalStatus personal) =>
        CultStats.Sum(id => Part(id, cult[id].Value)) + PersonalStats.Sum(id => Part(personal[id].Value));

    // 데미우르고스 ÷ 교주. 2를 넘으면 주도권을 뺏긴다 (정확히 2는 교주 쪽)
    public float Ratio(CultStatus playerCult, PersonalStatus playerPersonal)
    {
        var mine = WeightOf(playerCult, playerPersonal);
        return mine <= 0 ? (Weight > 0 ? float.MaxValue : 1f) : Weight / mine;
    }

    // ---- 저장 ----

    public List<int> CultValues => CultStats.Select(id => this[id]).ToList();
    public List<int> PersonalValues => PersonalStats.Select(id => this[id]).ToList();

    public void Restore(bool copied, IList<int> cultValues, IList<int> personalValues, int counter, int calamities)
    {
        cult.Clear();
        personal.Clear();
        cultRemainder.Clear();
        personalRemainder.Clear();
        Copied = copied;
        for (int i = 0; i < CultStats.Length && cultValues != null && i < cultValues.Count; i++) cult[CultStats[i]] = cultValues[i];
        for (int i = 0; i < PersonalStats.Length && personalValues != null && i < personalValues.Count; i++) personal[PersonalStats[i]] = personalValues[i];
        Counter = Math.Max(0, counter);
        Calamities = Math.Max(0, calamities);
    }
}
