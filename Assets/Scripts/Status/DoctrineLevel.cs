public static class DoctrineLevel
{
    const int LevelStep = 100;

    public static int Of(int value) => value <= 0 ? 0 : value / LevelStep + 1;
    public static bool IsMax(int value) => value >= ClampedStat.DefaultMax;
    public static string Label(int value) => IsMax(value) ? Lang.Get("doctrine_max") : string.Format(Lang.Get("doctrine_level"), Of(value));
}
