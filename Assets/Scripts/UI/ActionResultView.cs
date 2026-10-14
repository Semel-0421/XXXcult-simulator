using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 행동 결과(성공/실패) 픽토그램 애니메이션을 잠시 보여 준다. 시트는 Resources/Pictograms/{키}_ok|fail (8프레임 가로).
public class ActionResultView : MonoBehaviour
{
    const int Frames = 8;

    [SerializeField] CanvasGroup group;
    [SerializeField] RawImage image;
    [SerializeField] TMP_Text label;
    [SerializeField] float fps = 10, duration = 1.6f;

    void Awake() => Hide();

    public void Play(string actionKey, bool success, Action done) => StartCoroutine(Run(actionKey, success, done));

    IEnumerator Run(string actionKey, bool success, Action done)
    {
        image.texture = Resources.Load<Texture2D>($"Pictograms/{actionKey}_{(success ? "ok" : "fail")}");
        label.text = Lang.Get(success ? "action_success" : "action_fail");
        label.color = success ? new Color(0.95f, 0.85f, 0.55f) : new Color(0.86f, 0.35f, 0.35f);
        group.alpha = 1;
        for (float elapsed = 0; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            int frame = (int)(elapsed * fps) % Frames;
            image.uvRect = new Rect(frame / (float)Frames, 0, 1f / Frames, 1);
            yield return null;
        }
        Hide();
        done?.Invoke();
    }

    void Hide()
    {
        group.alpha = 0;
        group.blocksRaycasts = false;
    }
}
