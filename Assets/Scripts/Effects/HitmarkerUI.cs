using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HitmarkerUI : MonoBehaviour
{
    public static HitmarkerUI Instance { get; private set; }

    [Header("Refs")]
    [SerializeField] private Image image;
    [SerializeField] private RectTransform rect;

    [Header("Timing")]
    [SerializeField] private float showTime = 0.06f;      // сколько держится ярким
    [SerializeField] private float fadeOutTime = 0.12f;   // как быстро исчезает

    [Header("Pop Scale")]
    [SerializeField] private float popScale = 1.25f;      // насколько увеличится
    [SerializeField] private float popInTime = 0.03f;     // скорость "попа"

    [Header("Colors")]
    [SerializeField] private Color hitColor = Color.white;
    [SerializeField] private Color killColor = Color.red;

    private Coroutine routine;

    private void Awake()
    {
        Instance = this;

        if (image == null) image = GetComponent<Image>();
        if (rect == null) rect = GetComponent<RectTransform>();

        image.color = hitColor;
        SetAlpha(0f);
        rect.localScale = Vector3.one;
      
    }

    // обычное попадание (белый)
    public void ShowHit()
    {
        Play(hitColor);
    }

    // убийство (красный)
    public void ShowKill()
    {
        Play(killColor);
    }

    private void Play(Color color)
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(PlayRoutine(color));
    }

    private IEnumerator PlayRoutine(Color color)
    {
        image.color = color;

        // сразу видно
        SetAlpha(1f);

        // POP: быстро увеличили и вернули к 1
        float t = 0f;
        Vector3 start = Vector3.one;
        Vector3 peak = Vector3.one * popScale;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.0001f, popInTime);
            rect.localScale = Vector3.Lerp(start, peak, t);
            yield return null;
        }

        // быстро обратно к 1
        rect.localScale = Vector3.one;

        // держим чуть-чуть
        yield return new WaitForSecondsRealtime(showTime);

        // fade out
        float a = 0.85f;
        float speed = 1f / Mathf.Max(0.0001f, fadeOutTime);

        while (a > 0f)
        {
            a -= Time.unscaledDeltaTime * speed;
            SetAlpha(a);
            yield return null;
        }

        SetAlpha(0f);
        routine = null;
    }

    private void SetAlpha(float a)
    {
        Color c = image.color;
        c.a = a;
        image.color = c;
    }
}
