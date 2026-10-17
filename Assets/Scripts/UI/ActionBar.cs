using System;
using System.Linq;
using UnityEngine;

// 질문창 아래 행동 버튼 5개. 교단 창설 전에는 생활 행동, 교리 선포 후에는 교단 행동을 보여준다.
public class ActionBar : MonoBehaviour
{
    public enum Mode { Hidden, Life, Cult }

    static readonly string[] LifeKeys = { "life_Exercise", "life_Meditate", "life_Study", "life_Socialize", "life_Pray" };
    static readonly string[] CultKeys = { "action_Proselytize", "action_Preach", "action_Serve", "action_Desire", "action_Pray" };
    static readonly string[] GodKeys = { "god_act_Wonder", "god_act_Revelation", "god_act_Healing", "god_act_Feast", "god_act_Commune" }; // 준비된 신: 이적·계시·치유·향연·교감

    [SerializeField] CanvasGroup group;
    public event Action<LifeAction> LifeChosen;
    public event Action<CultAction> CultChosen;

    public Mode Current { get; private set; } = Mode.Hidden;
    public bool CanAct => Current != Mode.Hidden && group.interactable && string.IsNullOrEmpty(GameSession.PendingEventId); // 행동 선택을 기다리는 중
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
        if (GameSession.GodMode) AddConsecrateButton();
        if (tree.IsMax(DoctrineSkill.Declaration)) Show(Mode.Cult);
    }

    // 준비된 신: 행동 버튼 5개를 좁혀 여섯 번째 자리에 축성(봉헌 건축) 버튼을 붙인다. 누르면 축성 창이 열린다.
    void AddConsecrateButton()
    {
        const float gap = 10;
        var buttons = GetComponentsInChildren<UnityEngine.UI.Button>(true)
            .Select(b => (RectTransform)b.transform).OrderBy(r => r.anchoredPosition.x).ToList();
        var copy = (RectTransform)Instantiate(buttons[^1].gameObject, buttons[^1].parent).transform;
        copy.name = "Action_Consecrate";
        var button = copy.GetComponent<UnityEngine.UI.Button>();
        button.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
        button.onClick.AddListener(() => { if (CanAct) ConsecrateWindow.Open(); });
        copy.GetComponentInChildren<LocalizedText>(true).SetKey("god_act_Consecrate");
        buttons.Add(copy);
        var width = (((RectTransform)transform).sizeDelta.x - gap * (buttons.Count - 1)) / buttons.Count;
        for (int i = 0; i < buttons.Count; i++)
        {
            buttons[i].sizeDelta = new Vector2(width, buttons[i].sizeDelta.y);
            buttons[i].anchoredPosition = new Vector2(i * (width + gap), buttons[i].anchoredPosition.y);
        }
    }

    void OnDestroy() { if (tree != null) tree.Changed -= OnDoctrineChanged; }

    void OnDoctrineChanged(DoctrineSkill _)
    {
        if (Current != Mode.Cult && tree.IsMax(DoctrineSkill.Declaration)) Show(Mode.Cult);
    }

    static string[] KeysOf(Mode mode) => mode != Mode.Cult ? LifeKeys : GameSession.GodMode ? GodKeys : CultKeys;

    // 행동 안내 아래 붙는 단축키 안내 (예: "[1] 운동   [2] 명상 ...")
    public static string Legend(Mode mode) =>
        Keybinds.Legend(KeysOf(mode).Select(Lang.Get).ToList()); // 눌러서 고를 수 있는 글자라 작게 줄이지 않는다

    public void Show(Mode mode)
    {
        var keys = KeysOf(mode);
        for (int i = 0; i < labels.Length; i++) labels[i].SetKey(keys[i]);
        var wasHidden = Current == Mode.Hidden;
        Current = mode;
        SetInteractable(mode != Mode.Hidden);
        if (mode == Mode.Hidden) { StopAllCoroutines(); group.alpha = 0; }
        else if (wasHidden) StartCoroutine(CanvasFade.To(group, 1, 0.5f));
    }

    public void SetInteractable(bool active) => group.interactable = group.blocksRaycasts = active && Current != Mode.Hidden;

    // 단축키(기본 1~5)로 행동을 고른다. 행동을 고를 수 있고 메뉴가 닫혀 있을 때만 반응한다.
    void Update()
    {
        if (!CanAct || !Keybinds.Listening) return;
        for (int i = 0; i < Keybinds.ActionCount; i++)
        {
            if (!Keybinds.WasPressed(Keybinds.Get(i))) continue;
            Keybinds.Consume();
            Perform(i);
            return;
        }
    }

    // 버튼 OnClick에서 호출 (0~4)
    public void Perform(int index)
    {
        if (!CanAct) return; // 이벤트 진행 중 등 잠긴 상태에서는 무시
        AudioManager.PlaySfx("click");
        if (Current == Mode.Life) LifeChosen?.Invoke((LifeAction)index);
        else if (Current == Mode.Cult) CultChosen?.Invoke((CultAction)index);
    }
}
