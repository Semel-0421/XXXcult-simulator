using System;
using UnityEngine;

public enum CultAction { Proselytize, Preach, Serve, Desire, Pray }

// 교리 선포를 배우면 나타나는 교단 행동 버튼 줄. 각 행동의 효과는 추후 Performed에 연결한다.
public class CultActionBar : MonoBehaviour
{
    [SerializeField] CanvasGroup group;
    public event Action<CultAction> Performed;

    DoctrineTree tree;

    void Start()
    {
        tree = GameSession.Cult.Doctrines;
        tree.Changed += OnDoctrineChanged;
        var learned = tree.IsMax(DoctrineSkill.Declaration);
        group.alpha = learned ? 1 : 0;
        group.interactable = group.blocksRaycasts = learned;
    }

    void OnDestroy() { if (tree != null) tree.Changed -= OnDoctrineChanged; }

    void OnDoctrineChanged(DoctrineSkill _)
    {
        if (group.interactable || !tree.IsMax(DoctrineSkill.Declaration)) return;
        group.interactable = group.blocksRaycasts = true;
        StartCoroutine(CanvasFade.To(group, 1, 0.5f));
    }

    // 버튼 OnClick에서 호출 (0=포교, 1=설교, 2=봉사, 3=욕구, 4=기도)
    public void Perform(int action) => Performed?.Invoke((CultAction)action);
}
