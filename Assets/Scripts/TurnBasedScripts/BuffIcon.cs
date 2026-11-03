using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BuffIcon : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Image background;   // the square (root Image)
    public Image iconImage;    // child image
    public TMP_Text stackText; // small number in the corner
    public Color buffColor = new Color(0.2f, 0.6f, 0.3f, 1f);
    public Color debuffColor = new Color(0.7f, 0.2f, 0.2f, 1f);

    private StatusEffect effect;
    private BuffBar owner;

    public void Setup(StatusEffect e, BuffBar bar, Sprite icon)
    {
        effect = e;
        owner = bar;
        background.color = e.isDebuff ? debuffColor : buffColor;
        iconImage.sprite = icon;
        iconImage.enabled = icon != null;
        stackText.text = e.stacks > 1 ? e.stacks.ToString() : "";
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        owner.ShowTooltip(effect, (RectTransform)transform);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        owner.HideTooltip();
    }
}