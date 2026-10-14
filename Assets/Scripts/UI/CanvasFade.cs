using System.Collections;
using UnityEngine;

public static class CanvasFade
{
    public static IEnumerator To(CanvasGroup group, float targetAlpha, float seconds)
    {
        float startAlpha = group.alpha;
        for (float elapsed = 0; elapsed < seconds; elapsed += Time.unscaledDeltaTime)
        {
            group.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / seconds);
            yield return null;
        }
        group.alpha = targetAlpha;
    }
}
