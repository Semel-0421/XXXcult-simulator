using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 보유 기프티드 한 칸(작은 사각 아이콘). 커서를 올리면 설명창을 띄우고, 터치에서는 눌러서 연다.
[RequireComponent(typeof(Image))]
public class GiftedCellUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler, IPointerClickHandler
{
    Talent talent;
    GiftedTooltip tooltip;

    public void Bind(Talent owned, GiftedTooltip sharedTooltip)
    {
        talent = owned;
        tooltip = sharedTooltip;
        GetComponent<Image>().sprite = GiftedIcon.Of(owned);
    }

    public void OnPointerEnter(PointerEventData e) => tooltip.Show(talent, e.position, e.enterEventCamera);
    public void OnPointerMove(PointerEventData e) { if (tooltip.gameObject.activeSelf) tooltip.MoveTo(e.position, e.enterEventCamera); }
    public void OnPointerExit(PointerEventData e) => tooltip.Hide();
    public void OnPointerClick(PointerEventData e) => tooltip.Show(talent, e.position, e.pressEventCamera);

    void OnDisable() { if (tooltip != null) tooltip.Hide(); }
}
