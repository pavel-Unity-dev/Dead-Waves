using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DamageFlash : MonoBehaviour
{
    public static DamageFlash Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Image overlay;

    [Header("Tuning")]
    [SerializeField, Range(0f, 1f)] private float maxAlpha = 0.35f;
    [SerializeField] private float fadeInTime = 0.05f;
    [SerializeField] private float fadeOutTime = 0.25f;

    private Coroutine routine;

    private void Awake()
    {
        Instance = this;

        if (overlay == null)
            overlay = GetComponent<Image>();

        SetAlpha(0f);
    }

    public void Flash()
    {
        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        // Быстро появиться
        yield return FadeTo(maxAlpha, fadeInTime);

        // Плавно исчезнуть
        yield return FadeTo(0f, fadeOutTime);

        routine = null;
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        float startAlpha = overlay.color.a;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.0001f, duration);
            float a = Mathf.Lerp(startAlpha, targetAlpha, t);
            SetAlpha(a);
            yield return null;
        }

        SetAlpha(targetAlpha);
    }

    private void SetAlpha(float a)
    {
        Color c = overlay.color;
        c.a = a;
        overlay.color = c;
    }
}
