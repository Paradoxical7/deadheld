using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class StatusIconEntry
{
    public StatusId id;
    public Sprite icon;
}

public class BuffBar : MonoBehaviour
{
    public GameObject iconPrefab;
    public Transform container;
    public List<StatusIconEntry> statusIcons = new List<StatusIconEntry>();

    public RectTransform tooltipPanel;
    public TMP_Text tooltipTitle;
    public TMP_Text tooltipBody;
    public Vector2 tooltipOffset = new Vector2(0f, 8f);

    private StatusEffects bound;

    void Awake()
    {
        if (tooltipPanel != null)
        {
            CanvasGroup cg = tooltipPanel.GetComponent<CanvasGroup>();
            if (cg == null) cg = tooltipPanel.gameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false; // tooltip can never steal the hover
            cg.interactable = false;
            tooltipPanel.pivot = new Vector2(0.5f, 0f);
        }
    }

    void Start() { HideTooltip(); }

    public void Bind(StatusEffects statuses)
    {
        if (bound != null) bound.Changed -= Refresh;
        bound = statuses;
        if (bound != null) bound.Changed += Refresh;
        Refresh();
    }

    void OnDestroy()
    {
        if (bound != null) bound.Changed -= Refresh;
    }

    private void Refresh()
    {
        HideTooltip();

        foreach (Transform child in container)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        if (bound == null) return;

        foreach (StatusEffect effect in bound.All)
        {
            GameObject go = Instantiate(iconPrefab, container);
            go.GetComponent<BuffIcon>().Setup(effect, this, GetIcon(effect.id));
        }
    }

    private Sprite GetIcon(StatusId id)
    {
        foreach (StatusIconEntry entry in statusIcons)
            if (entry.id == id) return entry.icon;
        return null;
    }

    public void ShowTooltip(StatusEffect effect, RectTransform anchor)
    {
        ShowTooltipText(effect.Title(), effect.description, anchor);
    }

    public void ShowTooltipText(string title, string body, RectTransform anchor)
    {
        if (tooltipPanel == null) return;

        tooltipTitle.text = title;
        tooltipBody.text = body;
        tooltipPanel.gameObject.SetActive(true);
        LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipPanel);

        // place just above the anchor
        tooltipPanel.position = anchor.TransformPoint(new Vector3(0f, anchor.rect.height * 0.5f, 0f)) + (Vector3)tooltipOffset;

        // keep the whole tooltip inside the canvas
        Canvas canvas = tooltipPanel.GetComponentInParent<Canvas>().rootCanvas;
        RectTransform canvasRect = (RectTransform)canvas.transform;
        Vector3[] corners = new Vector3[4];
        tooltipPanel.GetWorldCorners(corners);
        Vector2 min = canvasRect.InverseTransformPoint(corners[0]);
        Vector2 max = canvasRect.InverseTransformPoint(corners[2]);
        Rect bounds = canvasRect.rect;

        Vector2 shift = Vector2.zero;
        if (min.x < bounds.xMin) shift.x = bounds.xMin - min.x;
        else if (max.x > bounds.xMax) shift.x = bounds.xMax - max.x;
        if (min.y < bounds.yMin) shift.y = bounds.yMin - min.y;
        else if (max.y > bounds.yMax) shift.y = bounds.yMax - max.y;

        tooltipPanel.position += canvasRect.TransformVector(shift);
    }

    public void HideTooltip()
    {
        if (tooltipPanel != null) tooltipPanel.gameObject.SetActive(false);
    }
}