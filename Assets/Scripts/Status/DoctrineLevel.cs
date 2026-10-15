using System;

// 교리 레벨: 교리 스탯에 대한 상용로그 곡선. 낮은 레벨은 빨리 오르고 높은 레벨일수록 더 많은 교리가 필요하다.
//   레벨 = floor(MaxLevel × log10(1 + 교리/Scale) / log10(1 + 최대치/Scale))  → 교리 0이면 0, 최대치(1000)면 MaxLevel
public static class DoctrineLevel
{
    public const int MaxLevel = 10;
    const float Scale = 100f;
    const double Epsilon = 1e-9; // 경계값(예: 1000 → 10)이 부동소수 오차로 한 단계 내려가지 않게

    public const float GainMultiplier = 2f;          // 행동·이벤트로 오르는 교리는 두 배
    public const float FaithGuardPerLevel = 0.05f;   // 교리 레벨마다 신도 믿음 감소를 5%씩 막는다 (10레벨이면 50%)

    static readonly double Top = Math.Log10(1 + ClampedStat.DefaultMax / Scale);

    public static int Of(int value) =>
        value <= 0 ? 0 : Math.Min(MaxLevel, (int)Math.Floor(MaxLevel * Math.Log10(1 + value / Scale) / Top + Epsilon));

    // 해당 레벨이 되는 최소 교리 값
    public static int Threshold(int level) =>
        level <= 0 ? 0 : (int)Math.Ceiling(Scale * (Math.Pow(10, Top * level / MaxLevel) - 1) - Epsilon);

    // 신도 믿음이 깎일 때 막아 주는 비율 (0~0.5)
    public static float FaithGuard(int value) => Of(value) * FaithGuardPerLevel;

    public static bool IsMax(int value) => value >= ClampedStat.DefaultMax;
    public static string Label(int value) => IsMax(value) ? Lang.Get("doctrine_max") : string.Format(Lang.Get("doctrine_level"), Of(value));
}
