using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ItemSlot : MonoBehaviour, IPointerEnterHandler
{
    public Image iconImage;
    public TMP_Text nameText;
    public TMP_Text countText;
    public Button button;

    private ItemData item;
    private System.Action<ItemData> onHover;

    public void Setup(ItemData data, int count, System.Action<ItemData> onClick, System.Action<ItemData> onHoverCallback)
    {
        item = data;
        onHover = onHoverCallback;

        if (iconImage != null) { iconImage.sprite = data.icon; iconImage.enabled = data.icon != null; }
        if (nameText != null) nameText.text = data.itemName;
        if (countText != null) countText.text = count > 1 ? "x" + count : "";

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => { if (onClick != null) onClick(item); });
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (onHover != null) onHover(item);
    }
}