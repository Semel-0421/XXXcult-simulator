using UnityEngine;
using UnityEngine.UI;

// 배경색과 문양을 계산하는 순수 로직.
//  가로축: 교단 명망 (낮음 ← → 높음)
//  세로축: 덕목 평균 − 욕망 평균 (아래 = 욕망이 큼, 위 = 덕목이 큼)
//  네 모서리마다 그 상황을 상징하는 종교적 문양이 있고, 중심에서 멀수록(극단일수록) 문양 색이 배경과 뚜렷하게 대비된다.
public static class Backdrop
{
    public enum Symbol { Halo, Crown, Thorns, Pentagram } // 위·오른쪽 / 아래·오른쪽 / 위·왼쪽 / 아래·왼쪽

    public const float DesireVirtueRange = 40f; // 덕목-욕망 평균 차이가 이만큼이면 세로축 끝

    static readonly PersonalStatId[] Desire = { PersonalStatId.Lust, PersonalStatId.Greed, PersonalStatId.Appetite, PersonalStatId.Ambition };
    static readonly PersonalStatId[] Virtue = { PersonalStatId.Temperance, PersonalStatId.Charity, PersonalStatId.Patience, PersonalStatId.Integrity };

    static readonly Color LowBackground = new(0.045f, 0.010f, 0.015f);   // 명망 바닥: 핏기 도는 어둠
    static readonly Color MidBackground = new(0.040f, 0.030f, 0.060f);   // 중립: 기존 짙은 남보라
    static readonly Color HighBackground = new(0.170f, 0.150f, 0.240f); // 명망 높음: 빛이 드는 어스름

    // 각 문양의 가장 진한(극단) 색
    static readonly Color[] SymbolColors =
    {
        new(1.00f, 0.88f, 0.45f), // 후광 - 금빛
        new(0.88f, 0.40f, 0.30f), // 왕관 - 세속 권세의 붉은 금
        new(0.72f, 0.78f, 0.92f), // 가시관 - 창백한 은빛
        new(0.90f, 0.08f, 0.10f), // 역오망성 - 핏빛
    };

    public readonly struct State
    {
        public readonly float X, Y, Extremity;
        public readonly Color Background;
        public readonly float[] Weights;   // Symbol 순서. 합이 1
        public readonly Color[] Colors;    // 현재 극단 정도가 반영된 문양 색 (배경에서 가장 진한 색까지)

        public State(float x, float y, float extremity, Color background, float[] weights, Color[] colors)
        {
            X = x; Y = y; Extremity = extremity; Background = background; Weights = weights; Colors = colors;
        }
    }

    // 현재 판 상태에서 축 값(-1~1)을 구한다
    public static (float x, float y) Axes(CultStatus cult, PersonalStatus personal)
    {
        var reputation = cult[StatId.Reputation].Value;
        var x = Mathf.Clamp((reputation - CultStatus.StartingReputation) / (float)CultStatus.StartingReputation, -1f, 1f);
        var y = Mathf.Clamp((Average(personal, Virtue) - Average(personal, Desire)) / DesireVirtueRange, -1f, 1f);
        return (x, y);
    }

    static float Average(PersonalStatus personal, PersonalStatId[] stats)
    {
        float total = 0;
        foreach (var stat in stats) total += personal[stat].Value;
        return total / stats.Length;
    }

    public static State Compute(float x, float y)
    {
        x = Mathf.Clamp(x, -1f, 1f);
        y = Mathf.Clamp(y, -1f, 1f);

        var background = x < 0 ? Color.Lerp(MidBackground, LowBackground, -x) : Color.Lerp(MidBackground, HighBackground, x);
        var extremity = Mathf.Clamp01(Mathf.Sqrt(x * x + y * y));

        // 네 문양에 대한 비중: 해당 사분면에 가까울수록 크다 (축 위에서도 두 문양이 섞여 어색한 끊김이 없다)
        float Part(float v, bool positive) => Mathf.Max(0f, positive ? v : -v) + 0.25f;
        var weights = new[]
        {
            Part(x, true) * Part(y, true),    // Halo: 명망 높음 + 덕목
            Part(x, true) * Part(y, false),   // Crown: 명망 높음 + 욕망
            Part(x, false) * Part(y, true),   // Thorns: 명망 낮음 + 덕목
            Part(x, false) * Part(y, false),  // Pentagram: 명망 낮음 + 욕망
        };
        var sum = weights[0] + weights[1] + weights[2] + weights[3];
        for (int i = 0; i < weights.Length; i++) weights[i] /= sum;

        // 극단으로 갈수록 배경색에서 문양 고유의 진한 색으로 다가간다 (이징으로 중심 근처에서는 거의 안 보임)
        var strength = extremity * extremity;
        var colors = new Color[4];
        for (int i = 0; i < colors.Length; i++) colors[i] = Color.Lerp(background, SymbolColors[i], strength);
        return new State(x, y, extremity, background, weights, colors);
    }
}

// 게임 화면 뒤 배경 이미지에 붙인다. 명망과 욕망·덕목에 따라 배경색과 문양이 서서히 바뀐다.
[RequireComponent(typeof(Image))]
public class ReligiousBackdrop : MonoBehaviour
{
    const float Speed = 0.5f;        // 초당 축 값이 따라가는 속도 (서서히 변하도록)
    const float SymbolScale = 0.9f;  // 화면 높이 대비 문양 크기

    Image background;
    Image[] symbols;
    float x, y;
    bool initialized;

    void Awake()
    {
        background = GetComponent<Image>();
        symbols = new Image[4];
        for (int i = 0; i < symbols.Length; i++)
        {
            var rect = UiKit.Child(transform, ((Backdrop.Symbol)i).ToString(), typeof(Image)).GetComponent<Image>();
            rect.raycastTarget = false;
            rect.sprite = BackdropSymbols.Get((Backdrop.Symbol)i);
            rect.preserveAspect = true;
            var t = (RectTransform)rect.transform;
            t.anchorMin = t.anchorMax = new Vector2(0.5f, 0.5f);
            symbols[i] = rect;
        }
    }

    void LateUpdate()
    {
        var (tx, ty) = Backdrop.Axes(GameSession.Cult, GameSession.Personal);
        if (!initialized) { x = tx; y = ty; initialized = true; }
        else
        {
            var step = Speed * Time.unscaledDeltaTime;
            x = Mathf.MoveTowards(x, tx, step);
            y = Mathf.MoveTowards(y, ty, step);
        }
        Apply(Backdrop.Compute(x, y));
    }

    void Apply(Backdrop.State state)
    {
        background.color = state.Background;
        var size = ((RectTransform)transform).rect.height * SymbolScale;
        for (int i = 0; i < symbols.Length; i++)
        {
            var color = state.Colors[i];
            color.a = state.Weights[i];
            symbols[i].color = color;
            ((RectTransform)symbols[i].transform).sizeDelta = new Vector2(size, size);
        }
    }
}
