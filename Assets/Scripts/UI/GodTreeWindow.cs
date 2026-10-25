using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 준비된 신 모드의 권능 트리 창. 왼쪽은 방사형 트리(GodTreeView), 오른쪽은 고른 노드의 정보와 익히기 버튼.
// 장면에 붙일 것 없이 GodTreeWindow.Open(GameSession.God)으로 코드에서 만들어 연다.
public static class GodTreeWindow
{
    static GameObject root;
    static GodTreeView view;
    static GodState state;
    static TMP_Text titleLabel, hintLabel, pointsLabel, stageLabel, closeLabel;
    static TMP_Text nameLabel, infoLabel, learnLabel;
    static Button learnButton;

    public static bool IsOpen => root != null && root.activeSelf;

    public static void Open(GodState godState)
    {
        if (root == null) Build();
        state = godState;
        if (!root.activeSelf) Menus.Opened();
        root.SetActive(true);
        view.Bind(state);
        view.PlayIntro(!state.IntroShown);
        state.IntroShown = true;
        Refresh();
    }

    public static void Close()
    {
        if (!IsOpen) return;
        Menus.Closed();
        root.SetActive(false);
    }

    static void Build()
    {
        var font = UiKit.FindFont();
        var canvas = UiKit.CreateOverlayCanvas("GodTreeCanvas", 305);
        var rootRect = UiKit.Child(canvas, "GodTreeWindow", typeof(Image));
        UiKit.Stretch(rootRect);
        rootRect.GetComponent<Image>().color = new Color(0, 0, 0, 0.8f);
        root = rootRect.gameObject;

        var panel = UiKit.Child(rootRect, "Panel", typeof(Image), typeof(Outline));
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = new Vector2(1240, 700);
        UiSkin.ApplyBookFrame(panel.GetComponent<Image>(), "GodTreeBook", UiPalette.BookAltarTop, UiPalette.BookAltarBottom);
        panel.GetComponent<Outline>().effectColor = UiPalette.GoldFaint;

        titleLabel = UiKit.Label(panel, font, "Title", 32, TextAlignmentOptions.Center, UiKit.Gold);
        UiKit.Anchor(titleLabel.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -32), new Vector2(0, 44));
        hintLabel = UiKit.Label(panel, font, "Hint", 18, TextAlignmentOptions.Center, UiKit.Muted);
        UiKit.Anchor(hintLabel.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -64), new Vector2(0, 26));
        pointsLabel = UiKit.Label(panel, font, "Points", 22, TextAlignmentOptions.Left, UiKit.Gold);
        UiKit.Anchor(pointsLabel.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(150, -30), new Vector2(260, 30));
        stageLabel = UiKit.Label(panel, font, "Stage", 18, TextAlignmentOptions.Left, new Color(1f, 0.95f, 0.8f));
        UiKit.Anchor(stageLabel.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(150, -58), new Vector2(260, 26));

        var close = UiKit.MakeButton(panel, font, "Close", "", 120, 44, out closeLabel);
        close.image.color = UiSkin.Crimson;
        UiKit.Anchor((RectTransform)close.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-80, -36), new Vector2(120, 44));
        close.onClick.AddListener(Close);
        EscClose.Attach(root, close);

        // 트리 영역 (확대 연출이 밖으로 넘치지 않게 잘라 낸다)
        var area = UiKit.Child(panel, "TreeArea", typeof(RectMask2D), typeof(Image));
        area.anchorMin = new Vector2(0, 0);
        area.anchorMax = new Vector2(1, 1);
        area.offsetMin = new Vector2(20, 20);
        area.offsetMax = new Vector2(-400, -86);
        area.GetComponent<Image>().color = new Color(0.04f, 0.02f, 0.07f, 0.85f);
        Canvas.ForceUpdateCanvases(); // 가지 이름 배치에 영역 크기가 필요하다
        view = GodTreeView.Create(area, font);
        view.SelectionChanged += _ => Refresh();

        BuildInfoPanel(panel, font);
        root.SetActive(false);
    }

    static void BuildInfoPanel(RectTransform panel, TMP_FontAsset font)
    {
        var info = UiKit.Child(panel, "Info", typeof(Image));
        info.anchorMin = new Vector2(1, 0);
        info.anchorMax = new Vector2(1, 1);
        info.pivot = new Vector2(1, 0.5f);
        info.offsetMin = new Vector2(-380, 20);
        info.offsetMax = new Vector2(-20, -86);
        info.GetComponent<Image>().color = new Color(0.06f, 0.04f, 0.10f, 1f);

        nameLabel = UiKit.Label(info, font, "Name", 28, TextAlignmentOptions.TopLeft, Color.white);
        UiKit.Anchor(nameLabel.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -40), new Vector2(-36, 60));

        infoLabel = UiKit.Label(info, font, "Body", 19, TextAlignmentOptions.TopLeft, new Color(0.88f, 0.85f, 0.95f));
        infoLabel.richText = true;
        infoLabel.rectTransform.anchorMin = Vector2.zero;
        infoLabel.rectTransform.anchorMax = Vector2.one;
        infoLabel.rectTransform.offsetMin = new Vector2(18, 90);
        infoLabel.rectTransform.offsetMax = new Vector2(-18, -76);

        learnButton = UiKit.MakeButton(info, font, "Learn", "", 330, 52, out learnLabel);
        UiKit.Anchor((RectTransform)learnButton.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 46), new Vector2(330, 52));
        learnLabel.enableAutoSizing = true;
        learnLabel.fontSizeMin = 14;
        learnLabel.fontSizeMax = 22;
        learnButton.onClick.AddListener(() => view.TryLearn());
    }

    static void Refresh()
    {
        if (state == null) return;
        var tree = state.Tree;
        titleLabel.text = Lang.Get("god_tree_title");
        hintLabel.text = Lang.Get(tree.HasNature ? "god_tree_hint" : "god_nature_hint");
        pointsLabel.text = string.Format(Lang.Get("god_points"), state.Available);
        stageLabel.text = string.Format(Lang.Get("god_stage_now"), GodSkillInfo.StageName(tree.ReachedStage))
            + (tree.BroadRings > 0 ? $"  <color=#9BE08A>{string.Format(Lang.Get("god_broad"), tree.BroadRings)}</color>" : "");
        closeLabel.text = Lang.Get("close");

        var skill = view.Selected;
        if (skill == null)
        {
            nameLabel.text = "";
            infoLabel.text = Lang.Get(tree.HasNature ? "god_tree_empty" : "god_nature_hint");
            learnButton.gameObject.SetActive(false);
            return;
        }

        var color = ColorUtility.ToHtmlStringRGB(GodTreeView.BranchColor(skill.Branch));
        nameLabel.text = $"<color=#{color}>{skill.Name}</color>";
        var lines = new System.Collections.Generic.List<string>
        {
            $"<color=#9A8FB0>{Lang.Get($"god_branch_{skill.Branch}")} · {GodSkillInfo.StageName(skill.Stage)} · {Lang.Get($"god_kind_{skill.Kind}")} · {string.Format(Lang.Get("god_cost"), skill.Cost)}</color>",
            skill.Description,
            "",
        };
        lines.AddRange(GodSkillInfo.EffectLines(skill).Select(line => $"· {line}"));
        if (tree.IsNature(skill)) lines.Insert(1, $"<color=#F2D978>{Lang.Get("god_nature_tag")}</color>");
        infoLabel.text = string.Join("\n", lines);

        learnButton.gameObject.SetActive(true);
        learnButton.interactable = view.CanLearnNow(skill);
        learnLabel.text = tree.IsLearned(skill) ? Lang.Get(skill.IsCenter ? "god_ascended" : "god_learned")
            : !tree.HasNature ? Lang.Get(skill.IsOuter ? "god_nature_pick" : "god_need_nature")
            : !tree.PrerequisitesMet(skill) ? Lang.Get(skill.IsCenter ? "god_need_cores" : "god_need_children")
            : state.Available < skill.Cost ? Lang.Get("god_need_points")
            : $"{Lang.Get("god_learn")} ({string.Format(Lang.Get("god_cost"), skill.Cost)})";
    }
}
