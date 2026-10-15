using System;
using UnityEngine;

// 질문창 아래 행동 버튼 5개. 교단 창설 전에는 생활 행동, 교리 선포 후에는 교단 행동을 보여준다.
public class ActionBar : MonoBehaviour
{
    public enum Mode { Hidden, Life, Cult }

    static readonly string[] LifeKeys = { "life_Exercise", "life_Meditate", "life_Study", "life_Socialize", "life_Pray" };
    static readonly string[] CultKeys = { "action_Proselytize", "action_Preach", "action_Serve", "action_Desire", "action_Pray" };

    [SerializeField] CanvasGroup group;
    public event Action<LifeAction> LifeChosen;
    public event Action<CultAction> CultChosen;

    public Mode Current { get; private set; } = Mode.Hidden;
    public bool CanAct => Current != Mode.Hidden && group.interactable; // 행동 선택을 기다리는 중 (저장 가능한 시점)
    LocalizedText[] labels;
    DoctrineTree tree;

    void Awake()
    {
        labels = GetComponentsInChildren<LocalizedText>(true);
        group.alpha = 0;
        group.interactable = group.blocksRaycasts = false;
    }

    void Start()
    {
        tree = GameSession.Cult.Doctrines;
        tree.Changed += OnDoctrineChanged;
        if (tree.IsMax(DoctrineSkill.Declaration)) Show(Mode.Cult);
    }

    void OnDestroy() { if (tree != null) tree.Changed -= OnDoctrineChanged; }

    void OnDoctrineChanged(DoctrineSkill _)
    {
        if (Current != Mode.Cult && tree.IsMax(DoctrineSkill.Declaration)) Show(Mode.Cult);
    }

    public void Show(Mode mode)
    {
        var keys = mode == Mode.Cult ? CultKeys : LifeKeys;
        for (int i = 0; i < labels.Length; i++) labels[i].SetKey(keys[i]);
        var wasHidden = Current == Mode.Hidden;
        Current = mode;
        SetInteractable(mode != Mode.Hidden);
        if (mode == Mode.Hidden) { StopAllCoroutines(); group.alpha = 0; }
        else if (wasHidden) StartCoroutine(CanvasFade.To(group, 1, 0.5f));
    }

    public void SetInteractable(bool active) => group.interactable = group.blocksRaycasts = active && Current != Mode.Hidden;

    // 버튼 OnClick에서 호출 (0~4)
    public void Perform(int index)
    {
        if (!group.interactable) return; // 이벤트 진행 중 등 잠긴 상태에서는 무시
        AudioManager.PlaySfx("click");
        if (Current == Mode.Life) LifeChosen?.Invoke((LifeAction)index);
        else if (Current == Mode.Cult) CultChosen?.Invoke((CultAction)index);
    }
}
