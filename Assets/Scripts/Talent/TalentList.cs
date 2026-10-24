using System;
using System.Collections.Generic;
using System.Linq;

// 보유한 기프티드 목록. 같은 기프티드는 한 번만 가질 수 있다.
public class TalentList
{
    readonly List<Talent> owned = new();

    public IReadOnlyList<Talent> Owned => owned;

    // 기프티드는 아니지만 같은 방식으로 효과를 주는 것들 (배운 교리). GameSession이 연결한다.
    public Func<IEnumerable<Talent>> ExtraEffects { get; set; }

    // 효과 계산용: 보유 기프티드 + 추가 효과
    public IEnumerable<Talent> Effective
    {
        get
        {
            foreach (var talent in owned) yield return talent;
            var extra = ExtraEffects?.Invoke();
            if (extra == null) yield break;
            foreach (var talent in extra) yield return talent;
        }
    }

    // 퍼센트 보정 합산 캐스 (기프티드 획득·상실 또는 교리 변경 시 무효화)
    Dictionary<PersonalStatId, float> _personalPercentCache;
    Dictionary<StatId, float> _cultPercentCache;
    bool _percentCacheDirty = true;

    public bool Has(string id) => owned.Any(talent => talent.Id == id);

    public bool Add(Talent talent)
    {
        if (Has(talent.Id)) return false;
        owned.Add(talent);
        NormalCounters.OnGifted(talent);
        HiddenCounters.OnGifted(owned.Count);
        _percentCacheDirty = true;
        return true;
    }

    // 기프티드 획득/상실 또는 교리 변경 후 호출해 퍼센트 캐시 무효화
    public void MarkPercentCacheDirty() => _percentCacheDirty = true;

    // GiftedEffects에서 호출: 개인 스탯 퍼센트 합산
    internal float GetPersonalPercent(PersonalStatId stat)
    {
        if (_percentCacheDirty) RebuildPercentCache();
        return _personalPercentCache.TryGetValue(stat, out var v) ? v : 0f;
    }

    // GiftedEffects에서 호출: 교단 스탯 퍼센트 합산
    internal float GetCultPercent(StatId stat)
    {
        if (_percentCacheDirty) RebuildPercentCache();
        return _cultPercentCache.TryGetValue(stat, out var v) ? v : 0f;
    }

    void RebuildPercentCache()
    {
        _personalPercentCache ??= new Dictionary<PersonalStatId, float>();
        _cultPercentCache ??= new Dictionary<StatId, float>();
        _personalPercentCache.Clear();
        _cultPercentCache.Clear();

        foreach (var g in Effective)
        {
            foreach (var m in g.Modifiers)
            {
                if (m.PersonalStat.HasValue)
                    _personalPercentCache[m.PersonalStat.Value] =
                        _personalPercentCache.GetValueOrDefault(m.PersonalStat.Value) + m.Percent;
                if (m.CultStat.HasValue)
                    _cultPercentCache[m.CultStat.Value] =
                        _cultPercentCache.GetValueOrDefault(m.CultStat.Value) + m.Percent;
            }
        }
        _percentCacheDirty = false;
    }
}
