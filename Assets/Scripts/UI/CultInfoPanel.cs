using TMPro;
using UnityEngine;

// 개인 스탯창 위의 교단 칸. 교단 이름과 교리 선택 버튼을 보여주고, 창설 직후 교리 선택을 강제한다.
public class CultInfoPanel : MonoBehaviour
{
    [SerializeField] CanvasGroup group;
    [SerializeField] TMP_Text nameLabel;
    [SerializeField] RectTransform doctrineButton;
    [SerializeField] ForcedHighlight tutorial;

    TMP_Text pointsLabel;
    int shownPoints = -1;

    void Start()
    {
        group.alpha = 0;
        group.interactable = group.blocksRaycasts = false;
        GameSession.Founded += OnFounded;
        BuildPointsLabel();
        if (GameSession.CultFounded) Show();
    }

    // 교리 선택 버튼 바로 아래, 버튼 오른쪽 끝에 맞춰 보유 교리 포인트를 적는다
    void BuildPointsLabel()
    {
        var button = doctrineButton;
        var parent = (RectTransform)button.parent;
        pointsLabel = UiKit.Label(parent, UiKit.FindFont(), "DoctrinePoints", 22, TextAlignmentOptions.TopRight, UiKit.Gold);
        var rect = pointsLabel.rectTransform;
        rect.anchorMin = button.anchorMin;
        rect.anchorMax = button.anchorMax;
        rect.pivot = new Vector2(1, 1);
        var rightEdge = button.anchoredPosition.x + button.sizeDelta.x * (1 - button.pivot.x);
        var bottomEdge = button.anchoredPosition.y - button.sizeDelta.y * button.pivot.y;
        rect.anchoredPosition = new Vector2(rightEdge, bottomEdge - 4);
        rect.sizeDelta = new Vector2(300, 30);
    }

    void Update()
    {
        if (pointsLabel == null) return;
        var points = DoctrinePoints.Available();
        if (points == shownPoints) return;
        shownPoints = points;
        pointsLabel.text = string.Format(Lang.Get("doctrine_points"), points);
    }

    void OnDestroy() => GameSession.Founded -= OnFounded;

    void OnFounded()
    {
        Show();
        if (!GameSession.Cult.Doctrines.IsMax(DoctrineSkill.Declaration))
            tutorial.Begin(doctrineButton, Lang.Get("doctrine_tutorial"));
    }

    void Show()
    {
        nameLabel.text = GameSession.Cult.Name;
        StartCoroutine(CanvasFade.To(group, 1, 0.5f));
        group.interactable = group.blocksRaycasts = true;
    }
}
