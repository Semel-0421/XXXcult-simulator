// 선언 순서 = 분류 순서 (체력군 → 기적 → 욕망 → 덕목)
public enum PersonalStatId
{
    Health, Mentality, Piety, Repute,
    Miracle,
    Lust, Greed, Appetite, Ambition,
    Temperance, Charity, Patience, Integrity,
    Wealth, // 나중에 추가한 스탯. 씬에 저장된 값이 밀리지 않게 맨 끝에 두고, 분류는 체력군으로 취급
}
