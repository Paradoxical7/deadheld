using UnityEngine;

public enum RewardType { Antidote, CritChance, DodgeChance, ExtraEnergyChance, DamageBoost, HealthBoost, MaxEnergy, Item, Weapon }
public enum RewardRarity { Common, Uncommon, Rare }

[System.Serializable]
public class RewardOption
{
    public string title;
    [TextArea] public string description;
    public Sprite icon;
    public RewardType type;
    public RewardRarity rarity;
    public float weight = 10f;  // higher = shows up more often
    public float amount; // percent / HP / seconds / energy / item count
    public ItemData item; // Item rewards
    public WeaponData weapon; // Weapon rewards
}