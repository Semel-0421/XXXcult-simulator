using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 스킬트리 가운데 노드. 처음에는 교리 선포이고, 최종 스킬을 배우면 그 문양이 이 자리를 대체한다.
// 그때부터 이 노드를 누르면 해당 최종 스킬의 최종 업그레이드(2레벨)를 한다.
public class DoctrineNodeUI : MonoBehaviour
{
    [SerializeField] TMP_Text nameLabel, levelLabel;
    [SerializeField] Button button;
    [SerializeField] Image ring;

    DoctrineTree tree;
    Image emblem;
    float baseSize;

    void Awake() => button.onClick.AddListener(OnClick);

    public void Bind(DoctrineTree doctrineTree)
    {
        if (tree != null) tree.Changed -= OnChanged;
        tree = doctrineTree;
        tree.Changed += OnChanged;
        Refresh();
    }

    void OnClick()
    {
        if (tree == null) return;
        if (tree.Emblem != null)
        {
            if (tree.UpgradeFinal(DoctrinePoints.Available())) AudioManager.PlaySfx("doctrine_learn");
        }
        else tree.LevelUp(DoctrineSkill.Declaration);
    }

    void OnChanged(DoctrineSkill _) => Refresh();

    public void Refresh()
    {
        if (tree == null) return;
        var final = tree.Emblem;
        // 영어 이름은 길어서 원 밖으로 넘친다: 원래 크기를 넘지 않는 선에서 자동 축소 (한국어는 그대로)
        if (baseSize == 0) baseSize = nameLabel.fontSize;
        nameLabel.enableAutoSizing = !Lang.IsKorean;
        nameLabel.fontSizeMax = baseSize;
        nameLabel.fontSizeMin = baseSize * 0.4f;
        if (Lang.IsKorean) nameLabel.fontSize = baseSize;
        if (final == null)
        {
            ShowEmblem(null);
            var max = tree.IsMax(DoctrineSkill.Declaration);
            nameLabel.text = DoctrineSkill.Declaration.Name;
            levelLabel.text = max ? Lang.Get("doctrine_max")
                : $"{tree.LevelOf(DoctrineSkill.Declaration)}/{DoctrineSkill.Declaration.MaxLevel}\n<size=70%>{Lang.Get("doctrine_learn")}</size>";
            button.interactable = !max;
            if (ring != null) ring.color = max ? new Color(0.83f, 0.69f, 0.22f) : new Color(0.6f, 0.5f, 0.8f);
            return;
        }

        ShowEmblem(final);
        var upgraded = tree.LevelOf(final) >= final.MaxLevel;
        nameLabel.text = final.Name;
        levelLabel.text = upgraded ? Lang.Get("doctrine_final_done")
            : $"{Lang.Get("doctrine_final_upgrade")}\n<size=70%>{string.Format(Lang.Get("doctrine_cost"), final.CostForLevel(2))}</size>";
        button.interactable = !upgraded && tree.CanUpgradeFinal(DoctrinePoints.Available());
        if (ring != null) ring.color = DoctrineEmblems.ColorOf(final);
    }

    // 최종 스킬 문양을 교리 선포 얼굴 위에 띄운다
    void ShowEmblem(DoctrineSkill final)
    {
        if (emblem == null)
        {
            var rect = UiKit.Child(transform, "Emblem", typeof(Image));
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(150, 150);
            nameLabel.transform.SetAsLastSibling(); // 문양 위에 글자가 보이도록
            levelLabel.transform.SetAsLastSibling();
            emblem = rect.GetComponent<Image>();
            emblem.raycastTarget = false;
            emblem.preserveAspect = true;
        }
        emblem.gameObject.SetActive(final != null);
        if (final == null) return;
        emblem.sprite = DoctrineEmblems.Get(final);
        // 어두운 얼굴 위에 가지 색으로 띄운다. 최종 업그레이드하면 진해진다
        var color = DoctrineEmblems.ColorOf(final);
        color.a = tree.LevelOf(final) >= final.MaxLevel ? 1f : 0.55f;
        emblem.color = color;
    }

    void OnDestroy() { if (tree != null) tree.Changed -= OnChanged; }
}
