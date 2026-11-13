using System.Collections.Generic;
using UnityEngine;

public static class PlayerProgress
{
    // starting stats: edit these
    public const float BaseMaxEnergy = 5f;
    public const float BaseDodge = 10f;
    public const float BaseCrit = 15f;
    public const float BaseExtraEnergyChance = 0f;

    // permanent bonuses earned from rewards
    public static float bonusMaxEnergy;
    public static float bonusDodge;
    public static float bonusCrit;
    public static float bonusExtraEnergyChance;
    public static float damageBonus;            // 0.03 = +3% on every damaging move
    public static float pendingParasiteSeconds; // Antidote time waiting for the overworld

    public static readonly List<WeaponData> ownedWeapons = new List<WeaponData>();
    public static WeaponData equippedWeapon;
    public static readonly Dictionary<ItemData, int> items = new Dictionary<ItemData, int>();

    public static float MaxEnergy { get { return BaseMaxEnergy + bonusMaxEnergy; } }
    public static float Dodge { get { return Mathf.Clamp(BaseDodge + bonusDodge, 0f, 100f); } }
    public static float Crit { get { return Mathf.Clamp(BaseCrit + bonusCrit, 0f, 100f); } }
    public static float ExtraEnergyChance { get { return Mathf.Clamp(BaseExtraEnergyChance + bonusExtraEnergyChance, 0f, 100f); } }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnPlay() { ResetAll(); }

    public static void ResetAll()
    {
        bonusMaxEnergy = bonusDodge = bonusCrit = bonusExtraEnergyChance = 0f;
        damageBonus = 0f;
        pendingParasiteSeconds = 0f;
        ownedWeapons.Clear();
        equippedWeapon = null;
        items.Clear();
    }

    // a new game owns the starting weapon
    public static void EnsureInit(WeaponData startingWeapon)
    {
        if (ownedWeapons.Count == 0 && startingWeapon != null)
        {
            ownedWeapons.Add(startingWeapon);
            equippedWeapon = startingWeapon;
        }
    }

    public static bool Owns(WeaponData w) { return ownedWeapons.Contains(w); }

    public static void AddWeapon(WeaponData w)
    {
        if (w != null && !ownedWeapons.Contains(w)) ownedWeapons.Add(w);
    }

    public static void Equip(WeaponData w)
    {
        if (w != null && ownedWeapons.Contains(w)) equippedWeapon = w;
    }

    public static void AddItem(ItemData item, int count = 1)
    {
        if (item == null) return;
        int c;
        items.TryGetValue(item, out c);
        items[item] = c + count;
    }

    public static bool UseItem(ItemData item)
    {
        int c;
        if (item == null || !items.TryGetValue(item, out c) || c <= 0) return false;
        if (c == 1) items.Remove(item); else items[item] = c - 1;
        return true;
    }

    // the overworld's parasite meter calls this to collect Antidote time
    public static float ConsumeParasiteSeconds()
    {
        float s = pendingParasiteSeconds;
        pendingParasiteSeconds = 0f;
        return s;
    }
}