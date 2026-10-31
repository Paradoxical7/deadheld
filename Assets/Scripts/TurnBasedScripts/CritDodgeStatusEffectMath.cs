using UnityEngine;

public struct AttackRoll
{
    public float damage;
    public bool isCrit;
}

public static class Combat
{
    public const float CritMultiplier = 2f;
    public const float ChamberBonusPerShell = 0.10f;
    public const int MaxChamberShells = 3;

    // Call once per attacking move. Consumes Chamber and Aim.
    public static AttackRoll PrepareAttack(float baseDamage, float critChancePercent, StatusEffects statuses)
    {
        float damage = baseDamage;

        int shells = statuses.Stacks(StatusId.Chamber);
        if (shells > 0)
        {
            damage *= 1f + ChamberBonusPerShell * shells;
            statuses.Remove(StatusId.Chamber);
        }

        bool crit;
        if (statuses.Has(StatusId.Aim))
        {
            crit = true;
            statuses.Remove(StatusId.Aim);
        }
        else
        {
            crit = Random.value * 100f < critChancePercent;
        }

        if (crit) damage *= CritMultiplier;
        return new AttackRoll { damage = damage, isCrit = crit };
    }

    public static bool RollDodge(float dodgeChancePercent)
    {
        return Random.value * 100f < dodgeChancePercent;
    }
}