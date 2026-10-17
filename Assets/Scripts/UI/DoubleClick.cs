using System;
using UnityEngine;
using UnityEngine.EventSystems;

// 더블 클릭(모바일은 더블 탭)을 받는다. 클릭만 받으므로 부모의 드래그·스크롤은 막지 않는다.
public class DoubleClick : MonoBehaviour, IPointerClickHandler
{
    public event Action DoubleClicked;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left && eventData.clickCount == 2) DoubleClicked?.Invoke();
    }
}
