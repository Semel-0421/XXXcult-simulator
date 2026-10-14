using System;

// 한 판의 진행 상태 보관소. 씬이 바뀌어도 유지된다.
public static class GameSession
{
    public static CultStatus Cult { get; private set; } = new();
    public static PersonalStatus Personal { get; private set; } = new();
    public static int Seed { get; private set; } = Environment.TickCount;
    public static Random Rng { get; private set; } = new(Seed);

    public static void StartNew(int? seed = null)
    {
        Cult = new CultStatus();
        Personal = new PersonalStatus();
        Seed = seed ?? Environment.TickCount;
        Rng = new Random(Seed);
    }
}
