using System;
using System.Linq;
using TMPro;
using UnityEngine;

// 질문창 아래 행동 버튼 5개. 교단 창설 전에는 생활 행동, 교리 선포 후에는 교단 행동을 보여준다.
// 버튼이 주 입력이다: 글자 앞에 단축키를 붙인다 (예: "1 포교"). 키를 바꾸거나 언어를 바꾸면 다시 쓴다.
public class ActionBar : MonoBehaviour
{
    public enum Mode { Hidden, Life, Cult }

    static readonly string[] LifeKeys = { "life_Exercise", "life_Meditate", "life_Study", "life_Socialize", "life_Pray" };
    static readonly string[] CultKeys = { "action_Proselytize", "action_Preach", "action_Serve", "action_Desire", "action_Pray" };
    static readonly string[] GodKeys = { "god_act_Wonder", "god_act_Revelation", "god_act_Healing", "god_act_Feast", "god_act_Commune" }; // 준비된 신: 이적·계시·치유·향연·교감

    [SerializeField] CanvasGroup group;
    public event Action<LifeAction> LifeChosen;
    public event Action<CultAction> CultChosen;
    public static event Action Acting; // 행동을 고른 직후, 효과가 들어가기 전 (상단 스탯 강조를 새 턴 기준으로 비운다)

    public Mode Current { get; private set; } = Mode.Hidden;
    public bool CanAct => Current != Mode.Hidden && group.interactable && string.IsNullOrEmpty(GameSession.PendingEventId); // 행동 선택을 기다리는 중
    LocalizedText[] labels;
    TMP_Text[] texts;
    string[] keys;
    DoctrineTree tree;

    void Awake()
    {
        labels = GetComponentsInChildren<LocalizedText>(true);
        texts = labels.Select(l => l.GetComponent<TMP_Text>()).ToArray();
        keys = labels.Select(l => l.Key).ToArray();
        foreach (var l in labels) l.enabled = false; // 단축키를 앞에 붙여야 해서 글자는 RefreshLabels가 직접 쓴다
        Keybinds.Changed += RefreshLabels;
        Lang.Changed += RefreshLabels;
        RefreshLabels();
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
        var consecrate = copy.GetComponentInChildren<LocalizedText>(true);
        consecrate.enabled = true; // 축성은 단축키가 없어 이름만 (언어 변경은 LocalizedText가 맡는다)
        consecrate.SetKey("god_act_Consecrate");
        buttons.Add(copy);
        var width = (((RectTransform)transform).sizeDelta.x - gap * (buttons.Count - 1)) / buttons.Count;
        for (int i = 0; i < buttons.Count; i++)
        {
            buttons[i].sizeDelta = new Vector2(width, buttons[i].sizeDelta.y);
            buttons[i].anchoredPosition = new Vector2(i * (width + gap), buttons[i].anchoredPosition.y);
        }
    }

    void OnDestroy()
    {
        Keybinds.Changed -= RefreshLabels;
        Lang.Changed -= RefreshLabels;
        if (tree != null) tree.Changed -= OnDoctrineChanged;
    }

    // 버튼 글자: "단축키 행동이름" (단축키는 행동 안내 링크와 같은 금색)
    void RefreshLabels()
    {
        for (int i = 0; i < texts.Length && i < keys.Length; i++)
        {
            if (texts[i] == null) continue;
            var label = Lang.Get(keys[i]);
            texts[i].text = i < Keybinds.ActionCount ? $"<color=#F2D978>{Keybinds.Label(Keybinds.Get(i))}</color> {label}" : label;
        }
    }

    void OnDoctrineChanged(DoctrineSkill _)
    {
        if (Current != Mode.Cult && tree.IsMax(DoctrineSkill.Declaration)) Show(Mode.Cult);
    }

    static string[] KeysOf(Mode mode) => mode != Mode.Cult ? LifeKeys : GameSession.GodMode ? GodKeys : CultKeys;

    public void Show(Mode mode)
    {
        keys = KeysOf(mode);
        RefreshLabels();
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
        Acting?.Invoke();
        if (Current == Mode.Life) LifeChosen?.Invoke((LifeAction)index);
        else if (Current == Mode.Cult) CultChosen?.Invoke((CultAction)index);
    }
}
