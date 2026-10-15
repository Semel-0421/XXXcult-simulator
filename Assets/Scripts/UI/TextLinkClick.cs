using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

// 글자 속 <link="id">를 누르면 id를 알려 준다. 글자를 직접 눌러 고르는 안내문에 붙인다.
[RequireComponent(typeof(TMP_Text))]
public class TextLinkClick : MonoBehaviour, IPointerClickHandler
{
    TMP_Text text;
    public event Action<string> Clicked;

    void Awake() => text = GetComponent<TMP_Text>();

    public void OnPointerClick(PointerEventData eventData)
    {
        var index = TMP_TextUtilities.FindIntersectingLink(text, eventData.position, eventData.pressEventCamera);
        if (index >= 0) Clicked?.Invoke(text.textInfo.linkInfo[index].GetLinkID());
    }
}
