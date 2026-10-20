using UnityEngine;

[System.Serializable]
public class TurnBasedPlayer
{
    public string name;
    public float baseHP;
    public float currentHP;
    public float baseNRG;
    public float currentNRG;

    [Range(0, 100)] public float dodgeChance = 10f; // percent
    [Range(0, 100)] public float critChance = 15f;  // percent

    public int nrgGain; // Chance to gain extra energy
}
