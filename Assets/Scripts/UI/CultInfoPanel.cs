using TMPro;
using UnityEngine;

// 개인 스탯창 위의 교단 칸. 교단 이름과 교리 선택 버튼을 보여주고, 창설 직후 교리 선택을 강제한다.
public class CultInfoPanel : MonoBehaviour
{
    [SerializeField] CanvasGroup group;
    [SerializeField] TMP_Text nameLabel;
    [SerializeField] RectTransform doctrineButton;
    [SerializeField] ForcedHighlight tutorial;

    void Start()
    {
        group.alpha = 0;
        group.interactable = group.blocksRaycasts = false;
        GameSession.Founded += OnFounded;
        if (GameSession.CultFounded) Show();
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
