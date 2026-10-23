using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Drives the exploration HUD's HP and EN bars, and the parasite meter, from GameManager's persistent player stats.
// The bar fills are plain stretched images, so the bar is shown by narrowing the fill's right anchor.
public class ExplorationHUD : MonoBehaviour
{
    [SerializeField] private RectTransform hpFill;
    [SerializeField] private RectTransform energyFill;
    [SerializeField] private TMP_Text hpValue;
    [SerializeField] private TMP_Text energyValue;

    [Header("Parasite meter (built at runtime, right edge)")]
    [SerializeField] private Vector2 parasiteSize = new Vector2(56f, 520f);
    [SerializeField] private float parasiteEdgeOffset = 40f;
    [SerializeField] private Color parasiteFull = new Color(0.45f, 0.85f, 0.35f);
    [SerializeField] private Color parasiteHalf = new Color(0.95f, 0.65f, 0.15f);
    [SerializeField] private Color parasiteLow = new Color(0.85f, 0.1f, 0.08f);
    [SerializeField, Range(0f, 1f)] private float parasitePulseBelow = 0.2f;

    private GameObject parasiteMeter;
    private RectTransform parasiteFill;
    private Image parasiteFillImage;
    private TMP_Text parasiteTime;

    private void Awake()
    {
        if (hpFill == null) hpFill = transform.Find("HealthBar/Fill") as RectTransform;
        if (energyFill == null) energyFill = transform.Find("EnergyBar/Fill") as RectTransform;
        if (hpValue == null && hpFill != null) hpValue = MakeValueText(hpFill.parent);
        if (energyValue == null && energyFill != null) energyValue = MakeValueText(energyFill.parent);
        BuildParasiteMeter();
    }

    private void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        Show(hpFill, hpValue, gm.currentHP, gm.maxHP);
        Show(energyFill, energyValue, gm.currentEnergy, gm.maxEnergy);
        ShowParasite(gm);
    }

    // Vertical meter that empties downwards; amber at half, red and pulsing when nearly empty.
    private void ShowParasite(GameManager gm)
    {
        parasiteMeter.SetActive(gm.parasiteActive);
        if (!gm.parasiteActive) return;

        float t = gm.parasiteMax > 0f ? Mathf.Clamp01(gm.parasiteCurrent / gm.parasiteMax) : 0f;
        parasiteFill.gameObject.SetActive(t * parasiteSize.y > 11f);   // below the 10 px of padding the fill would turn inside out
        parasiteFill.anchorMax = new Vector2(1f, t);

        Color c = t > 0.5f
            ? Color.Lerp(parasiteHalf, parasiteFull, (t - 0.5f) * 2f)
            : Color.Lerp(parasiteLow, parasiteHalf, t * 2f);
        if (t < parasitePulseBelow) c.a = Mathf.Lerp(0.35f, 1f, Mathf.PingPong(Time.unscaledTime * 2.5f, 1f));
        parasiteFillImage.color = c;

        float secondsLeft = gm.parasiteDrainPerSecond > 0f ? gm.parasiteCurrent / gm.parasiteDrainPerSecond : gm.parasiteCurrent;
        int s = Mathf.CeilToInt(secondsLeft);
        parasiteTime.text = (s / 60) + ":" + (s % 60).ToString("00");
        parasiteTime.color = t < parasitePulseBelow ? parasiteLow : Color.white;
    }

    private void BuildParasiteMeter()
    {
        parasiteMeter = new GameObject("ParasiteMeter", typeof(RectTransform), typeof(Image), typeof(Outline));
        parasiteMeter.transform.SetParent(transform, false);
        var rt = (RectTransform)parasiteMeter.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 0.5f);
        rt.sizeDelta = parasiteSize;
        rt.anchoredPosition = new Vector2(-parasiteEdgeOffset, 0f);
        var bg = parasiteMeter.GetComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.06f, 0.85f);
        bg.raycastTarget = false;
        var outline = parasiteMeter.GetComponent<Outline>();
        outline.effectColor = new Color(0.6f, 0.6f, 0.6f, 0.9f);
        outline.effectDistance = new Vector2(2f, -2f);

        var fillGO = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillGO.transform.SetParent(rt, false);
        parasiteFill = (RectTransform)fillGO.transform;
        parasiteFill.anchorMin = Vector2.zero;
        parasiteFill.anchorMax = Vector2.one;
        parasiteFill.offsetMin = new Vector2(5f, 5f);
        parasiteFill.offsetMax = new Vector2(-5f, -5f);
        parasiteFillImage = fillGO.GetComponent<Image>();
        parasiteFillImage.raycastTarget = false;

        var title = MakeLabel("Label", rt, "PARASITE", 18f, new Vector2(0.5f, 1f), new Vector2(0f, 22f));
        title.characterSpacing = 6f;
        parasiteTime = MakeLabel("Time", rt, "", 22f, new Vector2(0.5f, 0f), new Vector2(0f, -22f));

        parasiteMeter.SetActive(false);
    }

    private static TMP_Text MakeLabel(string name, Transform parent, string text, float size, Vector2 anchor, Vector2 offset)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.sizeDelta = new Vector2(200f, 32f);
        rt.anchoredPosition = offset;

        var t = go.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null) t.font = TMP_Settings.defaultFontAsset;
        t.text = text;
        t.fontSize = size;
        t.fontStyle = FontStyles.Bold;
        t.alignment = TextAlignmentOptions.Center;
        t.color = Color.white;
        t.raycastTarget = false;
        return t;
    }

    private static void Show(RectTransform fill, TMP_Text label, float value, float max)
    {
        float t = max > 0f ? Mathf.Clamp01(value / max) : 0f;
        if (fill != null)
        {
            fill.gameObject.SetActive(t > 0.001f);   // a zero-width fill would turn inside out
            fill.anchorMax = new Vector2(t, fill.anchorMax.y);
        }
        if (label != null) label.text = Mathf.CeilToInt(value) + " / " + Mathf.RoundToInt(max);
    }

    private static TMP_Text MakeValueText(Transform bar)
    {
        var go = new GameObject("Value", typeof(RectTransform));
        go.transform.SetParent(bar, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(8f, 0f);
        rt.offsetMax = new Vector2(-10f, 0f);

        var t = go.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null) t.font = TMP_Settings.defaultFontAsset;
        t.fontSize = 20f;
        t.alignment = TextAlignmentOptions.Right;
        t.color = Color.white;
        t.raycastTarget = false;
        return t;
    }
}
