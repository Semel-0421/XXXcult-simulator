using TMPro;
using UnityEngine;

// 교단 스탯 바 아래에 교단 창설 이후 시간과 사회적 시선(명망 단계)을 표시한다.
[RequireComponent(typeof(TMP_Text))]
public class CultTimeUI : MonoBehaviour
{
    TMP_Text label;
    GameClock clock;
    ClampedStat reputation;

    void Start()
    {
        label = GetComponent<TMP_Text>();
        clock = GameSession.CultTime;
        reputation = GameSession.Cult[StatId.Reputation];
        clock.Changed += Refresh;
        reputation.Changed += OnReputationChanged;
        Lang.Changed += Refresh;
        GameSession.Founded += Refresh;
        GameSession.MisfortuneChanged += Refresh;
        Refresh();
    }

    void OnReputationChanged(int _) => Refresh();

    // 교단 창설 전에는 비워 둔다
    void Refresh()
    {
        if (!GameSession.CultFounded) { label.text = ""; return; }
        var standing = SocialStanding.Of(reputation.Value);
        var color = standing switch
        {
            Standing.Persecuted or Standing.Dissolved => "#DC5050",
            Standing.Watched => "#E0A050",
            Standing.Neutral => "#C8C0D8",
            _ => "#F2D978",
        };
        var misfortune = GameSession.Misfortune > 0 ? $"   <color=#DC5050>{string.Format(Lang.Get("misfortune_label"), GameSession.Misfortune)}</color>" : "";
        label.text = $"{string.Format(Lang.Get("cult_time"), clock.Years, clock.Quarter)}   <color={color}>{Lang.Get("standing_label")} {Lang.Get($"standing_{standing}")}</color>{misfortune}";
    }

    void OnDestroy()
    {
        Lang.Changed -= Refresh;
        GameSession.Founded -= Refresh;
        GameSession.MisfortuneChanged -= Refresh;
        if (clock != null) clock.Changed -= Refresh;
        if (reputation != null) reputation.Changed -= OnReputationChanged;
    }
}
