using UnityEngine;

[System.Serializable]
public class TurnBasedPlayer
{
    public string name;
    public float baseHP;
    public float currentHP;
    public float baseNRG;
    public float currentNRG;

    public float baseATK;
    public float currentATK;

    public int agility; // Dodge chance
    public int nrgGain; // Chance to gain extra energy
}
