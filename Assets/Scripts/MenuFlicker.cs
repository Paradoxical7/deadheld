using UnityEngine;

// Makes a UI group stutter now and then, like a failing light.
[RequireComponent(typeof(CanvasGroup))]
public class MenuFlicker : MonoBehaviour
{
    [SerializeField] private Vector2 gapSeconds = new Vector2(2.5f, 7f);
    [SerializeField] private int minFlashes = 2;
    [SerializeField] private int maxFlashes = 5;
    [SerializeField, Range(0f, 1f)] private float dimAlpha = 0.25f;

    private CanvasGroup group;
    private float nextFlicker;
    private int flashesLeft;
    private float flashUntil;

    private void Awake() { group = GetComponent<CanvasGroup>(); }

    // Play Mode Options skips the reload, so these keep their last-session values.
    private void OnEnable()
    {
        flashesLeft = 0;
        nextFlicker = Time.unscaledTime + Random.Range(gapSeconds.x, gapSeconds.y);
        if (group != null) group.alpha = 1f;
    }

    private void Update()
    {
        float now = Time.unscaledTime;

        if (flashesLeft == 0)
        {
            group.alpha = 1f;
            if (now < nextFlicker) return;
            flashesLeft = Random.Range(minFlashes, maxFlashes + 1) * 2;   // each flash is an off + an on
        }

        if (now < flashUntil) return;

        flashesLeft--;
        bool off = flashesLeft % 2 == 1;
        group.alpha = off ? Random.Range(dimAlpha * 0.5f, dimAlpha) : 1f;
        flashUntil = now + Random.Range(0.03f, 0.09f);

        if (flashesLeft == 0) nextFlicker = now + Random.Range(gapSeconds.x, gapSeconds.y);
    }
}
