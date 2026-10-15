using UnityEngine;
using UnityEngine.SceneManagement;

// 기기마다 다른 화면 비율에서 UI가 겹치지 않도록, 장면의 UI 전체를 정해진 비율의 영역(AspectFrame) 안에 가운데 맞춰 넣는다.
// 남는 가장자리는 검은 여백이 된다. 비율은 옵션 창에서 고르고 PlayerPrefs에 저장한다.
public static class AspectFit
{
    const string Pref = "aspect_mode";

    // 자동 = 16:9 (레이아웃을 설계한 기준 비율, 가장 안전)
    public static readonly string[] Modes = { "auto", "16:9", "18:9", "20:9", "4:3" };

    public static string Mode
    {
        get => PlayerPrefs.GetString(Pref, "auto");
        set { PlayerPrefs.SetString(Pref, value); PlayerPrefs.Save(); }
    }

    public static float Ratio(string mode) => mode switch
    {
        "18:9" => 18f / 9f,
        "20:9" => 20f / 9f,
        "4:3" => 4f / 3f,
        _ => 16f / 9f,
    };

    public static string Next(string mode)
    {
        var index = System.Array.IndexOf(Modes, mode);
        return Modes[(index + 1) % Modes.Length];
    }

    // 프레임의 배치 크기: 설계 해상도를 ratio 비율로 넓힌다(줄이지 않는다).
    // 넓은 비율은 가로만, 좁은 비율은 세로만 늘어나므로 설계한 배치보다 좁아져 겹치는 일이 없다.
    public static Vector2 FrameSize(Vector2 design, float ratio) =>
        design.x / design.y < ratio ? new Vector2(design.y * ratio, design.y) : new Vector2(design.x, design.x / ratio);

    // 프레임을 캔버스 안에 통째로 넣는 배율 (남는 쪽은 검은 여백)
    public static float FitScale(Vector2 canvas, Vector2 frame) => Mathf.Min(canvas.x / frame.x, canvas.y / frame.y);

    // 비율을 바꿨을 때 열린 장면의 모든 프레임을 다시 맞춘다
    public static void ApplyAll() => AspectFrame.FitAll();

    // AfterSceneLoad 훅은 첫 장면에서만 불리므로, 모든 장면 로드 이벤트에 연결한다
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Subscribe() => SceneManager.sceneLoaded += (scene, _) => WrapScene(scene);

    // 막 불러온 장면에 들어 있는 최상위 오버레이 캔버스만 감싼다.
    // 코드로 나중에 만드는 창(옵션·업적 등)과 장면을 넘어 남는 객체(토스트 등)는 이 시점에 이 장면에 없으므로 제외된다.
    static void WrapScene(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
            foreach (var canvas in root.GetComponentsInChildren<Canvas>(true))
                if (canvas.isRootCanvas && canvas.renderMode == RenderMode.ScreenSpaceOverlay
                    && canvas.GetComponentInChildren<AspectFrame>(true) == null)
                    AspectFrame.Wrap((RectTransform)canvas.transform);
    }
}
