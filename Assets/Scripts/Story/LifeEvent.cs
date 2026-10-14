using System;
using System.Collections.Generic;
using System.Linq;

// 나이대가 정해진 생애 이벤트. 목록은 LifeEvents(생성 코드)에 있다.
public class LifeEvent
{
    public string Id { get; }
    public int MinAge { get; }
    public int MaxAge { get; }
    public Question Question { get; }

    public LifeEvent(string id, int minAge, int maxAge, Question question)
    {
        Id = id; MinAge = minAge; MaxAge = maxAge; Question = question;
    }

    public bool Fits(int age) => MinAge <= age && age <= MaxAge;

    // 현재 나이에 맞는 이벤트 중 아직 안 나온 것을 rng로 고른다. 다 썼으면 이전 나이대, 그래도 없으면 전체에서 고른다.
    public static LifeEvent Pick(IReadOnlyList<LifeEvent> pool, int age, ISet<string> used, Random rng)
    {
        var candidates = pool.Where(e => e.Fits(age) && !used.Contains(e.Id)).ToList();
        if (candidates.Count == 0) candidates = pool.Where(e => e.MinAge <= age && !used.Contains(e.Id)).ToList();
        var picked = candidates.Count > 0 ? candidates[rng.Next(candidates.Count)] : pool[rng.Next(pool.Count)];
        used.Add(picked.Id);
        return picked;
    }
}
