using UnityEngine;

public enum MoveKind { Attack, Heal, Buff, Summon }
public enum MoveTarget { SingleEnemy, AllEnemies, Self }
public enum MoveEffect { None, Aim, Chamber }

[System.Serializable]
public class PlayerMove
{
    public string moveName;
    public float energyCost;
    public float damage; // flat damage (replaced dmgMultiplier)
    public int cooldown; // cooldown as in turns
    public MoveKind kind = MoveKind.Attack; // Attack by default
    public MoveTarget target = MoveTarget.SingleEnemy;
    public MoveEffect effect = MoveEffect.None;
    public float healAmount; // only used when kind == Heal
    public GameObject summonPrefab; // only used by Summon moves
}
