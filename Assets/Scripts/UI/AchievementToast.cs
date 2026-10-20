using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 업적을 달성하는 순간 화면 위쪽에 잠깐 알림을 띄운다. 게임 시작 시 스스로 만들어져 장면이 바뀌어도 남는다.
public class AchievementToast : MonoBehaviour
{
    const float FadeTime = 0.35f, HoldTime = 3f;

    readonly Queue<Achievement> pending = new();
    CanvasGroup group;
    TMP_Text titleLabel, nameLabel, rewardLabel;
    bool showing;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Create()
    {
        var go = new GameObject("AchievementToast");
        DontDestroyOnLoad(go);
        go.AddComponent<AchievementToast>();
    }

    void OnEnable() => Achievements.Unlocked += OnUnlocked;
    void OnDisable() => Achievements.Unlocked -= OnUnlocked;

    void OnUnlocked(Achievement achievement)
    {
        pending.Enqueue(achievement);
        AudioManager.PlaySfx("achievement");
        if (!showing) StartCoroutine(ShowAll());
    }

    IEnumerator ShowAll()
    {
        showing = true;
        while (pending.Count > 0)
        {
            Build();
            var achievement = pending.Dequeue();
            titleLabel.text = Lang.Get("ach_toast");
            nameLabel.text = achievement.Name;
            rewardLabel.text = Profile.RewardsEnabled ? Achievements.RewardText(achievement) : "";
            yield return Fade(0, 1);
            yield return new WaitForSecondsRealtime(HoldTime);
            yield return Fade(1, 0);
        }
        showing = false;
    }

    IEnumerator Fade(float from, float to)
    {
        for (float t = 0; t < FadeTime; t += Time.unscaledDeltaTime)
        {
            group.alpha = Mathf.Lerp(from, to, t / FadeTime);
            yield return null;
        }
        group.alpha = to;
    }

    // 알림 UI는 처음 필요할 때 만든다 (글꼴을 장면에서 빌려 와야 하므로)
    void Build()
    {
        if (group != null) return;
        var font = UiKit.FindFont();
        var canvas = UiKit.CreateOverlayCanvas("AchievementToastCanvas", 500);
        canvas.SetParent(transform, false);

        var panel = UiKit.Child(canvas, "Toast", typeof(Image), typeof(Outline), typeof(CanvasGroup));
        UiKit.Anchor(panel, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -70), new Vector2(560, 96));
        panel.GetComponent<Image>().color = new Color(0.08f, 0.06f, 0.13f, 0.96f);
        panel.GetComponent<Image>().raycastTarget = false;
        panel.GetComponent<Outline>().effectColor = new Color(0.83f, 0.69f, 0.22f, 0.7f);
        group = panel.GetComponent<CanvasGroup>();
        group.alpha = 0;
        group.blocksRaycasts = false;

        titleLabel = UiKit.Label(panel, font, "Title", 18, TextAlignmentOptions.TopLeft, new Color(0.6f, 0.88f, 0.52f));
        UiKit.Anchor(titleLabel.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        titleLabel.rectTransform.offsetMin = new Vector2(18, 8);
        titleLabel.rectTransform.offsetMax = new Vector2(-18, -8);

        nameLabel = UiKit.Label(panel, font, "Name", 28, TextAlignmentOptions.MidlineLeft, UiKit.Gold);
        UiKit.Anchor(nameLabel.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        nameLabel.rectTransform.offsetMin = new Vector2(18, 8);
        nameLabel.rectTransform.offsetMax = new Vector2(-18, -8);

        rewardLabel = UiKit.Label(panel, font, "Reward", 18, TextAlignmentOptions.BottomLeft, UiKit.Muted);
        UiKit.Anchor(rewardLabel.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        rewardLabel.rectTransform.offsetMin = new Vector2(18, 8);
        rewardLabel.rectTransform.offsetMax = new Vector2(-18, -8);
    }
}
