using TMPro;
using UnityEngine;

// Drives the exploration HUD's HP and EN bars from GameManager's persistent player stats.
// The bar fills are plain stretched images, so the bar is shown by narrowing the fill's right anchor.
public class ExplorationHUD : MonoBehaviour
{
    [SerializeField] private RectTransform hpFill;
    [SerializeField] private RectTransform energyFill;
    [SerializeField] private TMP_Text hpValue;
    [SerializeField] private TMP_Text energyValue;

    private void Awake()
    {
        if (hpFill == null) hpFill = transform.Find("HealthBar/Fill") as RectTransform;
        if (energyFill == null) energyFill = transform.Find("EnergyBar/Fill") as RectTransform;
        if (hpValue == null && hpFill != null) hpValue = MakeValueText(hpFill.parent);
        if (energyValue == null && energyFill != null) energyValue = MakeValueText(energyFill.parent);
    }

    private void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        Show(hpFill, hpValue, gm.currentHP, gm.maxHP);
        Show(energyFill, energyValue, gm.currentEnergy, gm.maxEnergy);
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
