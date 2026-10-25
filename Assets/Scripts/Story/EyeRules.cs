using System;
using System.Linq;

// 황금의 눈동자 모드 (Docs/GDD/systems/SYS_DEMIURGE.md). 준비된 신과 거울 세계를 합친 세 번째 특수 모드.
//  - 해금 (DEC-028 A): 준비된 신의 승천 엔딩 하나 + 거울 세계 전용 엔딩 하나를 모두 봐야 열린다.
//    신 모드 패배 엔딩(그릇의 붕괴·잊힌 신·해산된 교단)은 세지 않는다.
//  - 시작 (DEC-027·028 D): 신 모드의 성인 그릇 상태(GodRules.StartVessel) 위에서 돈다. GodMode도 켜서 그릇·접신·압박·축성·전용 이벤트를 그대로 쓰고,
//    다른 점은 EyeMode로 가른다: 기프티드 없음, 신 모드 업적 카운터 안 쌓음, 신의 본성 선택 없이 창설과 동시에 교단 행동이 열림, 패배 엔딩 없음.
//  - 첫 질문(눈동자) → 이름 → 성별 순서로 묻는다 (IntroSequence). 고른 섹터의 0단계 스킬 2개를 바로 얻는다 (AeonTree.Grant).
//  - 아이온 트리(AeonTree)는 GodState.Aeons에 있고, 효과·포인트·단계·건물 해금은 GodTree 계산에 더해진다.
//  - 데미우르고스 스탯·재앙(4단계), 아이온 트리 화면(5단계), 엔딩(6단계)은 아직 없다.
public enum EyeSector { None, Yahweh, Satan, Samael, Yaldabaoth }

public static class EyeRules
{
    public static readonly EyeSector[] Sectors = { EyeSector.Yahweh, EyeSector.Satan, EyeSector.Samael, EyeSector.Yaldabaoth };

    // 새 판: 성인 그릇 + 모드 플래그. 섹터는 첫 질문에서 정한다
    public static void Begin(int? seed = null)
    {
        GodRules.StartVessel(seed);
        GameSession.SetEyeMode(true);
        Profile.Count("eye_runs");
    }

    public static void ChooseSector(EyeSector sector)
    {
        if (sector == EyeSector.None) return;
        GameSession.SetEyeSector(sector);
        GameSession.God.Aeons.Grant(sector); // 고른 섹터의 0단계 선·악 두 노드를 처음부터 얻는다 (DEC-027)
    }

    // 교단 창설: 신의 본성 대신 바로 교리 선포를 채워 교단 행동을 연다
    public static void OnFounded()
    {
        var doctrines = GameSession.Cult.Doctrines;
        if (!doctrines.IsMax(DoctrineSkill.Declaration)) doctrines.LevelUp(DoctrineSkill.Declaration);
    }

    public static string SectorKey(EyeSector sector) => $"eye_sector_{sector.ToString().ToLowerInvariant()}";

    // 첫 질문 글: 질문 + 번호 붙은 답 4개. 답은 <link="e1">~<link="e4">로 눌러 고른다
    public static string QuestionText()
    {
        var lines = Sectors.Select((s, i) => $"{i + 1}. <link=\"e{i + 1}\"><color=#F2D978>{Lang.Get(SectorKey(s) + "_answer")}</color></link>");
        return $"{Lang.Get("eye_question")}\n\n{string.Join("\n", lines)}";
    }

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
