using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class LocalizedText : MonoBehaviour
{
    [SerializeField] string key;
    TMP_Text label;
    public string Key => key;

    void Awake() => label = GetComponent<TMP_Text>();
    void OnEnable() { Lang.Changed += Refresh; Refresh(); }
    void OnDisable() => Lang.Changed -= Refresh;
    void Refresh() => label.text = Lang.Get(key);

    public void SetKey(string newKey) { key = newKey; Refresh(); }
}
