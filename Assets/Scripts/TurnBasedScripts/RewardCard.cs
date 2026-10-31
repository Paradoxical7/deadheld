using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RewardCard : MonoBehaviour
{
    public Button button;
    public TMP_Text titleText;
    public TMP_Text descriptionText;
    public TMP_Text rarityText;
    public Image iconImage;
    public Image rarityBar;
    public Color commonColor = new Color(0.7f, 0.7f, 0.7f);
    public Color uncommonColor = new Color(0.3f, 0.7f, 1f);
    public Color rareColor = new Color(1f, 0.7f, 0.2f);

    public void Setup(RewardOption option, System.Action<RewardOption> onPick)
    {
        if (titleText != null) titleText.text = option.title;
        if (descriptionText != null) descriptionText.text = option.description;
        if (rarityText != null) rarityText.text = option.rarity.ToString();
        if (iconImage != null) { iconImage.sprite = option.icon; iconImage.enabled = option.icon != null; }

        Color c = option.rarity == RewardRarity.Rare ? rareColor
                : option.rarity == RewardRarity.Uncommon ? uncommonColor : commonColor;
        if (rarityBar != null) rarityBar.color = c;
        if (rarityText != null) rarityText.color = c;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onPick(option));
    }
}