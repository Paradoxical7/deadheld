using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class RarityFrame
{
    public Sprite normal;       // the card frame for this rarity
    public Sprite highlighted;  // optional: shown on hover (falls back to normal)
    public Sprite pressed;      // optional: shown while clicking (falls back to highlighted)
}

public class RewardCard : MonoBehaviour
{
    public Button button;
    public Image frameImage;          // the Image that shows the card frame
    public TMP_Text titleText;
    public TMP_Text descriptionText;
    public TMP_Text rarityText;
    public Image iconImage;

    [Header("Frame sprites per rarity")]
    public RarityFrame common;
    public RarityFrame uncommon;
    public RarityFrame rare;

    public void Setup(RewardOption option, System.Action<RewardOption> onPick)
    {
        if (titleText != null) titleText.text = option.title;
        if (descriptionText != null) descriptionText.text = option.description;
        if (rarityText != null) rarityText.text = option.rarity.ToString();
        if (iconImage != null) { iconImage.sprite = option.icon; iconImage.enabled = option.icon != null; }

        ApplyFrame(option.rarity);

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onPick(option));
    }

    private void ApplyFrame(RewardRarity rarity)
    {
        RarityFrame f = rarity == RewardRarity.Rare ? rare
                      : rarity == RewardRarity.Uncommon ? uncommon : common;

        if (frameImage == null || f == null || f.normal == null) return; // nothing assigned: keep the current look

        frameImage.sprite = f.normal;

        // hover and click use sprite swap, with this rarity's sprites
        button.targetGraphic = frameImage;
        button.transition = Selectable.Transition.SpriteSwap;

        SpriteState state = new SpriteState();
        state.highlightedSprite = f.highlighted != null ? f.highlighted : f.normal;
        state.selectedSprite = state.highlightedSprite;
        state.pressedSprite = f.pressed != null ? f.pressed : state.highlightedSprite;
        button.spriteState = state;
    }
}