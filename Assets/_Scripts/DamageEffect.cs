using System;
using UnityEngine;
using System.Collections;

public class DamageEffect : MonoBehaviour
{
    [Header("Effect Settings")]
    public CanvasGroup canvasGroup;
    public float fadeDuration = 0.2f;
    public float showDuration = 0.1f;

    private Coroutine damageCoroutine;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0;
    }

    public void ShowDamageEffect()
    {
        if (damageCoroutine != null)
        {
            StopCoroutine(damageCoroutine);
        }
        damageCoroutine = StartCoroutine(FadeBloodEffect());
    }

    private IEnumerator FadeBloodEffect()
    {
        // Fade in
        yield return StartCoroutine(FadeCanvasGroup(0f, 1f, fadeDuration));

        // Wait while fully visible
        yield return new WaitForSeconds(showDuration);

        // Fade out
        yield return StartCoroutine(FadeCanvasGroup(1f, 0f, fadeDuration));
    }

    private IEnumerator FadeCanvasGroup(float startAlpha, float endAlpha, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            canvasGroup.alpha = alpha;
            elapsed += Time.deltaTime;
            yield return null;
        }
        canvasGroup.alpha = endAlpha;
    }
}

