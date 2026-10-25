using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        Lang.Changed += OnLangChanged;
        BuildPointsLabel();
        if (GameSession.EyeMode) UseTree("eye_tree_title", AeonTreeWindow.Open);
        else if (GameSession.GodMode) UseTree("god_tree_title", () => GodTreeWindow.Open(GameSession.God));
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
        rect.sizeDelta = new Vector2(300, 56);
    }

    void Update()
    {
        if (pointsLabel == null) return;
        if (GameSession.GodMode) { ShowGodPoints(); return; }
        var points = DoctrinePoints.Available();
        if (points == shownPoints) return;
        shownPoints = points;
        pointsLabel.text = string.Format(Lang.Get("doctrine_points"), points);
    }

    // 권능 포인트와 압박 단계 (둘을 한 값으로 묶어 바뀔 때만 다시 쓴다)
    void ShowGodPoints()
    {
        var god = GameSession.God;
        var heaven = god.Pressure.Stage(Pressure.Heaven, GameSession.Cult);
        var earth = god.Pressure.Stage(Pressure.Earth, GameSession.Cult);
        var shows = GiftedV2GodHooks.ShowsEndingProgress;
        var cores = shows ? GodSkillCatalog.Cores.Count(god.Tree.IsLearned) : 0;
        var key = ((god.Available * 100 + heaven * 10 + earth) * 16 + cores * 2 + (god.Tree.Ascended ? 1 : 0)) * 2 + (shows ? 1 : 0);
        if (key == shownPoints) return;
        shownPoints = key;
        var pressure = GameSession.EyeMode ? "" : string.Format(Lang.Get("god_pressure_now"), heaven, earth); // 눈동자 모드에는 압박이 없다
        pointsLabel.text = $"{string.Format(Lang.Get(GameSession.EyeMode ? "eye_points" : "god_points"), god.Available)}\n<size=80%>{pressure}{(shows ? "\n" + EndingProgress(god.Tree, cores) : "")}</size>";
    }

    // 기프티드 v2g046: 승천 엔딩 진행도 (핵 2개를 먼저 익힌 조합이 엔딩을 정하고, 중심 노드가 승천이다)
    static string EndingProgress(GodTree tree, int cores)
    {
        var ko = Lang.IsKorean;
        if (tree.Ascended) return ko ? "승천 완료" : "Ascended";
        var next = tree.AscensionOpen ? (ko ? "승천 노드 개방" : "Ascension open") : $"{(ko ? "핵" : "Cores")} {Mathf.Min(cores, 2)}/2";
        return $"{(ko ? "승천 진행" : "Ascension")}: {next}";
    }

    void OnDestroy() { GameSession.Founded -= OnFounded; Lang.Changed -= OnLangChanged; }

    void OnLangChanged() => shownPoints = -1; // 다음 Update에서 새 언어로 다시 쓴다

    void OnFounded()
    {
        Show();
        if (!GameSession.EyeMode && !GameSession.Cult.Doctrines.IsMax(DoctrineSkill.Declaration))
            tutorial.Begin(doctrineButton, Lang.Get(GameSession.GodMode ? "god_tutorial" : "doctrine_tutorial"));
    }

    // 준비된 신 모드: 교리 선택 버튼이 장면에 연결된 교리 창 대신 권능 트리를 연다 (황금의 눈동자는 아이온 트리)
    void UseTree(string titleKey, System.Action open)
    {
        var button = doctrineButton.GetComponent<Button>();
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(() => { tutorial.End(); open(); });
        var label = doctrineButton.GetComponentInChildren<LocalizedText>(true);
        if (label != null) label.SetKey(titleKey);
        else doctrineButton.GetComponentInChildren<TMP_Text>(true).text = Lang.Get(titleKey);
    }

    void Show()
    {
        nameLabel.text = GameSession.Cult.Name;
        StartCoroutine(CanvasFade.To(group, 1, 0.5f));
        group.interactable = group.blocksRaycasts = true;
    }
}
