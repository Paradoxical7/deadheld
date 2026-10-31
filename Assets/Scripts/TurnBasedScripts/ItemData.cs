using UnityEngine;

public enum ItemEffect { Heal, Adrenaline }

[CreateAssetMenu(fileName = "NewItem", menuName = "Combat/Item")]
public class ItemData : ScriptableObject
{
    public string itemName;
    [TextArea] public string description;
    public Sprite icon;
    public ItemEffect effect;
    public float amount = 25f; // Heal: HP restored. Adrenaline: extra energy per turn
    public int duration = 2;   // Adrenaline: how many turns
}