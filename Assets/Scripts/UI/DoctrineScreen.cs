using UnityEngine;
using UnityEngine.UI;

// 은하수 배경 위에 교리 스킬창을 띄운다. 교리 선포를 배우기 전에는 닫을 수 없다.
public class DoctrineScreen : MonoBehaviour
{
    [SerializeField] GameObject window;
    [SerializeField] Button closeButton;
    [SerializeField] DoctrineNodeUI[] nodes;
    [SerializeField] ForcedHighlight tutorial;

    DoctrineTree Tree => GameSession.Cult.Doctrines;

    void Start() => window.SetActive(false);

    public void Open()
    {
        tutorial.End();
        window.SetActive(true);
        foreach (var node in nodes) node.Bind(Tree);
        Tree.Changed += OnChanged;
        RefreshClose();
    }

    public void Close()
    {
        if (!closeButton.interactable) return;
        Tree.Changed -= OnChanged;
        window.SetActive(false);
    }

    void OnChanged(DoctrineSkill _) => RefreshClose();

    void RefreshClose() => closeButton.interactable = Tree.IsMax(DoctrineSkill.Declaration);
}
