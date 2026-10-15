using UnityEngine;
using UnityEngine.UI;

// 은하수 배경 위에 교리 스킬창을 띄운다. 교리 선포를 배우기 전에는 닫을 수 없다.
public class DoctrineScreen : MonoBehaviour
{
    [SerializeField] GameObject window;
    [SerializeField] Button closeButton;
    [SerializeField] DoctrineNodeUI[] nodes;
    [SerializeField] ForcedHighlight tutorial;

    DoctrineTreeView treeView;

    DoctrineTree Tree => GameSession.Cult.Doctrines;

    void Start() => window.SetActive(false);

    public void Open()
    {
        tutorial.End();
        if (!window.activeSelf) Menus.Opened();
        window.SetActive(true);
        foreach (var node in nodes) node.Bind(Tree);
        if (treeView == null)
        {
            var oldWeb = window.transform.Find("Web"); // 예전 자리표시용 거미줄은 스킬트리로 대체
            if (oldWeb != null) oldWeb.gameObject.SetActive(false);
            treeView = DoctrineTreeView.Build(window.transform);
        }
        treeView.Bind(Tree);
        Tree.Changed -= OnChanged;
        Tree.Changed += OnChanged;
        RefreshClose();
    }

    public void Close()
    {
        if (!closeButton.interactable) return;
        Tree.Changed -= OnChanged;
        if (window.activeSelf) Menus.Closed();
        window.SetActive(false);
    }

    void OnDestroy() => Tree.Changed -= OnChanged;

    void OnChanged(DoctrineSkill _) => RefreshClose();

    void RefreshClose() => closeButton.interactable = Tree.IsMax(DoctrineSkill.Declaration);
}
