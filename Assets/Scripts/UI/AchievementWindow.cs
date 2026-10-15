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
        public TMP_Text name, description, reward;
    }

    static GameObject root;
    static TMP_Text titleLabel, hintLabel, closeLabel, rewardsLabel;
    static Row[] rows;

    public static void Open()
    {
        if (root == null) Build();
        Refresh();
        if (!root.activeSelf) AudioManager.MenuOpened();
        root.SetActive(true);
    }

    public static void Close()
    {
        if (root == null || !root.activeSelf) return;
        AudioManager.MenuClosed();
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

        // 보상 켜기/끄기 버튼 (제목 왼쪽)
        var panel = (RectTransform)root.transform.Find("Panel");
        var toggle = UiKit.MakeButton(panel, font, "RewardsToggle", "", 200, 44, out rewardsLabel);
        UiKit.Anchor((RectTransform)toggle.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(120, -34), new Vector2(200, 44));
        toggle.onClick.AddListener(() =>
        {
            Profile.RewardsEnabled = !Profile.RewardsEnabled;
            Refresh();
        });

        rows = new Row[AchievementCatalog.All.Count];
        for (int i = 0; i < rows.Length; i++) rows[i] = BuildRow(content, font, i);
    }

    static Row BuildRow(RectTransform parent, TMP_FontAsset font, int index)
    {
        var rect = UiKit.Child(parent, $"Row{index + 1:000}", typeof(Image), typeof(LayoutElement));
        rect.GetComponent<LayoutElement>().preferredHeight = 76;
        var row = new Row { background = rect.GetComponent<Image>() };

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
            row.background.color = done ? new Color(0.20f, 0.17f, 0.30f, 1f) : new Color(0.12f, 0.10f, 0.17f, 1f);
            row.name.text = done ? $"{achievement.Name}  <size=70%><color=#9BE08A>{Lang.Get("ach_done")}</color></size>" : achievement.Name;
            row.name.color = done ? UiKit.Gold : new Color(0.55f, 0.52f, 0.62f);
            row.description.text = achievement.Description;
            row.description.color = done ? new Color(0.85f, 0.82f, 0.92f) : new Color(0.5f, 0.47f, 0.57f);
            row.reward.text = Achievements.RewardText(achievement);
            row.reward.color = done ? new Color(0.7f, 0.9f, 0.6f) : new Color(0.45f, 0.5f, 0.42f);
        }
    }
}
