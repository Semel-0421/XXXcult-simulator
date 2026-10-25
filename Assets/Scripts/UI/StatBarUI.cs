using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 상단 교단 스탯 한 칸. 결정 4: 믿음·신도·명망·봉헌금은 넓고 크게, 신도의 믿음·권위·교리는 좁고 작게.
// 값이 바뀌면 그 턴 동안 금테 + ▲/▼ 표시, 다음 행동을 고르면(ActionBar.Acting) 그때 값을 기준으로 다시 잰다.
public class StatBarUI : MonoBehaviour
{
    [SerializeField] StatId statId;
    [SerializeField] TMP_Text nameLabel, valueLabel;
    ClampedStat stat;
    int baseline;      // 이번 턴이 시작될 때의 값 (▲/▼ 기준)
    Outline highlight; // 바뀐 칸 금테
    string up = "▲", down = "▼";

    const string DownColor = "#E65959";

    static bool IsMajor(StatId id) => id is StatId.LeaderFaith or StatId.Followers or StatId.Reputation or StatId.Offerings;

    public void Bind(CultStatus status)
    {
        stat = status[statId];
        baseline = stat.Value;
        stat.Changed += OnStatChanged;
        Lang.Changed += Refresh;
        ActionBar.Acting += OnTurnStart;
        ApplySize();
        PrepareHighlight();
        Refresh();
    }

    // 가로 배치(HorizontalLayoutGroup)가 남는 폭을 비율대로 나누므로 중요한 칸에 더 큰 몫을 준다
    void ApplySize()
    {
        var major = IsMajor(statId);
        if (!TryGetComponent<LayoutElement>(out var layout)) layout = gameObject.AddComponent<LayoutElement>();
        layout.flexibleWidth = major ? 1.5f : 1f;
        valueLabel.fontSize = valueLabel.fontSizeMax = major ? UiType.Heading : UiType.Label;
        valueLabel.fontSizeMin = 12;
        valueLabel.enableAutoSizing = true; // 좁은 칸의 "최고 단계"·영어 이름이 넘치지 않게 줄인다
        // 이름도 한 줄로 두고 넘치면 줄인다 (좁은 칸의 "Followers' Faith"가 두 줄로 넘쳤다)
        nameLabel.textWrappingMode = TextWrappingModes.NoWrap;
        nameLabel.fontSizeMax = nameLabel.fontSize;
        nameLabel.fontSizeMin = nameLabel.fontSize * 0.6f;
        nameLabel.enableAutoSizing = true;
        if (!major) StackLabels();
    }

    // 작은 칸은 이름을 윗줄, 값을 아랫줄에 둔다 (3안 A: 영어 이름을 줄이지 않고 칸 폭을 다 쓴다)
    void StackLabels()
    {
        var name = nameLabel.rectTransform;
        name.anchorMin = new Vector2(0, 0.5f);
        name.anchorMax = Vector2.one;
        name.offsetMin = new Vector2(8, -1);
        name.offsetMax = new Vector2(-8, -2);
        nameLabel.alignment = TextAlignmentOptions.BottomLeft;

        var value = valueLabel.rectTransform;
        value.anchorMin = Vector2.zero;
        value.anchorMax = new Vector2(1, 0.5f);
        value.offsetMin = new Vector2(8, 2);
        value.offsetMax = new Vector2(-8, 1);
        valueLabel.alignment = TextAlignmentOptions.TopRight;
    }

    void PrepareHighlight()
    {
        if (GetComponent<Graphic>() == null) return; // 칸 배경이 없으면 금테를 그릴 수 없다
        highlight = gameObject.AddComponent<Outline>(); // 씬·다른 스킨의 테두리와 섞이지 않게 따로 붙인다
        highlight.effectColor = UiPalette.Gold;
        highlight.effectDistance = new Vector2(2, 2);
        highlight.useGraphicAlpha = false;
        highlight.enabled = false;
        // 글꼴에 ▲▼가 없으면(장식 글꼴 등) 있는 글자로 바꾼다
        var font = valueLabel.font;
        if (font != null && !(font.HasCharacter('▲', true, true) && font.HasCharacter('▼', true, true))) { up = "+"; down = "-"; }
    }

    void OnStatChanged(int _) => Refresh();

    void OnTurnStart()
    {
        if (stat == null) return;
        baseline = stat.Value;
        Refresh();
    }

    void Refresh()
    {
        nameLabel.text = StatLabel.Of(statId);
        var text = statId == StatId.Doctrine ? DoctrineLevel.Label(stat.Value) : stat.Value.ToString("N0");
        var delta = stat.Value - baseline;
        if (delta != 0) text += delta > 0 ? $" <size=70%><color=#F2D978>{up}</color></size>" : $" <size=70%><color={DownColor}>{down}</color></size>";
        valueLabel.text = text;
        if (highlight != null) highlight.enabled = delta != 0;
    }

    void OnDestroy()
    {
        Lang.Changed -= Refresh;
        ActionBar.Acting -= OnTurnStart;
        if (stat != null) stat.Changed -= OnStatChanged;
    }
}
