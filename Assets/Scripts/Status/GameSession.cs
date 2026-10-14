// 한 판의 진행 상태 보관소. 씬이 바뀌어도 유지된다.
public static class GameSession
{
    public static CultStatus Cult { get; private set; } = new();
    public static PersonalStatus Personal { get; private set; } = new();

    public static void StartNew()
    {
        Cult = new CultStatus();
        Personal = new PersonalStatus();
    }
}
