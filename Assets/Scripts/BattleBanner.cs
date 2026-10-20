using System.Collections;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class BattleBanner : MonoBehaviour
{
    public TMP_Text label;
    public float fadeTime = 0.15f;
    public float holdTime = 0.9f;

    private CanvasGroup group;

    void Awake()
    {
        group = GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;
    }

    public IEnumerator Show(string text)
    {
        if (label != null) label.text = text;
        yield return StartCoroutine(Fade(0f, 1f));
        yield return new WaitForSeconds(holdTime);
        yield return StartCoroutine(Fade(1f, 0f));
    }

    private IEnumerator Fade(float from, float to)
    {
        float duration = Mathf.Max(fadeTime, 0.01f);
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            group.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        group.alpha = to;
    }
}