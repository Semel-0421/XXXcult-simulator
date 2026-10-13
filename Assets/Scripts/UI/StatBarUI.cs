using UnityEngine;
using TMPro;

public class StatBarUI : MonoBehaviour
{
    [SerializeField] StatId statId;
    [SerializeField] TMP_Text nameLabel, valueLabel;
    CultStatus status;

    public void Bind(CultStatus s)
    {
        status = s;
        status[statId].Changed += _ => Refresh();
        Refresh();
    }

    void Refresh()
    {
        nameLabel.text = StatLabel.Of(statId);
        var v = status[statId].Value;
        valueLabel.text = statId == StatId.Doctrine ? DoctrineLevel.Label(v) : v.ToString("N0");
    }

    void OnEnable() { if (status != null) Refresh(); }
}
