using System;
using System.Collections.Generic;

public enum StatusId { Guard, Focus, Aim, Chamber }

public class StatusEffect
{
    public StatusId id;
    public string displayName;
    public string description;
    public bool isDebuff;
    public int stacks;
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