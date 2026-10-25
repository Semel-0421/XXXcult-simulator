using System;
using System.Linq;

// 황금의 눈동자 모드 (Docs/GDD/systems/SYS_DEMIURGE.md). 준비된 신과 거울 세계를 합친 세 번째 특수 모드.
//  - 해금 (DEC-028 A): 준비된 신의 승천 엔딩 하나 + 거울 세계 전용 엔딩 하나를 모두 봐야 열린다.
//    신 모드 패배 엔딩(그릇의 붕괴·잊힌 신·해산된 교단)은 세지 않는다.
//  - 판 진행(첫 질문·데미우르고스 스탯·재앙·아이온 트리)은 다음 단계에서 붙인다. 지금은 메뉴에서 설명만 보여 준다.
public static class EyeRules
{
    public const int UnlockConditions = 2;

    public static bool GodAscensionSeen => GodEndings.AscensionIds.Any(Endings.Seen);
    public static bool MirrorEndingSeen => MirrorEndings.All.Any(e => Endings.Seen(e.Id));

    // 채운 해금 조건 수 (0~2)
    public static int UnlockProgress => (GodAscensionSeen ? 1 : 0) + (MirrorEndingSeen ? 1 : 0);
    public static bool Unlocked => UnlockProgress >= UnlockConditions;

    // 잠긴 버튼 제목: 조건 하나마다 금빛이 절반씩 차오른다 (조건이 둘뿐이라 신 모드처럼 "마지막 한 칸 남김"을 하지 않는다)
    public static float LockedFill(int progress) => (float)Math.Min(progress, UnlockConditions) / UnlockConditions;
    public static string LockedCount(int progress) => $"({Math.Min(progress, UnlockConditions)}/{UnlockConditions})";
}
