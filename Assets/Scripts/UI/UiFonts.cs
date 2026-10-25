using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 글꼴을 역할로 나눈다 (SYS_UI_DESIGN 결정 2).
// 제목(창 제목·교단 이름·항목 이름·욕망/덕목 머리글)은 기존 장식 글꼴, 본문·라벨·수치는 명조, 버튼은 굵은 명조.
// 장면에 놓인 글자는 장면을 불러올 때 한 번 바꾸고, 코드로 만드는 글자는 UiKit.Label·MakeButton이 고른다.
public static class UiFonts
{
    static TMP_FontAsset body, strong, display;

    public static TMP_FontAsset Body => body != null ? body : body = Resources.Load<TMP_FontAsset>("Fonts/NotoSerifKR-Regular SDF");
    public static TMP_FontAsset Strong => strong != null ? strong : strong = Resources.Load<TMP_FontAsset>("Fonts/NotoSerifKR-Bold SDF");

    // 장식 글꼴: 장면의 한글 글꼴(다카포)을 바꾸기 전에 기억해 둔다. 코드로 만드는 창의 제목이 이것을 쓴다
    public static TMP_FontAsset Display => display;

    // 코드로 만드는 글자 중 장식 글꼴을 그대로 둘 이름
    public static bool IsDisplayName(string name) =>
        name.Contains("Title") || name == "Name" || name.EndsWith("Header") || name == "Glyph";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Hook()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded; // 도메인 리로드 없이 플레이를 다시 시작해도 한 번만 건다
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        foreach (var root in scene.GetRootGameObjects())
            ApplyScene(root.transform);
    }

    // 장면 글자: 교단 이름·머리글·"성공!" 표시만 장식 글꼴, 버튼 안 글자는 굵은 명조, 나머지는 명조
    public static void ApplyScene(Transform root)
    {
        if (Body == null) return;
        foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.font == Body || text.font == Strong) continue;
            if (display == null && text.font != null && text.font.HasCharacter('가')) display = text.font;
            var parent = text.transform.parent;
            bool keep = text.name == "CultName" || text.name.EndsWith("Header") || text.name.Contains("Title")
                || (text.name == "Result" && parent != null && parent.name == "ActionResult");
            if (keep) continue;
            text.font = text.GetComponentInParent<Button>(true) != null ? Strong : Body;
        }
    }
}
