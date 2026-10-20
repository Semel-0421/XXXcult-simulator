using System.Linq;
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
    static TMP_Text titleLabel, hintLabel, closeLabel, rewardsLabel, galleryLabel, adultGalleryLabel;
    static Button adultGallery;
    static Button zoomInButton, zoomOutButton;
    static Row[] rows;
    static readonly string[] TabKeys = { "normal", "god", "mirror", "hidden" };
    static readonly Button[] tabButtons = new Button[4];
    static readonly TMP_Text[] tabLabels = new TMP_Text[4];
    static readonly System.Collections.Generic.List<TMP_Text> giftedCells = new System.Collections.Generic.List<TMP_Text>();
    static RectTransform scroll, giftedPanel;
    static TMP_Text emptyLabel, viewLabel;
    static AchievementTabs.Tab tab;
    static bool showGifted;

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

        // 엔딩 도감 열기 (보상 버튼 오른쪽)
        var gallery = UiKit.MakeButton(panel, font, "EndingGalleryButton", "", 190, 44, out galleryLabel);
        gallery.image.color = UiSkin.Crimson;
        UiKit.Anchor((RectTransform)gallery.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(340, -34), new Vector2(190, 44));
        gallery.onClick.AddListener(EndingGallery.Open);

        // 성인 회상 갤러리 (성인 콘텐츠를 켠 경우에만 보인다)
        adultGallery = UiKit.MakeButton(panel, font, "AdultGalleryButton", "", 190, 44, out adultGalleryLabel);
        adultGallery.image.color = UiSkin.Crimson;
        UiKit.Anchor((RectTransform)adultGallery.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(540, -34), new Vector2(190, 44));
        adultGallery.onClick.AddListener(AdultGallery.Open);

        // 글씨 확대/축소 (제목 오른쪽 위, 닫기 버튼 왼쪽)
        zoomInButton = UiKit.MakeButton(panel, font, "ZoomIn", "A+", 110, 44, out _);
        UiKit.Anchor((RectTransform)zoomInButton.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-210, -34), new Vector2(110, 44));
        zoomInButton.onClick.AddListener(() => { ZoomIndex = ZoomIndex + 1; ApplyZoom(); });
        zoomOutButton = UiKit.MakeButton(panel, font, "ZoomOut", "A-", 110, 44, out _);
        UiKit.Anchor((RectTransform)zoomOutButton.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-330, -34), new Vector2(110, 44));
        zoomOutButton.onClick.AddListener(() => { ZoomIndex = ZoomIndex - 1; ApplyZoom(); });

        // 모드 탭 (월계관 아래 한 줄). 숨김 탭은 해금 뒤에만 보인다
        scroll = (RectTransform)panel.Find("Scroll");
        scroll.offsetMax = new Vector2(-18, -214);
        for (int i = 0; i < TabKeys.Length; i++)
        {
            int index = i;
            tabButtons[i] = UiKit.MakeButton(panel, font, "Tab" + TabKeys[i], "", 130, 40, out tabLabels[i]);
            UiKit.Anchor((RectTransform)tabButtons[i].transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(83 + i * 138, -176), new Vector2(130, 40));
            tabButtons[i].onClick.AddListener(() => { tab = (AchievementTabs.Tab)index; showGifted = false; Refresh(); });
        }
        var view = UiKit.MakeButton(panel, font, "GiftedToggle", "", 200, 40, out viewLabel);
        view.image.color = UiSkin.Crimson;
        UiKit.Anchor((RectTransform)view.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-118, -176), new Vector2(200, 40));
        view.onClick.AddListener(() => { showGifted = !showGifted; Refresh(); });

        // 빈 탭 안내문
        emptyLabel = UiKit.Label(panel, font, "Empty", 26, TextAlignmentOptions.Center, UiKit.Muted);
        UiKit.Anchor(emptyLabel.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        emptyLabel.rectTransform.offsetMin = new Vector2(18, 18);
        emptyLabel.rectTransform.offsetMax = new Vector2(-18, -214);

        // 기프티드 도감 자리표시: 칸 격자에 "준비 중"만 적는다
        giftedPanel = UiKit.Child(panel, "GiftedPlaceholder", typeof(GridLayoutGroup));
        UiKit.Anchor(giftedPanel, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        giftedPanel.offsetMin = new Vector2(18, 18);
        giftedPanel.offsetMax = new Vector2(-18, -214);
        var grid = giftedPanel.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(210, 110);
        grid.spacing = new Vector2(12, 12);
        grid.padding = new RectOffset(6, 6, 6, 6);
        for (int i = 0; i < 12; i++)
        {
            var cell = UiKit.Child(giftedPanel, $"Cell{i + 1:00}", typeof(Image));
            cell.GetComponent<Image>().color = new Color(0.16f, 0.07f, 0.24f, 0.9f);
            var text = UiKit.Label(cell, font, "Soon", 22, TextAlignmentOptions.Center, UiKit.Muted);
            UiKit.Anchor(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            giftedCells.Add(text);
        }

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
        galleryLabel.text = Lang.Get("gallery_open");
        adultGalleryLabel.text = Lang.Get("adult_gallery_open");
        adultGallery.gameObject.SetActive(AdultGate.ContentEnabled);

        // 탭·보기 전환
        if (tab == AchievementTabs.Tab.Hidden && !AchievementTabs.HiddenUnlocked) tab = AchievementTabs.Tab.Normal;
        for (int i = 0; i < tabButtons.Length; i++)
        {
            tabLabels[i].text = Lang.Get("ach_tab_" + TabKeys[i]);
            tabButtons[i].image.color = i == (int)tab && !showGifted ? UiSkin.Crimson : new Color(0.22f, 0.1f, 0.3f, 0.95f);
            tabButtons[i].gameObject.SetActive(i != (int)AchievementTabs.Tab.Hidden || AchievementTabs.HiddenUnlocked);
        }
        viewLabel.text = Lang.Get(showGifted ? "ach_view_list" : "ach_view_gifted");
        var list = AchievementTabs.Of(tab).ToList();
        scroll.gameObject.SetActive(!showGifted && list.Count > 0);
        emptyLabel.gameObject.SetActive(!showGifted && list.Count == 0);
        emptyLabel.text = Lang.Get("ach_tab_empty");
        giftedPanel.gameObject.SetActive(showGifted);
        foreach (var cell in giftedCells) cell.text = Lang.Get("ach_soon");

        for (int i = 0; i < rows.Length; i++)
        {
            // shortcut: 행은 일반 탭 개수만큼만 만들어 둠, 다른 탭에 업적이 생기면 행 풀을 늘릴 것
            var row = rows[i];
            row.background.gameObject.SetActive(i < list.Count);
            if (i >= list.Count) continue;
            var achievement = list[i];
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
