using System;
using System.Collections.Generic;

public enum StatusId { Guard, Focus, Aim, Chamber, Fortify }

public class StatusEffect
{
    public StatusId id;
    public string displayName;
    public string description;
    public bool isDebuff;
    public int stacks;
    public int turnsLeft;   // 0 = lasts until consumed or cleared
    public float magnitude; // e.g. 0.2 = 20%

    public string Title()
    {
        string t = displayName;
        if (stacks > 1) t += " x" + stacks;
        if (turnsLeft > 0) t += " (" + turnsLeft + (turnsLeft == 1 ? " turn)" : " turns)");
        return t;
    }
}

public class StatusEffects
{
    public event Action Changed;
    private readonly List<StatusEffect> list = new List<StatusEffect>();
    public IReadOnlyList<StatusEffect> All => list;

    public bool Has(StatusId id) { return Find(id) != null; }

    public int Stacks(StatusId id)
    {
        StatusEffect e = Find(id);
        return e != null ? e.stacks : 0;
    }

    public float Magnitude(StatusId id)
    {
        StatusEffect e = Find(id);
        return e != null ? e.magnitude : 0f;
    }

    public void Apply(StatusId id, string displayName, bool isDebuff, int maxStacks, Func<int, string> describe)
    {
        StatusEffect e = Find(id);
        if (e == null)
        {
            e = new StatusEffect { id = id, displayName = displayName, isDebuff = isDebuff };
            list.Add(e);
        }
        e.stacks = Math.Min(e.stacks + 1, maxStacks);
        e.description = describe(e.stacks);
        Changed?.Invoke();
    }

    // timed effect: re-applying refreshes the duration
    public void ApplyTimed(StatusId id, string displayName, bool isDebuff, float magnitude, int turns, string description)
    {
        StatusEffect e = Find(id);
        if (e == null)
        {
            e = new StatusEffect { id = id, displayName = displayName, isDebuff = isDebuff };
            list.Add(e);
        }
        e.stacks = 1;
        e.magnitude = magnitude;
        e.turnsLeft = turns;
        e.description = description;
        Changed?.Invoke();
    }

    // call at the start of the owner's turn
    public void Tick()
    {
        bool changed = false;
        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (list[i].turnsLeft <= 0) continue;
            list[i].turnsLeft--;
            if (list[i].turnsLeft <= 0) list.RemoveAt(i);
            changed = true;
        }
        if (changed) Changed?.Invoke();
    }

    public void Remove(StatusId id)
    {
        StatusEffect e = Find(id);
        if (e == null) return;
        list.Remove(e);
        Changed?.Invoke();
    }

    private StatusEffect Find(StatusId id)
    {
        foreach (StatusEffect e in list)
            if (e.id == id) return e;
        return null;
    }
}