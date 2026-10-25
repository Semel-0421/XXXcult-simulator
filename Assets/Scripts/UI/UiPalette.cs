using UnityEngine;

// UI 색을 역할 이름으로 모아 둔 곳. 창마다 같은 값을 직접 적지 말고 여기 이름을 쓴다.
// 진홍은 닫기·위험에만 쓴다 (SYS_UI_DESIGN 결정 7). 값은 기존 화면과 똑같이 맞춰 두었다.
public static class UiPalette
{
    // 금박
    public static readonly Color Gold = new(0.83f, 0.69f, 0.22f);
    public static readonly Color GoldLight = new(0.95f, 0.82f, 0.42f);
    public static readonly Color GoldDark = new(0.42f, 0.28f, 0.08f);
    public static readonly Color GoldFaint = new(0.83f, 0.69f, 0.22f, 0.5f); // 금테 Outline

    // 바탕 (어두운 순)
    public static readonly Color Ink = new(0.04f, 0.02f, 0.08f);
    public static readonly Color Panel = new(0.05f, 0.03f, 0.09f, 0.98f);
    public static readonly Color Row = new(0.10f, 0.07f, 0.16f, 1f);
    public static readonly Color Button = new(0.24f, 0.18f, 0.36f, 1f);
    public static readonly Color TabIdle = new(0.16f, 0.07f, 0.24f, 0.95f);   // 도감 탭 (고르지 않음)
    public static readonly Color TabSelected = new(0.36f, 0.26f, 0.52f);      // 고른 탭 (금테와 함께)
    public static readonly Color RowIdle = new(0.16f, 0.07f, 0.24f, 0.9f);    // 목록 행 (못 얻음)
    public static readonly Color RowDone = new(0.42f, 0.12f, 0.24f, 0.96f);   // 목록 행 (얻음)

    // 책 표지 틀의 위·아래 색 (UiSkin.BookFrame)
    public static readonly Color BookGalleryTop = new(0.4f, 0.1f, 0.26f), BookGalleryBottom = new(0.2f, 0.06f, 0.34f); // 도감·엔딩
    public static readonly Color BookEventTop = new(0.22f, 0.08f, 0.30f), BookEventBottom = new(0.08f, 0.03f, 0.12f);  // 본문·결과
    public static readonly Color BookAltarTop = new(0.30f, 0.12f, 0.32f), BookAltarBottom = new(0.12f, 0.06f, 0.22f);  // 권능·축성

    // 글자
    public static readonly Color Parchment = new(0.88f, 0.82f, 0.68f);
    public static readonly Color TextBright = new(0.92f, 0.88f, 0.98f);  // 도감 설명
    public static readonly Color TextSoft = new(0.85f, 0.82f, 0.92f);    // 목록 설명
    public static readonly Color Muted = new(0.68f, 0.62f, 0.78f);      // 가장 밝은 책 표지 위에서도 대비 4.5:1 이상
    public static readonly Color Locked = new(0.55f, 0.5f, 0.6f);       // 잠긴 이름 "???"

    // 상태
    public static readonly Color Crimson = new(0.55f, 0.08f, 0.14f);    // 닫기·위험
}

// 글자 크기 단계 (기준 해상도 1280×720). 새로 만드는 글자는 이 다섯 단계만 쓴다 (SYS_UI_DESIGN 3-2).
public static class UiType
{
    public const float Title = 34;   // 창 제목, 엔딩 이름
    public const float Heading = 28; // 소제목
    public const float Body = 22;    // 본문, 선택지
    public const float Label = 18;   // 스탯 이름, 버튼
    public const float Caption = 16; // 안내, 보조
}
