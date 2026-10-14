using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 교리 스킬 노드 하나. 누르면 레벨을 올린다.
public class DoctrineNodeUI : MonoBehaviour
{
    [SerializeField] TMP_Text nameLabel, levelLabel;
    [SerializeField] Button button;
    [SerializeField] Image ring;

    DoctrineTree tree;
    DoctrineSkill skill;

    void Awake() => button.onClick.AddListener(() => tree?.LevelUp(skill));

    public void Bind(DoctrineTree doctrineTree)
    {
        if (tree != null) tree.Changed -= OnChanged;
        tree = doctrineTree;
        skill = DoctrineSkill.Declaration; // 교리가 늘어나면 노드별 스킬을 지정한다
        tree.Changed += OnChanged;
        Refresh();
    }

    void OnChanged(DoctrineSkill _) => Refresh();

    void Refresh()
    {
        var level = tree.LevelOf(skill);
        var max = tree.IsMax(skill);
        nameLabel.text = skill.Name;
        levelLabel.text = max ? Lang.Get("doctrine_max") : $"{level}/{skill.MaxLevel}\n<size=70%>{Lang.Get("doctrine_learn")}</size>";
        button.interactable = !max;
        if (ring != null) ring.color = max ? new Color(0.95f, 0.85f, 0.55f) : new Color(0.6f, 0.5f, 0.8f);
    }

    void OnDestroy() { if (tree != null) tree.Changed -= OnChanged; }
}
