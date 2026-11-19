using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Cryo wake-up at the start of a run: the eyes blink open from black, then a few lines of inner monologue.
// Plays once per run (GameManager.introPlayed), so coming back from a battle doesn't replay it.
// Builds its own overlay canvas at runtime, so all it needs in the scene is this component.
public class CryoIntro : MonoBehaviour
{
    [System.Serializable]
    public class Line
    {
        [TextArea] public string text;
        public float hold = 2.5f;     // seconds fully visible

        public Line(string text, float hold) { this.text = text; this.hold = hold; }
    }

    [Header("Blink")]
    [Tooltip("Multiplies every blink timing: 2 = twice as fast, 0.5 = twice as slow.")]
    [SerializeField] private float blinkSpeed = 1f;
    [SerializeField] private float startBlack = 1f;            // eyes shut before anything happens
    [Tooltip("How far the eyes get on each attempt before drooping shut again (0 shut, 1 open). One entry per blink.")]
    [SerializeField] private float[] blinkPeaks = { 0.3f, 0.6f };
    [SerializeField] private float blinkOpenTime = 0.9f;       // slow lift
    [SerializeField] private float blinkPeekTime = 0.3f;       // hold at the peak
    [SerializeField] private float blinkCloseTime = 0.3f;      // quicker droop
    [SerializeField, Range(0f, 1f)] private float blinkCloseTo = 0.08f;   // how shut each droop gets
    [SerializeField] private float finalOpenTime = 2f;

    [Header("Monologue")]
    [SerializeField] private List<Line> lines = new List<Line>
    {
        new Line("...Cold. So cold.", 2.5f),
        new Line("Where am I?", 2f),
        new Line("How did I get here?", 2.5f),
        new Line("I can't remember anything.", 2.5f),
        new Line("Hello? ...Is anyone there?", 3f),
        new Line("Why is it so quiet.", 3f),
    };
    [SerializeField] private float lineFadeIn = 0.8f;
    [SerializeField] private float lineFadeOut = 0.8f;
    [SerializeField] private float lineGap = 0.4f;
    [SerializeField] private float textSize = 34f;
    [SerializeField] private Color textColor = new Color(0.78f, 0.8f, 0.82f, 1f);

    [Header("Testing")]
    [Tooltip("Editor only: don't play when entering Play mode. Builds always play it at the start of a run.")]
    [SerializeField] private bool skipInEditor;

    private TankController player;
    private GameObject canvasGO;
    private Image fade;
    private RectTransform topLid, bottomLid;
    private TMP_Text subtitle;
    private bool running, frozen;
    private bool skipStep, skipAll;

    private void Start()
    {
#if UNITY_EDITOR
        if (skipInEditor) return;
#endif
        // Start, not Awake: by now GameManager has woken up and handled any battle return point.
        var gm = GameManager.Instance;
        if (gm != null && (gm.introPlayed || gm.hasReturnPoint)) return;
        Begin();
    }

    // Right-click the component in Play mode.
    [ContextMenu("Replay Intro")]
    private void Replay()
    {
        if (!Application.isPlaying) { Debug.Log("CryoIntro: Replay only works in Play mode."); return; }
        if (GameManager.Instance != null) GameManager.Instance.introPlayed = false;
        Finish();
        Begin();
    }

    private void Begin()
    {
        // Marked at the start, so a battle that starts mid-monologue doesn't bring it back.
        if (GameManager.Instance != null) GameManager.Instance.introPlayed = true;
        player = FindFirstObjectByType<TankController>();
        BuildOverlay();
        running = true;
        StartCoroutine(Run());
    }

    private void OnDisable() => Finish();

    private void Update()
    {
        if (!running) return;
        var kb = Keyboard.current;
        var mouse = Mouse.current;
        bool click = mouse != null && mouse.leftButton.wasPressedThisFrame;

        if (kb != null && kb.escapeKey.wasPressedThisFrame) { skipAll = true; return; }
        // The player is frozen during the blink, so any key will do. Once they can walk, WASD mustn't eat the lines.
        if (frozen) skipStep |= click || (kb != null && kb.anyKey.wasPressedThisFrame);
        else skipStep |= click || (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame));
    }

    private IEnumerator Run()
    {
        Freeze(true);
        yield return Blink();
        SetOpen(1f);
        fade.enabled = false;   // black and lids gone; the overlay stays for the subtitles
        topLid.gameObject.SetActive(false);
        bottomLid.gameObject.SetActive(false);
        Freeze(false);

        skipStep = false;
        foreach (var line in lines)
        {
            if (skipAll) break;
            yield return ShowLine(line);
        }
        Finish();
    }

    private IEnumerator Blink()
    {
        float speed = Mathf.Max(0.01f, blinkSpeed);
        SetOpen(0f);
        yield return Hold(startBlack / speed);

        float open = 0f;
        foreach (float peak in blinkPeaks)
        {
            yield return Ease(open, peak, blinkOpenTime / speed);
            yield return Hold(blinkPeekTime / speed);
            yield return Ease(peak, blinkCloseTo, blinkCloseTime / speed);
            open = blinkCloseTo;
        }
        yield return Ease(open, 1f, finalOpenTime / speed);
    }

    private IEnumerator ShowLine(Line line)
    {
        subtitle.text = line.text;
        skipStep = false;
        // a skip during fade-in or hold jumps straight to the fade-out
        yield return FadeText(0f, 1f, lineFadeIn, true);
        if (!skipStep && !skipAll) yield return Hold(line.hold);
        float fadeOut = skipAll ? 0f : skipStep ? Mathf.Min(lineFadeOut, 0.25f) : lineFadeOut;
        skipStep = false;
        yield return FadeText(subtitle.alpha, 0f, fadeOut, false);
        if (!skipAll) yield return Hold(lineGap);
        skipStep = false;
    }

    private IEnumerator FadeText(float from, float to, float seconds, bool skippable)
    {
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            if (skipAll || (skippable && skipStep)) break;
            subtitle.alpha = Mathf.Lerp(from, to, t / seconds);
            yield return null;
        }
        if (!skippable || !skipStep) subtitle.alpha = to;
    }

    private IEnumerator Ease(float from, float to, float seconds)
    {
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            if (skipStep || skipAll) yield break;
            SetOpen(Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / seconds)));
            yield return null;
        }
        SetOpen(to);
    }

    private IEnumerator Hold(float seconds)
    {
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            if (skipStep || skipAll) yield break;
            yield return null;
        }
    }

    // 0 = eyes shut (black, lids meeting), 1 = wide open (nothing on screen).
    private void SetOpen(float open)
    {
        open = Mathf.Clamp01(open);
        fade.color = new Color(0f, 0f, 0f, Mathf.Pow(1f - open, 1.5f));
        float lid = (1f - open) * 0.62f;   // over half each, so the soft edges overlap when shut
        topLid.anchorMin = new Vector2(0f, 1f - lid);
        topLid.anchorMax = Vector2.one;
        bottomLid.anchorMin = Vector2.zero;
        bottomLid.anchorMax = new Vector2(1f, lid);
    }

    // Same lock the keypad and NPC use; FirstPersonLook also stops looking while TankController is off.
    private void Freeze(bool on)
    {
        frozen = on;
        if (player == null) return;
        player.enabled = !on;
        if (!on) return;

        var rb = player.GetComponent<Rigidbody>();
        if (rb != null) rb.linearVelocity = Vector3.zero;
        var anim = player.GetComponentInChildren<Animator>();
        if (anim != null) anim.SetFloat("Speed", 0f);
    }

    private void Finish()
    {
        StopAllCoroutines();
        if (frozen) Freeze(false);
        if (canvasGO != null) Destroy(canvasGO);
        running = false;
        skipStep = skipAll = false;
    }

    // Overlay sits under the parasite death screen (1000) and over the HUD.
    private void BuildOverlay()
    {
        canvasGO = new GameObject("CryoIntroOverlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        fade = Stretch("Black", null).GetComponent<Image>();

        // Lids are a soft-edged black gradient: solid at the screen edge, fading out toward the middle.
        var tex = new Texture2D(1, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < 64; y++) tex.SetPixel(0, y, new Color(0f, 0f, 0f, Mathf.SmoothStep(0f, 1f, y / 63f / 0.35f)));
        tex.Apply();
        var lidSprite = Sprite.Create(tex, new Rect(0, 0, 1, 64), new Vector2(0.5f, 0.5f));
        topLid = Stretch("TopLid", lidSprite);
        bottomLid = Stretch("BottomLid", lidSprite);
        bottomLid.localScale = new Vector3(1f, -1f, 1f);   // flipped so its solid side faces the bottom edge

        var textGO = new GameObject("Subtitle", typeof(RectTransform));
        textGO.transform.SetParent(canvasGO.transform, false);
        var rt = (RectTransform)textGO.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(1400f, 100f);
        rt.anchoredPosition = new Vector2(0f, 140f);
        subtitle = textGO.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null) subtitle.font = TMP_Settings.defaultFontAsset;
        subtitle.fontSize = textSize;
        subtitle.fontStyle = FontStyles.Italic;
        subtitle.color = textColor;
        subtitle.alignment = TextAlignmentOptions.Center;
        subtitle.characterSpacing = 4f;
        subtitle.raycastTarget = false;
        subtitle.alpha = 0f;
    }

    private RectTransform Stretch(string name, Sprite sprite)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(canvasGO.transform, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = Color.black;
        img.raycastTarget = false;
        return rt;
    }
}
