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
        if (!root.activeSelf) AudioManager.MenuOpened();
        root.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        if (root == null || !root.activeSelf) return;
        AudioManager.MenuClosed();
        root.SetActive(false);
    }

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
        var content = UiKit.BuildScrollWindow(canvas.rootCanvas.transform, font != null ? font : UiKit.FindFont(), "SaveSlotWindow",
            new Vector2(820, 640), out root, out titleLabel, out hintLabel, out var close);
        close.GetComponentInChildren<TMP_Text>().text = Lang.Get("close");
        close.onClick.AddListener(Close);

        for (int slot = 1; slot <= SaveSlots.MaxSlots; slot++)
            rows[slot - 1] = BuildRow(content, slot);
    }

    Row BuildRow(RectTransform parent, int slot)
    {
        var f = font != null ? font : UiKit.FindFont();
        var rowRect = UiKit.Child(parent, $"Slot{slot:000}", typeof(Image), typeof(LayoutElement));
        rowRect.GetComponent<Image>().color = UiKit.RowColor;
        rowRect.GetComponent<LayoutElement>().preferredHeight = 62;

        var row = new Row();
        row.info = UiKit.Label(rowRect, f, "Info", 21, TextAlignmentOptions.MidlineLeft, new Color(0.9f, 0.87f, 0.96f));
        row.info.richText = true;
        row.info.overflowMode = TextOverflowModes.Ellipsis;
        UiKit.Anchor(row.info.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        row.info.rectTransform.offsetMin = new Vector2(14, 4);
        row.info.rectTransform.offsetMax = new Vector2(-290, -4);

        row.save = UiKit.MakeButton(rowRect, f, "Save", Lang.Get("save_btn"), 88, 44, out row.saveLabel);
        row.load = UiKit.MakeButton(rowRect, f, "Load", Lang.Get("load_btn"), 88, 44, out row.loadLabel);
        row.delete = UiKit.MakeButton(rowRect, f, "Delete", Lang.Get("delete_btn"), 72, 44, out row.deleteLabel);
        UiKit.Anchor((RectTransform)row.delete.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-46, 0), new Vector2(72, 44));
        UiKit.Anchor((RectTransform)row.load.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-132, 0), new Vector2(88, 44));
        UiKit.Anchor((RectTransform)row.save.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-226, 0), new Vector2(88, 44));

        var captured = slot;
        row.save.onClick.AddListener(() => OnSave(captured));
        row.load.onClick.AddListener(() => OnLoad(captured));
        row.delete.onClick.AddListener(() => OnDelete(captured));
        return row;
    }
}
