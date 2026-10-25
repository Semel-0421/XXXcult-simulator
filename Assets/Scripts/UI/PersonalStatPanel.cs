using TMPro;
using UnityEngine;

public class PersonalStatPanel : MonoBehaviour
{
    [SerializeField] TMP_Text nameLabel;
    [SerializeField] PersonalStatRowUI[] rows;
    PersonalStatus status;
    GameClock age;

    static readonly PersonalStatId[] TraitStats =
    {
        PersonalStatId.Lust, PersonalStatId.Greed, PersonalStatId.Appetite, PersonalStatId.Ambition,
        PersonalStatId.Temperance, PersonalStatId.Charity, PersonalStatId.Patience, PersonalStatId.Integrity,
    };

    public void Show(PersonalStatus personal)
    {
        status = personal;
        age = GameSession.Age;
        age.Changed += RefreshName;
        Lang.Changed += RefreshName;
        foreach (var id in TraitStats) status[id].Changed += OnTraitChanged;
        if (GameSession.GodMode) GameSession.Cult[StatId.Communion].Changed += OnTraitChanged;
        RefreshName();
        foreach (var row in rows) row.Bind(personal);
    }

    void OnTraitChanged(int _) => RefreshName();

    // 이름 · 나이 · 몇 번째 생. 5번째 생부터는 덕목·욕망 평균도 함께 보여 준다 (최종 엔딩의 굿/배드 판정 기준)
    // 준비된 신 모드: 생 번호 대신 모드 이름, 평균 대신 접신 게이지 (이 모드에선 공개 자원)
    void RefreshName()
    {
        if (GameSession.GodMode)
        {
            var communion = GameSession.Cult[StatId.Communion].Value; // 글꼴에 ●○ 글리프가 없어 숫자로
            nameLabel.text = $"{status.Name} <size=60%>{string.Format(Lang.Get("age"), age.Years)} · {Lang.Get(GameSession.EyeMode ? "eye_mode" : "god_mode")}</size>"
                + $"\n<size=45%>{Lang.Get("stat_Communion")} {communion}/{CultStatus.CommunionMax}</size>";
            return;
        }
        var text = $"{status.Name} <size=60%>{string.Format(Lang.Get("age"), age.Years)} · {string.Format(Lang.Get("run_label"), Progress.CurrentRun)}</size>";
        if (Progress.ShowsAverages)
        {
            var virtue = CultEvent.TraitAverage(status, CultEvent.MoodTrait.Virtue);
            var desire = CultEvent.TraitAverage(status, CultEvent.MoodTrait.Desire);
            text += $"\n<size=45%>{string.Format(Lang.Get("trait_averages"), virtue.ToString("0.#"), desire.ToString("0.#"))}</size>";
        }
        nameLabel.text = text;
    }

    void OnDestroy()
    {
        Lang.Changed -= RefreshName;
        if (age != null) age.Changed -= RefreshName;
        if (status != null) foreach (var id in TraitStats) status[id].Changed -= OnTraitChanged;
        GameSession.Cult[StatId.Communion].Changed -= OnTraitChanged;
    }
}
