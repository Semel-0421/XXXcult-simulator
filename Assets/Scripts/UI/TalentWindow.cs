using UnityEngine;

// 보유 기프티드를 보여주는 창. 열 때마다 현재 목록으로 다시 그린다.
public class TalentWindow : MonoBehaviour
{
    [SerializeField] GameObject window, emptyLabel;
    [SerializeField] Transform content;
    [SerializeField] TalentRowUI rowTemplate;

    void Start()
    {
        rowTemplate.gameObject.SetActive(false);
        window.SetActive(false);
    }

    public void Open()
    {
        Refresh();
        window.SetActive(true);
    }

    public void Close() => window.SetActive(false);

    void Refresh()
    {
        foreach (Transform child in content)
            if (child != rowTemplate.transform) Destroy(child.gameObject);

        var owned = GameSession.Personal.Talents.Owned;
        emptyLabel.SetActive(owned.Count == 0);
        foreach (var talent in owned)
        {
            var row = Instantiate(rowTemplate, content);
            row.gameObject.SetActive(true);
            row.Bind(talent);
        }
    }
}
