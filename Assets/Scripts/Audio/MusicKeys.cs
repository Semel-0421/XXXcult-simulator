// 배경음악 파일 이름 규칙. 파일은 Assets/Resources/Audio/Music/{키}.(ogg|wav|mp3) 로 넣는다.
//   menu                 게임 시작 전 메인 화면 (어둡고 스산한 성가풍)
//   tier_Persecuted      탄압  (가장 어둡고 악마에게 지배당한 듯 암울하게)
//   tier_Watched         경계
//   tier_Neutral         중립
//   tier_Respected       존중
//   tier_Recognized      공인  (가장 성스럽고 밝게)
//   tier_Dissolved       해체 결말 (없으면 tier_Persecuted를 쓴다)
public static class MusicKeys
{
    public const string Menu = "menu";

    public static string ForStanding(Standing standing) => $"tier_{standing}";

    // 해당 키의 파일이 없을 때 대신 찾아볼 키 (앞쪽부터 시도)
    public static string[] Candidates(string key) => key == "tier_Dissolved"
        ? new[] { key, "tier_Persecuted" }
        : new[] { key };
}
