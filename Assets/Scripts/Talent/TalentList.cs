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

    public bool Has(string id) => owned.Any(talent => talent.Id == id);

    public bool Add(Talent talent)
    {
        if (Has(talent.Id)) return false;
        owned.Add(talent);
        return true;
    }
}
