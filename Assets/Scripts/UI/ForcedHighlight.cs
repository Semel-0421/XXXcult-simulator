using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 강제 이벤트: 화면 전체를 막고, 대상 버튼만 위로 올려 우로보로스 뱀이 주위를 돌게 한다.
public class ForcedHighlight : MonoBehaviour
{
    [SerializeField] GameObject blocker;
    [SerializeField] RectTransform ouroboros;
    [SerializeField] TMP_Text hintLabel;
    [SerializeField] float spinSpeed = -90f; // 초당 각도 (음수 = 시계 방향)

    RectTransform target;
    Canvas raised;

    void Awake() => SetVisible(false);

    public void Begin(RectTransform button, string hint)
    {
        if (target != null) return;
        target = button;
        raised = target.gameObject.AddComponent<Canvas>();
        raised.overrideSorting = true;
        raised.sortingOrder = 100;
        target.gameObject.AddComponent<GraphicRaycaster>();
        hintLabel.text = hint;
        SetVisible(true);
    }

    public void End()
    {
        if (target == null) return;
        Destroy(target.GetComponent<GraphicRaycaster>());
        Destroy(raised);
        target = null;
        SetVisible(false);
    }

    void LateUpdate()
    {
        if (target == null) return;
        ouroboros.position = target.TransformPoint(target.rect.center);
        ouroboros.Rotate(0, 0, spinSpeed * Time.unscaledDeltaTime);
    }

    void SetVisible(bool visible)
    {
        blocker.SetActive(visible);
        ouroboros.gameObject.SetActive(visible);
        hintLabel.gameObject.SetActive(visible);
    }
}
