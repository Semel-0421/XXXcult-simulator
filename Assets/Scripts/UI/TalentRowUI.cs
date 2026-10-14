using TMPro;
using UnityEngine;

public class TalentRowUI : MonoBehaviour
{
    [SerializeField] TMP_Text nameLabel, descriptionLabel;

    public void Bind(Talent talent)
    {
        nameLabel.text = talent.Name;
        descriptionLabel.text = talent.Description;
    }
}
