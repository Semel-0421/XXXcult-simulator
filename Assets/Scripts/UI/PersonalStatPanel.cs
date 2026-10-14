using TMPro;
using UnityEngine;

public class PersonalStatPanel : MonoBehaviour
{
    [SerializeField] TMP_Text nameLabel;
    [SerializeField] PersonalStatRowUI[] rows;
    PersonalStatus status;
    GameClock age;

    public void Show(PersonalStatus personal)
    {
        status = personal;
        age = GameSession.Age;
        age.Changed += RefreshName;
        Lang.Changed += RefreshName;
        RefreshName();
        foreach (var row in rows) row.Bind(personal);
    }

    void RefreshName() => nameLabel.text = $"{status.Name} <size=60%>{string.Format(Lang.Get("age"), age.Years)}</size>";

    void OnDestroy()
    {
        Lang.Changed -= RefreshName;
        if (age != null) age.Changed -= RefreshName;
    }
}
