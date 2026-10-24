using System;

// v2 기프티드 효과 등록표. GiftedV2Effects가 처음 쓰일 때 RegisterAll을 한 번 부른다.
// 모드별 효과는 partial 파일(GiftedV2Table.Normal.cs / .God.cs / .Mirror.cs)에서 RegisterNormal/God/Mirror를 구현해 넣는다.
// 아래 3개는 패턴 예시(모드당 1개). 같은 id를 두 번 등록하면 Register가 예외를 낸다 — 예시를 모드 파일로 옮길 때는 여기서 지울 것.
// 목록·훅 대응표: Docs/GDD/systems/SYS_GIFTED_V2_EFFECTS.md
public static partial class GiftedV2Table
{
    public const float StubbornLuckChance = 0.125f;
    public const float HalfChance = 0.5f; // 한 번뿐인 보호 효과가 실제로 듣는 확률 (밸런스: 공유 효과 절반)
    public static Func<double> RollOverride; // 테스트에서 난수를 고정
    public static bool Pass(float chance) => (RollOverride?.Invoke() ?? GameSession.Rng.NextDouble()) < chance;
    public const int GodFaithPointsPerYear = 2;
    public const float InvertedReputeLossPercent = 20f;

    public static void RegisterAll()
    {
        // 끈질긴 행운 (일반·공유): 판정 실패 시 25% 확률로 성공
        GiftedV2Effects.Register("v2n001", new EffectDef
        {
            RescueFailure = _ => GameSession.Rng.NextDouble() < StubbornLuckChance,
        });

        // 신 전용: 매년 권능 포인트 +2
        GiftedV2Effects.Register("v2g066", new EffectDef
        {
            OnYearEnd = () => { if (GameSession.GodMode && GameSession.CultFounded) GameSession.God.Points.Add(GodFaithPointsPerYear); },
        });

        // 뒤집힌 평판 (거울 전용): 명망 하락 −20%
        GiftedV2Effects.Register("v2m107", new EffectDef
        {
            LossPercent = InvertedReputeLossPercent,
            StatWhen = c => c.Cult == StatId.Reputation,
        });

        RegisterNormal();
        RegisterGod();
        RegisterMirror();
    }

    static partial void RegisterNormal();
    static partial void RegisterGod();
    static partial void RegisterMirror();
}
