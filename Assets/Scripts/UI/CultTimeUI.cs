using TMPro;
using UnityEngine;

// 교단 스탯 바 아래에 교단 창설 이후 시간을 표시한다.
[RequireComponent(typeof(TMP_Text))]
public class CultTimeUI : MonoBehaviour
{
    TMP_Text label;
    GameClock clock;

    void Start()
    {
        label = GetComponent<TMP_Text>();
        clock = GameSession.CultTime;
        clock.Changed += Refresh;
        Lang.Changed += Refresh;
        Refresh();
    }

    void Refresh() => label.text = string.Format(Lang.Get("cult_time"), clock.Years, clock.Quarter);

    void OnDestroy()
    {
        Lang.Changed -= Refresh;
        if (clock != null) clock.Changed -= Refresh;
    }
}
