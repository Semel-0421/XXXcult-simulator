using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 엔딩 도감: 가능한 모든 엔딩을 타일로 보여 준다. 본 엔딩은 삽화와 이름을, 못 본 엔딩은 검게 가리고 ?로 둔다.
// 타일에 마우스를 올리면 엔딩 조건을 수수께끼 문장으로 보여 주고, 본 엔딩을 누르면 크게 보는 창(EndingDetail)이 열린다.
public static class EndingGallery
{
    sealed class Tile
    {
        public Endings.Ending ending;
        public Image frame, picture, cover;
        public TMP_Text label;
    }

    static GameObject root;
    static TMP_Text titleLabel, hintLabel, closeLabel;
    static RectTransform content, canvasRect, tooltipRect;
    static TMP_Text tooltipText;
    static TMP_FontAsset font;
    static readonly List<Tile> tiles = new();

    public static void Open()
    {
        if (root == null) Build();
        Refresh();
        if (!root.activeSelf) Menus.Opened();
        root.SetActive(true);
    }

    public static void Close()
    {
        if (root == null || !root.activeSelf) return;
        HideTooltip();
        EndingDetail.Close();
        Menus.Closed();
        root.SetActive(false);
    }

    static void Build()
    {
        font = UiKit.FindFont();
        var canvas = UiKit.CreateOverlayCanvas("EndingGalleryCanvas", 310);
        canvasRect = (RectTransform)canvas;
        content = UiKit.BuildScrollWindow(canvas, font, "EndingGallery", new Vector2(1040, 680),
            out root, out titleLabel, out hintLabel, out var close);
        closeLabel = close.GetComponentInChildren<TMP_Text>();
        close.onClick.AddListener(Close);

        var panel = (RectTransform)root.transform.Find("Panel");
        UiSkin.ApplyBookFrame(panel.GetComponent<Image>(), "EndingGalleryBook", new Color(0.4f, 0.1f, 0.26f), new Color(0.2f, 0.06f, 0.34f));
        panel.Find("Close").GetComponent<Image>().color = UiSkin.Crimson;

        // 세로 목록 대신 격자로 배치한다
        Object.Destroy(content.GetComponent<VerticalLayoutGroup>());
        var grid = content.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(160, 200);
        grid.spacing = new Vector2(12, 12);
        grid.padding = new RectOffset(10, 10, 10, 10);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 6;

        for (int i = 0; i < Endings.All.Count; i++) tiles.Add(BuildTile(Endings.All[i], i));
        BuildTooltip();
        EndingDetail.Build(canvas, font);
    }

    static Tile BuildTile(Endings.Ending ending, int index)
    {
        var rect = UiKit.Child(content, $"Ending{index + 1:00}", typeof(Image));
        var tile = new Tile { ending = ending, frame = rect.GetComponent<Image>() };
        tile.frame.color = new Color(0.16f, 0.07f, 0.24f, 0.95f);

        tile.picture = UiKit.Child(rect, "Picture", typeof(Image)).GetComponent<Image>();
        UiKit.Anchor(tile.picture.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        tile.picture.rectTransform.offsetMin = new Vector2(14, 44);
        tile.picture.rectTransform.offsetMax = new Vector2(-14, -12);
        tile.picture.raycastTarget = false;

        tile.label = UiKit.Label(rect, font, "Name", 17, TextAlignmentOptions.Bottom, UiSkin.GoldLight);
        UiKit.Anchor(tile.label.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        tile.label.rectTransform.offsetMin = new Vector2(6, 4);
        tile.label.rectTransform.offsetMax = new Vector2(-6, -150);
        tile.label.raycastTarget = false;

        // 못 본 엔딩을 가리는 검은 덮개와 물음표
        tile.cover = UiKit.Child(rect, "Cover", typeof(Image)).GetComponent<Image>();
        UiKit.Stretch(tile.cover.rectTransform);
        tile.cover.color = new Color(0, 0, 0, 0.94f);
        var mark = UiKit.Label(tile.cover.rectTransform, font, "Mark", 80, TextAlignmentOptions.Center, new Color(0.5f, 0.45f, 0.56f));
        mark.text = "?";
        UiKit.Stretch(mark.rectTransform);

        var trigger = rect.gameObject.AddComponent<EventTrigger>();
        AddTrigger(trigger, EventTriggerType.PointerEnter, data => ShowTooltip(ending, (PointerEventData)data));
        AddTrigger(trigger, EventTriggerType.PointerExit, _ => HideTooltip());
        AddTrigger(trigger, EventTriggerType.PointerClick, _ =>
        {
            if (Endings.Seen(ending.Id)) EndingDetail.Open(ending);
        });
        return tile;
    }

    static void AddTrigger(EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> action)
    {
        var entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(action);
        trigger.triggers.Add(entry);
    }

    static void BuildTooltip()
    {
        var box = UiKit.Child(root.transform, "Tooltip", typeof(Image));
        tooltipRect = box;
        box.sizeDelta = new Vector2(400, 150);
        box.GetComponent<Image>().color = new Color(0.1f, 0.04f, 0.16f, 0.97f);
        box.GetComponent<Image>().raycastTarget = false;
        tooltipText = UiKit.Label(box, font, "Text", 21, TextAlignmentOptions.TopLeft, new Color(0.92f, 0.88f, 0.98f));
        UiKit.Stretch(tooltipText.rectTransform);
        tooltipText.rectTransform.offsetMin = new Vector2(16, 12);
        tooltipText.rectTransform.offsetMax = new Vector2(-16, -12);
        tooltipText.raycastTarget = false;
        box.gameObject.SetActive(false);
    }

    static void ShowTooltip(Endings.Ending ending, PointerEventData data)
    {
        tooltipText.text = $"<color=#F2D978>{Lang.Get("quiz_hint")}</color>\n{EndingQuiz.Of(ending)}";
        tooltipRect.gameObject.SetActive(true);
        tooltipRect.SetAsLastSibling();
        MoveTooltip(data);
    }

    // 마우스 옆에 붙이되 화면 밖으로 나가면 왼쪽·위로 밀어 넣는다
    static void MoveTooltip(PointerEventData data)
    {
        if (data == null || !tooltipRect.gameObject.activeSelf) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, data.position, null, out var local);
        var half = canvasRect.rect.size / 2;
        var pos = local + new Vector2(18, -18);
        if (pos.x + tooltipRect.sizeDelta.x > half.x) pos.x = local.x - tooltipRect.sizeDelta.x - 18;
        if (pos.y - tooltipRect.sizeDelta.y < -half.y) pos.y = -half.y + tooltipRect.sizeDelta.y;
        tooltipRect.anchoredPosition = pos;
    }

    static void HideTooltip()
    {
        if (tooltipRect != null) tooltipRect.gameObject.SetActive(false);
    }

    static void Refresh()
    {
        titleLabel.text = Lang.Get("gallery_title");
        closeLabel.text = Lang.Get("close");
        hintLabel.text = string.Format(Lang.Get("ending_progress"), Endings.SeenCount, Endings.All.Count);
        foreach (var tile in tiles)
        {
            var seen = Endings.Seen(tile.ending.Id);
            tile.cover.gameObject.SetActive(!seen);
            tile.picture.gameObject.SetActive(seen);
            if (seen)
            {
                tile.picture.sprite = EndingIllustrations.Get(tile.ending);
                tile.picture.color = EndingIllustrations.ColorOf(tile.ending);
                tile.frame.color = tile.ending.Kind switch
                {
                    EndingKind.Good => new Color(0.32f, 0.22f, 0.1f, 0.96f),
                    EndingKind.Bad => new Color(0.36f, 0.08f, 0.14f, 0.96f),
                    _ => new Color(0.24f, 0.12f, 0.34f, 0.96f),
                };
            }
            tile.label.text = seen ? Endings.DisplayName(tile.ending) : Lang.Get("ending_unseen");
            tile.label.color = seen ? UiSkin.GoldLight : new Color(0.55f, 0.5f, 0.6f);
        }
    }
}
