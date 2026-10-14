using System;

// 한 판의 진행 상태 보관소. 씬이 바뀌어도 유지된다.
public static class GameSession
{
    public static CultStatus Cult { get; private set; } = new();
    public static PersonalStatus Personal { get; private set; } = new();
    public static int Seed { get; private set; } = Environment.TickCount;
    public static Random Rng { get; private set; } = new(Seed);
    public static GameClock Age { get; private set; } = new();      // 플레이어 나이 (시작부터)
    public static GameClock CultTime { get; private set; } = new(); // 교단 창설 이후 시간
    public static bool CultFounded { get; private set; }

    public static event Action Founded;

    public static void FoundCult(string name)
    {
        Cult.Name = name;
        CultFounded = true;
        Founded?.Invoke();
    }

    public static void AdvanceTurn()
    {
        Age.Advance();
        if (CultFounded) CultTime.Advance();
    }

    public static void StartNew(int? seed = null)
    {
        Cult = new CultStatus();
        Personal = new PersonalStatus();
        Seed = seed ?? Environment.TickCount;
        Rng = new Random(Seed);
        Age = new GameClock();
        CultTime = new GameClock();
        CultFounded = false;
    }
}
