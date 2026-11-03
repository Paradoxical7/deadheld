using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CombatantHUD : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public TMP_Text nameText;
    public TMP_Text hpText;
    public Image hpFill;   // Image Type = Filled, Horizontal, Left
    public TMP_Text nrgText;
    public Image nrgFill;  // Image Type = Filled, Horizontal, Left
    public bool showTooltipOnHover = true;

    private TurnBasedPlayer stats;
    private StatusEffects statuses;
    private BuffBar tooltip;
    private string displayName;
    private bool hovering;

    public void Bind(string label, TurnBasedPlayer statsRef, StatusEffects statusRef, BuffBar tooltipBox)
    {
        if (statuses != null) statuses.Changed -= OnStatusesChanged;

        displayName = label;
        stats = statsRef;
        statuses = statusRef;
        tooltip = tooltipBox;
        if (statuses != null) statuses.Changed += OnStatusesChanged;

        if (nameText != null) nameText.text = label;
        gameObject.SetActive(true);
        Refresh();
    }

    public void Unbind()
    {
        if (statuses != null) statuses.Changed -= OnStatusesChanged;
        if (hovering && tooltip != null) tooltip.HideTooltip();
        hovering = false;
        stats = null;
        statuses = null;
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (stats != null) Refresh();
    }

    private void Refresh()
    {
        float maxHp = Mathf.Max(stats.baseHP, 0.0001f);
        float maxNrg = Mathf.Max(stats.baseNRG, 0.0001f);

        if (hpText != null) hpText.text = "HP: " + Mathf.CeilToInt(stats.currentHP) + "/" + Mathf.CeilToInt(stats.baseHP);
        if (hpFill != null) hpFill.fillAmount = Mathf.Clamp01(stats.currentHP / maxHp);
        if (nrgText != null) nrgText.text = "NRG: " + Mathf.CeilToInt(stats.currentNRG) + "/" + Mathf.CeilToInt(stats.baseNRG);
        if (nrgFill != null) nrgFill.fillAmount = Mathf.Clamp01(stats.currentNRG / maxNrg);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovering = true;
        ShowTooltip();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovering = false;
        if (tooltip != null) tooltip.HideTooltip();
    }

    private void OnStatusesChanged()
    {
        if (hovering) ShowTooltip();
    }

    private void ShowTooltip()
    {
        if (!showTooltipOnHover || tooltip == null || statuses == null) return;

        string body;
        if (statuses.All.Count == 0)
        {
            body = "No active effects";
        }
        else
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (StatusEffect e in statuses.All)
            {
                string color = e.isDebuff ? "#E57373" : "#81C784";
                sb.Append("<color=").Append(color).Append(">").Append(e.Title()).Append("</color>: ")
                  .Append(e.description).Append("\n");
            }
            body = sb.ToString().TrimEnd();
        }

        tooltip.ShowTooltipText(displayName, body, (RectTransform)transform);
    }
}