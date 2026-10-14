using UnityEngine;
using TMPro;

public class StatBarUI : MonoBehaviour
{
    [SerializeField] StatId statId;
    [SerializeField] TMP_Text nameLabel, valueLabel;
    ClampedStat stat;

    public void Bind(CultStatus status)
    {
        stat = status[statId];
        stat.Changed += OnStatChanged;
        Lang.Changed += Refresh;
        Refresh();
    }

    void OnStatChanged(int _) => Refresh();

    void Refresh()
    {
        nameLabel.text = StatLabel.Of(statId);
        valueLabel.text = statId == StatId.Doctrine ? DoctrineLevel.Label(stat.Value) : stat.Value.ToString("N0");
    }

    void OnDestroy()
    {
        Lang.Changed -= Refresh;
        if (stat != null) stat.Changed -= OnStatChanged;
    }
}
