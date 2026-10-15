using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 업적 100개 목록 창. 달성한 업적은 밝게, 못 한 업적은 흐리게 보여 주고 보상을 함께 적는다.
// 장면에 따로 붙일 것 없이 AchievementWindow.Open()만 부르면 코드로 만들어 연다.
public static class AchievementWindow
{
    sealed class Row
    {
        public Image background;
        public LayoutElement layout;
        public TMP_Text name, description, reward;
    }

    // 글씨 확대 단계 (기본 1배). 설명 문구 내용은 그대로 두고 크기만 키운다.
    static readonly float[] ZoomLevels = { 1f, 1.25f, 1.5f, 1.75f };
    const float BaseRowHeight = 76, BaseNameSize = 24, BaseTextSize = 20;

    static GameObject root;
    static TMP_Text titleLabel, hintLabel, closeLabel, rewardsLabel;
    static Button zoomInButton, zoomOutButton;
    static Row[] rows;

    static int ZoomIndex
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt("ach_zoom", 0), 0, ZoomLevels.Length - 1);
        set { PlayerPrefs.SetInt("ach_zoom", value); PlayerPrefs.Save(); }
    }

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
        Menus.Closed();
        root.SetActive(false);
    }

    static void Build()
    {
        var font = UiKit.FindFont();
        var canvas = UiKit.CreateOverlayCanvas("AchievementCanvas", 300);
        var content = UiKit.BuildScrollWindow(canvas, font, "AchievementWindow", new Vector2(980, 660),
            out root, out titleLabel, out hintLabel, out var close);
        closeLabel = close.GetComponentInChildren<TMP_Text>();
        close.onClick.AddListener(Close);

        // 왕실 장식: 자줏빛·진홍 비단 표지에 금실 테두리, 제목을 황금 월계관이 감싼다
        var panel = (RectTransform)root.transform.Find("Panel");
        UiSkin.ApplyBookFrame(panel.GetComponent<Image>(), "AchievementBook", new Color(0.42f, 0.1f, 0.26f), new Color(0.22f, 0.06f, 0.36f));
        var laurel = UiKit.Child(panel, "Laurel", typeof(Image));
        laurel.anchorMin = laurel.anchorMax = new Vector2(0.5f, 1);
        laurel.anchoredPosition = new Vector2(0, -48);
        laurel.sizeDelta = new Vector2(150, 150);
        var laurelImage = laurel.GetComponent<Image>();
        laurelImage.sprite = UiSkin.Laurel();
        laurelImage.preserveAspect = true;
        laurelImage.raycastTarget = false;
        laurel.SetAsFirstSibling(); // 글자는 월계관 위에 보이도록
        // 월계관 아래로 진행 문구와 목록 위치를 내린다
        ((RectTransform)panel.Find("Scroll")).offsetMax = new Vector2(-18, -162);
        UiKit.Anchor(hintLabel.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -136), new Vector2(0, 30));
        // 버튼도 진홍색으로
        panel.Find("Close").GetComponent<Image>().color = UiSkin.Crimson;

        // 보상 켜기/끄기 버튼 (제목 왼쪽)
        var toggle = UiKit.MakeButton(panel, font, "RewardsToggle", "", 200, 44, out rewardsLabel);
        toggle.image.color = UiSkin.Crimson;
        UiKit.Anchor((RectTransform)toggle.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(120, -34), new Vector2(200, 44));
        toggle.onClick.AddListener(() =>
        {
            Profile.RewardsEnabled = !Profile.RewardsEnabled;
            Refresh();
        });

        // 글씨 확대/축소 (제목 오른쪽 위, 닫기 버튼 왼쪽)
        zoomInButton = UiKit.MakeButton(panel, font, "ZoomIn", "A+", 110, 44, out _);
        UiKit.Anchor((RectTransform)zoomInButton.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-210, -34), new Vector2(110, 44));
        zoomInButton.onClick.AddListener(() => { ZoomIndex = ZoomIndex + 1; ApplyZoom(); });
        zoomOutButton = UiKit.MakeButton(panel, font, "ZoomOut", "A-", 110, 44, out _);
        UiKit.Anchor((RectTransform)zoomOutButton.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-330, -34), new Vector2(110, 44));
        zoomOutButton.onClick.AddListener(() => { ZoomIndex = ZoomIndex - 1; ApplyZoom(); });

        rows = new Row[AchievementCatalog.All.Count];
        for (int i = 0; i < rows.Length; i++) rows[i] = BuildRow(content, font, i);
        ApplyZoom();
    }

    static void ApplyZoom()
    {
        float z = ZoomLevels[ZoomIndex];
        if (rows != null)
            foreach (var row in rows)
            {
                row.layout.preferredHeight = BaseRowHeight * z;
                row.name.fontSize = BaseNameSize * z;
                row.description.fontSize = BaseTextSize * z;
                row.reward.fontSize = BaseTextSize * z;
            }
        zoomInButton.interactable = ZoomIndex < ZoomLevels.Length - 1;
        zoomOutButton.interactable = ZoomIndex > 0;
    }

    static Row BuildRow(RectTransform parent, TMP_FontAsset font, int index)
    {
        var rect = UiKit.Child(parent, $"Row{index + 1:000}", typeof(Image), typeof(LayoutElement));
        var row = new Row { background = rect.GetComponent<Image>(), layout = rect.GetComponent<LayoutElement>() };
        row.layout.preferredHeight = BaseRowHeight;

        row.name = UiKit.Label(rect, font, "Name", 24, TextAlignmentOptions.TopLeft, UiKit.Gold);
        UiKit.Anchor(row.name.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        row.name.rectTransform.offsetMin = new Vector2(16, 8);
        row.name.rectTransform.offsetMax = new Vector2(-370, -8);

        row.description = UiKit.Label(rect, font, "Description", 20, TextAlignmentOptions.BottomLeft, UiKit.Muted);
        UiKit.Anchor(row.description.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        row.description.rectTransform.offsetMin = new Vector2(16, 8);
        row.description.rectTransform.offsetMax = new Vector2(-370, -8);

        row.reward = UiKit.Label(rect, font, "Reward", 20, TextAlignmentOptions.MidlineRight, new Color(0.7f, 0.9f, 0.6f));
        UiKit.Anchor(row.reward.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        row.reward.rectTransform.offsetMin = new Vector2(620, 8);
        row.reward.rectTransform.offsetMax = new Vector2(-16, -8);
        return row;
    }

    static void Refresh()
    {
        titleLabel.text = Lang.Get("ach_title");
        closeLabel.text = Lang.Get("close");
        var progress = $"{string.Format(Lang.Get("ach_progress"), Profile.UnlockedCount, AchievementCatalog.All.Count)}   <color=#F2D978>{Legacy.Describe()}</color>";
        hintLabel.text = Profile.RewardsEnabled ? progress : $"{progress}   <color=#E0A050>{Lang.Get("ach_rewards_off_hint")}</color>";
        rewardsLabel.text = Lang.Get(Profile.RewardsEnabled ? "ach_rewards_on" : "ach_rewards_off");

        for (int i = 0; i < rows.Length; i++)
        {
            var achievement = AchievementCatalog.All[i];
            var row = rows[i];
            var done = Profile.IsUnlocked(achievement.Id);
            row.background.color = done ? new Color(0.42f, 0.12f, 0.24f, 0.96f) : new Color(0.16f, 0.07f, 0.24f, 0.9f);
            row.name.text = done ? $"{achievement.Name}  <size=70%><color=#9BE08A>{Lang.Get("ach_done")}</color></size>" : achievement.Name;
            row.name.color = done ? UiSkin.GoldLight : new Color(0.55f, 0.5f, 0.6f);
            row.description.text = achievement.Description;
            row.description.color = done ? new Color(0.85f, 0.82f, 0.92f) : new Color(0.5f, 0.47f, 0.57f);
            row.reward.text = Achievements.RewardText(achievement);
            row.reward.color = done ? new Color(0.7f, 0.9f, 0.6f) : new Color(0.45f, 0.5f, 0.42f);
        }
    }
}
