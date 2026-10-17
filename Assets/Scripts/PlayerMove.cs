using UnityEngine;

public enum MoveKind { Attack, Heal }

[System.Serializable]
public class PlayerMove
{
    public string moveName;
    public float energyCost;
    public float dmgMultiplier;
    public MoveKind kind = MoveKind.Attack; // Attack by default
    public float healAmount;                // only used when kind == Heal
}
