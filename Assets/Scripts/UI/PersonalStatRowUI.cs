using TMPro;
using UnityEngine;

public class PersonalStatRowUI : MonoBehaviour
{
    [SerializeField] PersonalStatId statId;
    [SerializeField] TMP_Text nameLabel, valueLabel;
    ClampedStat stat;

    public void Bind(PersonalStatus status)
    {
        stat = status[statId];
        stat.Changed += OnStatChanged;
        Lang.Changed += Refresh;
        Refresh();
    }

    void OnStatChanged(int _) => Refresh();

    void Refresh()
    {
        nameLabel.text = Lang.Get($"pstat_{statId}");
        valueLabel.text = stat.Value.ToString();
    }

    void OnDestroy()
    {
        Lang.Changed -= Refresh;
        if (stat != null) stat.Changed -= OnStatChanged;
    }
}
