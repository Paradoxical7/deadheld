using UnityEngine;
using System.Collections;

[System.Serializable]
public class TurnBasedPlayer
{
    public string name;
    public float baseHP;
    public float currentHP;
    public float baseNRG;
    public float currentNRG;

    public int agility; // Dodge chance
    public int nrgGain; // Chance to gain extra energy
}
