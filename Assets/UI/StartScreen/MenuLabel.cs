using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 타이틀 메뉴 버튼 글자를 미리 구운 그림(Docs/gen_menu_labels.py -> Resources/UI/MenuLabels)으로 보여 준다.
// 언어가 바뀌면 그림을 바꾸고, 잠긴 버튼은 회색 취소선 그림 위에 금색 그림을 진행도만큼 왼쪽부터 채운다 (글자가 하나씩 금빛으로 돌아오던 힌트).
public class MenuLabel : MonoBehaviour
{
    string key;
    Image image, lit;
    TMP_Text count; // 잠김 진행 (n/m). 숫자·괄호는 고딕 폰트에 있다
    Func<bool> locked;
    Func<float> progress;
    Func<string> countText;

    // label(TMP)을 끄고 같은 자리에 그림 글자를 놓는다
    public static MenuLabel Replace(TMP_Text label, string key, Func<bool> locked = null, Func<float> progress = null, Func<string> countText = null)
    {
        var parent = label.transform.parent;
        var rect = UiKit.Child(parent, "LabelImage", typeof(Image));
        rect.SetSiblingIndex(label.transform.GetSiblingIndex());
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        var menuLabel = rect.gameObject.AddComponent<MenuLabel>();
        menuLabel.key = key;
        menuLabel.locked = locked;
        menuLabel.progress = progress;
        menuLabel.countText = countText;
        menuLabel.image = rect.GetComponent<Image>();
        menuLabel.image.raycastTarget = false;
        if (locked != null)
        {
            var litRect = UiKit.Child(rect, "Lit", typeof(Image));
            litRect.anchorMin = Vector2.zero; litRect.anchorMax = Vector2.one; litRect.sizeDelta = Vector2.zero;
            menuLabel.lit = litRect.GetComponent<Image>();
            menuLabel.lit.raycastTarget = false;
            menuLabel.lit.type = Image.Type.Filled;
            menuLabel.lit.fillMethod = Image.FillMethod.Horizontal;
            menuLabel.lit.fillOrigin = key == "mirror_mode" ? (int)Image.OriginHorizontal.Right : (int)Image.OriginHorizontal.Left; // 반전 글자는 첫 글자가 오른쪽
            // 진행 숫자는 원래 TMP를 그림 오른쪽에 붙여 쓴다
            label.transform.SetParent(rect, false);
            var countRect = label.rectTransform;
            countRect.anchorMin = countRect.anchorMax = new Vector2(1, 0.5f);
            countRect.pivot = new Vector2(0, 0.5f);
            countRect.anchoredPosition = new Vector2(8, 0);
            countRect.sizeDelta = new Vector2(120, 50);
            countRect.localScale = Vector3.one;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.enableAutoSizing = false;
            label.fontSize *= 0.7f;
            label.raycastTarget = false;
            menuLabel.count = label;
        }
        else label.gameObject.SetActive(false);
        Lang.Changed += menuLabel.Refresh;
        menuLabel.Refresh();
        return menuLabel;
    }

    void OnDestroy() => Lang.Changed -= Refresh;

    static Sprite Load(string name)
    {
        var texture = Resources.Load<Texture2D>($"UI/MenuLabels/{name}");
        return texture == null ? null : Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
    }

    public void Refresh()
    {
        var lang = Lang.IsKorean ? "ko" : "en";
        var isLocked = locked != null && locked();
        image.sprite = Load(isLocked ? $"{key}_{lang}_locked" : $"{key}_{lang}");
        if (image.sprite == null) return;
        // 그림은 36pt의 2배로 구웠다. 버튼보다 넓으면 줄인다
        var size = image.sprite.rect.size / 2f;
        var maxWidth = ((RectTransform)transform.parent).rect.width - 20;
        if (maxWidth > 0 && size.x > maxWidth) size *= maxWidth / size.x;
        ((RectTransform)transform).sizeDelta = size;
        if (lit == null) return;
        lit.gameObject.SetActive(isLocked);
        count.gameObject.SetActive(isLocked);
        if (!isLocked) return;
        lit.sprite = Load($"{key}_{lang}_lit");
        lit.fillAmount = progress();
        count.text = countText();
    }
}
