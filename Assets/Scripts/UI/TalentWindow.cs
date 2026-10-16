using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 보유 기프티드를 작은 사각 아이콘 격자로 보여주는 창. 스크롤로 내려 보고, 아이콘에 커서를 올리면 설명이 뜬다.
public class TalentWindow : MonoBehaviour
{
    const float CellSize = 76, Spacing = 12, Padding = 14;

    [SerializeField] GameObject window, emptyLabel;
    [SerializeField] RectTransform content;
    [SerializeField] TMP_Text fontSource; // 설명창이 같은 글꼴을 쓰도록

    GiftedTooltip tooltip;

    void Start()
    {
        BuildGrid();
        tooltip = GiftedTooltip.Create((RectTransform)window.transform, fontSource != null ? fontSource.font : null);
        window.SetActive(false);
        if (GameSession.GodMode) GameObject.Find("TalentButton")?.SetActive(false); // 신 모드엔 기프티드가 없다
    }

    // 세로 목록이던 내용 영역을 격자로 바꾼다
    void BuildGrid()
    {
        foreach (Transform child in content) Destroy(child.gameObject);
        var vertical = content.GetComponent<VerticalLayoutGroup>();
        if (vertical != null) DestroyImmediate(vertical);

        var grid = content.GetComponent<GridLayoutGroup>() ?? content.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(CellSize, CellSize);
        grid.spacing = new Vector2(Spacing, Spacing);
        grid.padding = new RectOffset((int)Padding, (int)Padding, (int)Padding, (int)Padding);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.childAlignment = TextAnchor.UpperLeft;
    }

    public void Open()
    {
        Refresh();
        window.SetActive(true);
    }

    public void Close()
    {
        if (tooltip != null) tooltip.Hide();
        window.SetActive(false);
    }

    void Refresh()
    {
        foreach (Transform child in content) Destroy(child.gameObject);

        var owned = GameSession.Personal.Talents.Owned;
        emptyLabel.SetActive(owned.Count == 0);
        foreach (var talent in owned)
        {
            var cell = new GameObject(talent.Name, typeof(RectTransform), typeof(Image), typeof(GiftedCellUI));
            cell.transform.SetParent(content, false);
            cell.GetComponent<GiftedCellUI>().Bind(talent, tooltip);
        }
    }
}
