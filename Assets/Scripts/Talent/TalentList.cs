using System.Collections.Generic;
using System.Linq;

// 보유한 기프티드 목록. 같은 기프티드는 한 번만 가질 수 있다.
public class TalentList
{
    readonly List<Talent> owned = new();

    public IReadOnlyList<Talent> Owned => owned;

    public bool Has(string id) => owned.Any(talent => talent.Id == id);

    public bool Add(Talent talent)
    {
        if (Has(talent.Id)) return false;
        owned.Add(talent);
        return true;
    }
}
