using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RewardScreen : MonoBehaviour
{
    public GameObject panel;
    public List<RewardCard> cards = new List<RewardCard>(); // the 3 cards
    public List<RewardOption> pool = new List<RewardOption>();

    private System.Action onDone;

    void Awake()
    {
        if (panel != null) panel.SetActive(false);
    }

    // call this when the player wins; 'done' runs after they pick (swap scenes there)
    public void Show(System.Action done)
    {
        onDone = done;

        List<RewardOption> options = Pick(cards.Count);
        Debug.Log("RewardScreen: pool=" + pool.Count + ", cards=" + cards.Count + ", options picked=" + options.Count);

        if (options.Count == 0)
        {
            Debug.LogWarning("RewardScreen: nothing to offer (empty pool, no cards assigned, or every entry is missing its item/weapon). Skipping rewards.");
            Finish();
            return;
        }

        gameObject.SetActive(true);        // works even if this object was left disabled in the editor
        transform.SetAsLastSibling();      // draw on top of the other battle UI
        panel.SetActive(true);

        for (int i = 0; i < cards.Count; i++)
        {
            if (i < options.Count)
            {
                cards[i].gameObject.SetActive(true);
                cards[i].Setup(options[i], OnPicked);
            }
            else cards[i].gameObject.SetActive(false);
        }
    }

    bool IsEligible(RewardOption r)
    {
        if (r.type == RewardType.Weapon) return r.weapon != null && !PlayerProgress.Owns(r.weapon);
        if (r.type == RewardType.Item || r.type == RewardType.Antidote) return r.item != null;
        return true;
    }

    // weighted pick without repeats
    List<RewardOption> Pick(int count)
    {
        List<RewardOption> candidates = pool.Where(IsEligible).ToList();
        List<RewardOption> picked = new List<RewardOption>();

        while (picked.Count < count && candidates.Count > 0)
        {
            float total = candidates.Sum(c => Mathf.Max(0f, c.weight));
            if (total <= 0f) break;

            float roll = Random.value * total;
            RewardOption chosen = candidates[candidates.Count - 1];
            foreach (RewardOption c in candidates)
            {
                roll -= Mathf.Max(0f, c.weight);
                if (roll <= 0f) { chosen = c; break; }
            }

            picked.Add(chosen);
            candidates.Remove(chosen);
        }
        return picked;
    }

    void OnPicked(RewardOption r)
    {
        Apply(r);
        panel.SetActive(false);
        Finish();
    }

    void Finish()
    {
        System.Action cb = onDone;
        onDone = null;
        if (cb != null) cb();
    }

    void Apply(RewardOption r)
    {
        Debug.Log("Reward chosen: " + r.title);

        switch (r.type)
        {
            case RewardType.CritChance: PlayerProgress.bonusCrit += r.amount; break;
            case RewardType.DodgeChance: PlayerProgress.bonusDodge += r.amount; break;
            case RewardType.ExtraEnergyChance: PlayerProgress.bonusExtraEnergyChance += r.amount; break;
            case RewardType.DamageBoost: PlayerProgress.damageBonus += r.amount / 100f; break; // amount 3 = +3%
            case RewardType.MaxEnergy: PlayerProgress.bonusMaxEnergy += r.amount; break;

            case RewardType.HealthBoost:
                {
                    int add = Mathf.RoundToInt(r.amount);

                    GameManager gm = GameManager.Instance;
                    if (gm != null)
                    {
                        gm.maxHP += add;
                        gm.currentHP += add;
                    }

                    // also raise the live battle HP so whatever copies it back keeps the bonus
                    foreach (PlayerState ps in FindObjectsByType<PlayerState>(FindObjectsSortMode.None))
                    {
                        ps.player.baseHP += add;
                        ps.player.currentHP += add;
                    }
                    break;
                }

            case RewardType.Item:
            case RewardType.Antidote:
                PlayerProgress.AddItem(r.item, Mathf.Max(1, Mathf.RoundToInt(r.amount)));
                break;

            case RewardType.Weapon:
                PlayerProgress.AddWeapon(r.weapon);
                break;
        }
    }
}