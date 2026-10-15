using System;

// 연령별 사망률: 해마다(분기마다 나눠서) 나이·성별에 맞는 확률로 주인공이 세상을 떠날 수 있다.
// 출처: WHO 세계보건통계(GHO) 대한민국 2019년 생명표의 5세 단위 사망확률(nqx).
//   n세 구간 안에 사망할 확률을 1년 확률로 바꾼 뒤(1-(1-nqx)^(1/n)), 다시 분기 확률로 나눈다.
//   85세 이상은 열린 구간이라 연 확률을 따로 추정해 넣었다(남 0.18, 여 0.14).
public static class Mortality
{
    // (구간 시작 나이, 구간 길이, 남자 nqx, 여자 nqx)
    static readonly (int from, int span, double male, double female)[] Table =
    {
        (0, 1, 0.002786, 0.002298),
        (1, 4, 0.000590, 0.000500),
        (5, 5, 0.000439, 0.000329),
        (10, 5, 0.000494, 0.000371),
        (15, 5, 0.001330, 0.000912),
        (20, 5, 0.001881, 0.001367),
        (25, 5, 0.002466, 0.001500),
        (30, 5, 0.003256, 0.002092),
        (35, 5, 0.004786, 0.002894),
        (40, 5, 0.006926, 0.003858),
        (45, 5, 0.011709, 0.005395),
        (50, 5, 0.018712, 0.007032),
        (55, 5, 0.027438, 0.009840),
        (60, 5, 0.039148, 0.013638),
        (65, 5, 0.059101, 0.022290),
        (70, 5, 0.095656, 0.040756),
        (75, 5, 0.172583, 0.087070),
        (80, 5, 0.298942, 0.178624),
    };

    const double Over85Male = 0.18, Over85Female = 0.14;

    // 그 나이에 1년 안에 사망할 확률
    public static double AnnualRate(int age, Gender gender)
    {
        if (age < 0) age = 0;
        foreach (var row in Table)
        {
            if (age >= row.from + row.span) continue;
            var q = gender == Gender.Female ? row.female : row.male;
            return 1 - Math.Pow(1 - q, 1.0 / row.span);
        }
        return gender == Gender.Female ? Over85Female : Over85Male;
    }

    // 한 분기(1턴) 안에 사망할 확률
    public static double QuarterRate(int age, Gender gender) =>
        1 - Math.Pow(1 - AnnualRate(age, gender), 1.0 / GameClock.QuartersPerYear);

    public static bool Dies(int age, Gender gender, Random rng) => rng.NextDouble() < QuarterRate(age, gender);
}
