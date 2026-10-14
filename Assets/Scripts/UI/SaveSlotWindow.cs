using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 저장 칸 100개 창. 칸마다 저장/불러오기/삭제 버튼이 있고, 스크롤로 내려 본다.
// 덮어쓰기·삭제는 같은 버튼을 한 번 더 눌러야 실행된다. UI는 코드로 만들어 두 씬 어디에나 붙일 수 있다.
public class SaveSlotWindow : MonoBehaviour
{
    enum Pending { None, Overwrite, Delete }

    sealed class Row
    {
        public TMP_Text info, saveLabel, loadLabel, deleteLabel;
        public Button save, load, delete;
    }

    static readonly Color PanelColor = new(0.09f, 0.07f, 0.14f, 0.98f);
    static readonly Color RowColor = new(0.15f, 0.12f, 0.22f, 1f);
    static readonly Color ButtonColor = new(0.30f, 0.24f, 0.46f, 1f);
    static readonly Color Gold = new(0.95f, 0.85f, 0.55f);

    [SerializeField] TMP_FontAsset font;
    [SerializeField] string gameScene = "GameMain";

    GameObject root;
    TMP_Text titleLabel, hintLabel;
    readonly Row[] rows = new Row[SaveSlots.MaxSlots];
    bool canSave;
    int pendingSlot;
    Pending pending;

    // canSave: 저장은 행동을 고르는 시점에만 허용 (타이틀에서는 불러오기만)
    public void Open(bool allowSave)
    {
        if (root == null) Build();
        canSave = allowSave;
        pending = Pending.None;
        root.SetActive(true);
        Refresh();
    }

    public void Close() { if (root != null) root.SetActive(false); }

    void Refresh()
    {
        titleLabel.text = Lang.Get("save_title");
        hintLabel.text = Lang.Get(canSave ? "save_hint" : "save_hint_locked");
        for (int slot = 1; slot <= SaveSlots.MaxSlots; slot++)
        {
            var row = rows[slot - 1];
            var data = SaveSlots.Read(slot);
            var exists = data != null || SaveSlots.Exists(slot); // 깨진 파일도 지울 수 있게

            row.info.text = Describe(slot, data, exists);
            var overwrite = pending == Pending.Overwrite && pendingSlot == slot;
            var delete = pending == Pending.Delete && pendingSlot == slot;
            row.saveLabel.text = Lang.Get(overwrite ? "save_confirm" : "save_btn");
            row.loadLabel.text = Lang.Get("load_btn");
            row.deleteLabel.text = Lang.Get(delete ? "save_confirm" : "delete_btn");
            row.save.interactable = canSave;
            row.load.interactable = data != null;
            row.delete.interactable = exists;
        }
    }

    static string Describe(int slot, SaveData data, bool exists)
    {
        var number = slot.ToString("000");
        if (data == null)
            return $"<color=#6F6585><b>{number}</b>  {Lang.Get(exists ? "save_broken" : "save_empty")}</color>";

        var age = string.Format(Lang.Get("age"), data.ageTurns / GameClock.QuartersPerYear);
        var summary = data.founded
            ? $"{data.playerName} · {data.cultName} · {age} · {string.Format(Lang.Get("cult_time"), data.cultTurns / GameClock.QuartersPerYear, data.cultTurns % GameClock.QuartersPerYear)}"
            : $"{data.playerName} · {age}";
        return $"<color=#F2D978><b>{number}</b></color>  {summary}\n<size=70%><color=#9A8FB0>{data.savedAt}</color></size>";
    }

    void OnSave(int slot)
    {
        if (!canSave) return;
        if (SaveSlots.Exists(slot) && !(pending == Pending.Overwrite && pendingSlot == slot))
        {
            pending = Pending.Overwrite;
            pendingSlot = slot;
            Refresh();
            return;
        }
        SaveSlots.Write(slot, GameSession.Capture());
        pending = Pending.None;
        Refresh();
    }

    void OnLoad(int slot)
    {
        var data = SaveSlots.Read(slot);
        if (data == null) return;
        GameSession.Restore(data);
        Time.timeScale = 1;
        SceneManager.LoadScene(gameScene);
    }

    void OnDelete(int slot)
    {
        if (!(pending == Pending.Delete && pendingSlot == slot))
        {
            pending = Pending.Delete;
            pendingSlot = slot;
            Refresh();
            return;
        }
        SaveSlots.Delete(slot);
        pending = Pending.None;
        Refresh();
    }

    // ---- UI 생성 ----

    void Build()
    {
        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
        root = new GameObject("SaveSlotWindow", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        root.transform.SetParent(canvas.rootCanvas.transform, false);
        Stretch((RectTransform)root.transform);
        var overlay = root.GetComponent<Canvas>();
        overlay.overrideSorting = true;
        overlay.sortingOrder = 300;

        var dim = Child(root.transform, "Dim", typeof(Image));
        Stretch(dim);
        dim.GetComponent<Image>().color = new Color(0, 0, 0, 0.75f);

        var panel = Child(root.transform, "Panel", typeof(Image), typeof(Outline));
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = new Vector2(820, 640);
        panel.GetComponent<Image>().color = PanelColor;
        panel.GetComponent<Outline>().effectColor = new Color(0.95f, 0.85f, 0.55f, 0.5f);

        titleLabel = Label(panel, "Title", 34, TextAlignmentOptions.Center, Gold);
        Anchor(titleLabel.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -41), new Vector2(0, 50));

        hintLabel = Label(panel, "Hint", 20, TextAlignmentOptions.Center, new Color(0.6f, 0.56f, 0.7f));
        Anchor(hintLabel.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -84), new Vector2(0, 30));

        var close = MakeButton(panel, "Close", "close", 120, 44);
        Anchor((RectTransform)close.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-80, -34), new Vector2(120, 44));
        close.onClick.AddListener(Close);

        var scroll = Child(panel, "Scroll", typeof(Image), typeof(ScrollRect));
        Anchor(scroll, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        scroll.offsetMin = new Vector2(18, 18);
        scroll.offsetMax = new Vector2(-18, -108);
        scroll.GetComponent<Image>().color = new Color(0, 0, 0, 0.25f);

        var viewport = Child(scroll, "Viewport", typeof(RectMask2D));
        Stretch(viewport);
        var content = Child(viewport, "Content", typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = new Vector2(1, 1);
        content.pivot = new Vector2(0.5f, 1);
        content.offsetMin = content.offsetMax = Vector2.zero;
        var layout = content.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.spacing = 6;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scrollRect = scroll.GetComponent<ScrollRect>();
        scrollRect.viewport = viewport;
        scrollRect.content = content;
        scrollRect.horizontal = false;
        scrollRect.scrollSensitivity = 40;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        for (int slot = 1; slot <= SaveSlots.MaxSlots; slot++)
            rows[slot - 1] = BuildRow(content, slot);

        root.SetActive(false);
    }

    Row BuildRow(RectTransform parent, int slot)
    {
        var rowRect = Child(parent, $"Slot{slot:000}", typeof(Image), typeof(LayoutElement));
        rowRect.GetComponent<Image>().color = RowColor;
        rowRect.GetComponent<LayoutElement>().preferredHeight = 62;

        var row = new Row();
        row.info = Label(rowRect, "Info", 21, TextAlignmentOptions.MidlineLeft, new Color(0.9f, 0.87f, 0.96f));
        row.info.richText = true;
        row.info.overflowMode = TextOverflowModes.Ellipsis;
        Anchor(row.info.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        row.info.rectTransform.offsetMin = new Vector2(14, 4);
        row.info.rectTransform.offsetMax = new Vector2(-290, -4);

        row.save = MakeButton(rowRect, "Save", "save_btn", 88, 44, out row.saveLabel);
        row.load = MakeButton(rowRect, "Load", "load_btn", 88, 44, out row.loadLabel);
        row.delete = MakeButton(rowRect, "Delete", "delete_btn", 72, 44, out row.deleteLabel);
        Anchor((RectTransform)row.delete.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-46, 0), new Vector2(72, 44));
        Anchor((RectTransform)row.load.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-132, 0), new Vector2(88, 44));
        Anchor((RectTransform)row.save.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-226, 0), new Vector2(88, 44));

        var captured = slot;
        row.save.onClick.AddListener(() => OnSave(captured));
        row.load.onClick.AddListener(() => OnLoad(captured));
        row.delete.onClick.AddListener(() => OnDelete(captured));
        return row;
    }

    Button MakeButton(RectTransform parent, string name, string labelKey, float width, float height) =>
        MakeButton(parent, name, labelKey, width, height, out _);

    Button MakeButton(RectTransform parent, string name, string labelKey, float width, float height, out TMP_Text label)
    {
        var rect = Child(parent, name, typeof(Image), typeof(Button));
        rect.sizeDelta = new Vector2(width, height);
        var image = rect.GetComponent<Image>();
        image.color = ButtonColor;
        var button = rect.GetComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.highlightedColor = new Color(1.2f, 1.2f, 1.3f);
        colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.45f);
        button.colors = colors;

        label = Label(rect, "Label", 20, TextAlignmentOptions.Center, Color.white);
        Stretch(label.rectTransform);
        label.text = Lang.Get(labelKey);
        label.raycastTarget = false;
        return button;
    }

    TMP_Text Label(RectTransform parent, string name, float size, TextAlignmentOptions align, Color color)
    {
        var rect = Child(parent, name, typeof(TextMeshProUGUI));
        var text = rect.GetComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.fontSize = size;
        text.alignment = align;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    static RectTransform Child(Transform parent, string name, params System.Type[] components)
    {
        var go = new GameObject(name, typeof(RectTransform));
        foreach (var type in components) go.AddComponent(type);
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    static void Anchor(RectTransform rect, Vector2 min, Vector2 max, Vector2 position, Vector2 size)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
