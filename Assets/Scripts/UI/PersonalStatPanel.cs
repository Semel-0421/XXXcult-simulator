using TMPro;
using UnityEngine;

public class PersonalStatPanel : MonoBehaviour
{
    [SerializeField] TMP_Text nameLabel;
    [SerializeField] PersonalStatRowUI[] rows;

    public void Show(PersonalStatus status)
    {
        nameLabel.text = status.Name;
        foreach (var row in rows) row.Bind(status);
    }
}
