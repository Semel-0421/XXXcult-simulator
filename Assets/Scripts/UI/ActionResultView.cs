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
        var key = $"{actionKey}_{(success ? "ok" : "fail")}";
        var art = AdultArt.Load(key); // 성인 콘텐츠가 켜져 있고 삽화가 있으면 픽토그램 대신 한 장짜리 삽화
        image.texture = art != null ? art : Resources.Load<Texture2D>($"Pictograms/{key}");
        label.text = Lang.Get(success ? "action_success" : "action_fail");
        label.color = success ? new Color(0.95f, 0.85f, 0.55f) : new Color(0.86f, 0.35f, 0.35f);
        group.alpha = 1;
        AudioManager.PlaySfx(actionKey == "Consecrate" ? "consecrate" : success ? "success" : "fail");
        for (float elapsed = 0; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            int frame = (int)(elapsed * fps) % Frames;
            image.uvRect = art != null ? SquareCrop(art) : new Rect(frame / (float)Frames, 0, 1f / Frames, 1);
            yield return null;
        }
        Hide();
        done?.Invoke();
    }

    // 정사각형 칸에 세로 삽화를 눌러 넣지 않고 가운데보다 조금 위(얼굴 쪽)를 정사각형으로 잘라 보여 준다
    static Rect SquareCrop(Texture art)
    {
        if (art.height <= art.width) { float w = art.height / (float)art.width; return new Rect((1 - w) / 2, 0, w, 1); }
        float h = art.width / (float)art.height;
        return new Rect(0, (1 - h) * 0.7f, 1, h);
    }

    void Hide()
    {
        group.alpha = 0;
        group.blocksRaycasts = false;
    }
}
