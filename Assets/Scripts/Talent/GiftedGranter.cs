using System;
using System.Linq;

// 기프티드 지급 진입점. 시작 질문, 랜덤 시드(추후 업적도) 모두 여기를 거친다.
public static class GiftedGranter
{
    public static bool Grant(TalentList gifted, string id)
    {
        var entry = GiftedCatalog.Find(id);
        if (entry == null) throw new ArgumentException($"Unknown gifted id: {id}");
        return gifted.Add(entry);
    }

    // 아직 없는 기프티드 중 count개를 rng로 뽑아 지급한다. 같은 시드면 같은 결과.
    public static void GrantRandom(TalentList gifted, Random rng, int count)
    {
        var pool = GiftedCatalog.All.Where(g => !gifted.Has(g.Id)).ToList();
        for (var i = 0; i < count && pool.Count > 0; i++)
        {
            var index = rng.Next(pool.Count);
            gifted.Add(pool[index]);
            pool.RemoveAt(index);
        }
    }
}
