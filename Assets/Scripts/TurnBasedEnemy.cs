using UnityEngine;
using System.Collections;

[System.Serializable]
public class TurnBasedEnemy
{
    public string name;
    public enum Type
    {
        firstZone,
        secondZone,
        ThirdZone
    }
    public Type EnemyType;
    public float baseHP;
    public float currentHP;
    public float baseNRG;
    public float currentNRG;

    public float baseATK;
    public float currentATK;
    public int agility; // Dodge chance
    public int nrgGain; // Chance to gain extra energy
}
