using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 기프티드 위에 올린 커서 옆에 뜨는 설명창: 이름·분류, 플레이버 텍스트, (히든을 뺀) 효과.
public class GiftedTooltip : MonoBehaviour
{
    const float Width = 380, Offset = 18;

    RectTransform rect, bounds;
    TMP_Text text;

    public static GiftedTooltip Create(RectTransform parent, TMP_FontAsset font)
    {
        var go = new GameObject("GiftedTooltip", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter), typeof(Outline));
        go.transform.SetParent(parent, false);
        var tooltip = go.AddComponent<GiftedTooltip>();
        tooltip.bounds = parent;
        tooltip.rect = (RectTransform)go.transform;
        tooltip.rect.sizeDelta = new Vector2(Width, 10);
        tooltip.rect.anchorMin = tooltip.rect.anchorMax = new Vector2(0.5f, 0.5f);

        var image = go.GetComponent<Image>();
        image.color = new Color(0.07f, 0.05f, 0.12f, 0.97f);
        image.raycastTarget = false; // 커서를 가로채 깜빡이지 않게
        go.GetComponent<Outline>().effectColor = new Color(0.83f, 0.69f, 0.22f, 0.6f);

        var layout = go.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 12, 12);
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;
        go.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var label = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        label.transform.SetParent(go.transform, false);
        tooltip.text = label.GetComponent<TextMeshProUGUI>();
        tooltip.text.fontSize = 20;
        tooltip.text.color = new Color(0.9f, 0.87f, 0.96f);
        tooltip.text.raycastTarget = false;
        if (font != null) tooltip.text.font = UiFonts.Body != null ? UiFonts.Body : font; // 본문은 명조

        go.SetActive(false);
        return tooltip;
    }

    public void Show(Talent talent, Vector2 screenPoint, Camera eventCamera)
    {
        var effects = GiftedInfo.EffectLines(talent);
        var body = $"<b><color=#F2D978>{talent.Name}</color></b>  <size=75%><color=#9A8FB0>{GiftedInfo.CategoryName(talent)}</color></size>\n" +
                   $"<color=#C8C0D8>{talent.Description}</color>";
        if (effects.Count > 0)
            body += $"\n\n<size=80%><color=#9A8FB0>{Lang.Get("talent_effects")}</color></size>\n{string.Join("\n", effects.Select(e => $"<color=#E8E0F4>{e}</color>"))}";
        text.text = body;

        gameObject.SetActive(true);
        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        MoveTo(screenPoint, eventCamera);
    }

    public void MoveTo(Vector2 screenPoint, Camera eventCamera)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(bounds, screenPoint, eventCamera, out var local)) return;
        local -= bounds.rect.center; // 부모 중심 기준으로
        var size = rect.rect.size;
        var half = bounds.rect.size / 2;

        // 기본은 커서 오른쪽 아래, 넘치면 반대쪽으로
        var x = local.x + Offset;
        var y = local.y - Offset;
        if (x + size.x > half.x) x = local.x - Offset - size.x;
        if (y - size.y < -half.y) y = local.y + Offset + size.y;
        x = Mathf.Clamp(x, -half.x, half.x - size.x);
        y = Mathf.Clamp(y, -half.y + size.y, half.y);

        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, y);
    }

    public void Hide() => gameObject.SetActive(false);
}
