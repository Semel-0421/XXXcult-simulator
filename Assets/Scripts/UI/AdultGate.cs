using System;
using UnityEngine;

// 자체 성인 인증: 옵션 창에 생년월일을 넣으면 컴퓨터 날짜로 만 나이를 계산해 성인 콘텐츠를 연다.
// 생년월일은 저장하지 않고 인증 결과만 기억한다. 한 번 인증하면 다시 묻지 않는다.
// 성인 콘텐츠(AdultEvents, Resources/AdultArt 삽화)는 Active로 거른다.
public static class AdultGate
{
    public const int AdultAge = 19; // 청소년보호법: 만 19세 미만은 청소년
    public const float EventChance = 0.25f; // 교단 행동 뒤 성인 이벤트를 굴릴 확률 (그 안에서 CultEvent.Chance를 한 번 더 굴린다)
    const string VerifiedPref = "adult_verified", ContentPref = "adult_content", AchievementPref = "adult_achievements";

    public enum Result { Adult, Minor, Invalid }

    public static bool Verified => PlayerPrefs.GetInt(VerifiedPref, 0) == 1;

    // 인증한 뒤에도 옵션에서 끌 수 있다 (기본 켜짐)
    public static bool ContentEnabled
    {
        get => Verified && PlayerPrefs.GetInt(ContentPref, 1) == 1;
        set { PlayerPrefs.SetInt(ContentPref, value ? 1 : 0); PlayerPrefs.Save(); }
    }

    // 성인 업적·성인 기프티드는 성인 콘텐츠와 별개로 직접 켜야 나타난다 (기본 꺼짐)
    public static bool AchievementsEnabled
    {
        get => ContentEnabled && PlayerPrefs.GetInt(AchievementPref, 0) == 1;
        set { PlayerPrefs.SetInt(AchievementPref, value ? 1 : 0); PlayerPrefs.Save(); }
    }

    // 플레이어가 인증·켬 상태이고, 게임 속 인물도 성인일 때만
    public static bool Active => ContentEnabled && GameSession.Age != null && GameSession.Age.Years >= AdultAge;

    // 만 나이: 올해 생일이 아직 안 지났으면 한 살 뺀다
    public static int AgeOn(DateTime birth, DateTime today)
    {
        var age = today.Year - birth.Year;
        if (today.Month < birth.Month || (today.Month == birth.Month && today.Day < birth.Day)) age--;
        return age;
    }

    public static Result Check(int year, int month, int day, DateTime today)
    {
        if (year < 1900 || year > 9999 || month < 1 || month > 12 || day < 1 || day > DateTime.DaysInMonth(year, month))
            return Result.Invalid;
        var birth = new DateTime(year, month, day);
        if (birth > today.Date) return Result.Invalid;
        return AgeOn(birth, today.Date) >= AdultAge ? Result.Adult : Result.Minor;
    }

    // 컴퓨터 날짜 기준으로 확인하고, 성인이면 인증을 기억한다
    public static Result Verify(int year, int month, int day)
    {
        var result = Check(year, month, day, DateTime.Today);
        if (result == Result.Adult)
        {
            PlayerPrefs.SetInt(VerifiedPref, 1);
            PlayerPrefs.SetInt(ContentPref, 1);
            PlayerPrefs.Save();
        }
        return result;
    }
}
