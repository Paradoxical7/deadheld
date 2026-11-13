using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerState : MonoBehaviour
{
    public TurnBasedPlayer player;
    private BattleStateMachine BSM;

    public enum TurnState { WAITING, ACTION, DEAD }
    public TurnState currentState;

    public GameObject selector;
    public GameObject enemyToAttack;
    private bool actionStarted = false;
    private Vector3 startPosition;
    private float animSpeed = 0.3f;

    // Moves come from the equipped weapon (the one chosen in the inventory wins)
    public WeaponData weapon;
    private static readonly List<PlayerMove> noMoves = new List<PlayerMove>();
    public List<PlayerMove> Moves { get { return weapon != null ? weapon.moves : noMoves; } }

    public float focusExtraEnergy = 1f;
    public float focusDamageIncrease = 0.15f;
    public float guardDamageReduction = 0.5f;

    [System.NonSerialized] public StatusEffects statuses = new StatusEffects();
    private readonly Dictionary<PlayerMove, int> cooldowns = new Dictionary<PlayerMove, int>();
    private PlayerMove pendingMove;

    void Start()
    {
        startPosition = transform.position;
        selector.SetActive(false);
        BSM = GameObject.Find("BattleManager").GetComponent<BattleStateMachine>();
        currentState = TurnState.WAITING;

        // weapon and stats come from PlayerProgress (base stats + rewards)
        PlayerProgress.EnsureInit(weapon);
        if (PlayerProgress.equippedWeapon != null) weapon = PlayerProgress.equippedWeapon;
        player.baseNRG = PlayerProgress.MaxEnergy;
        player.dodgeChance = PlayerProgress.Dodge;
        player.critChance = PlayerProgress.Crit;
        player.nrgGain = PlayerProgress.ExtraEnergyChance;

        // enter the fight at the health carried over from exploration (GameManager is the source of truth)
        var gm = GameManager.Instance;
        if (gm != null)
        {
            player.baseHP = gm.maxHP;
            player.currentHP = Mathf.Clamp(gm.currentHP, 1f, gm.maxHP);
        }
        else
        {
            player.currentHP = player.baseHP;   // battle scene played on its own
        }
        player.currentNRG = 0;
    }

    void Update()
    {
        if (currentState == TurnState.ACTION)
        {
            StartCoroutine(TimeForAction());
        }
    }

    public void OnTurnStart()
    {
        GainEnergy(1f);

        // random chance for a bonus energy (player only)
        if (Random.value * 100f < player.nrgGain)
        {
            GainEnergy(1f);
            Debug.Log(player.name + " gained a bonus energy!");
        }

        // Adrenaline: extra energy each turn while it lasts
        if (statuses.Has(StatusId.Adrenaline)) GainEnergy(statuses.Magnitude(StatusId.Adrenaline));

        statuses.Remove(StatusId.Guard);
        statuses.Remove(StatusId.Focus);

        foreach (PlayerMove move in new List<PlayerMove>(cooldowns.Keys))
        {
            cooldowns[move]--;
            if (cooldowns[move] <= 0) cooldowns.Remove(move);
        }

        statuses.Tick();
    }

    // energy and cooldowns
    public void GainEnergy(float amount)
    {
        player.currentNRG = Mathf.Min(player.currentNRG + amount, player.baseNRG);
    }

    public bool HasEnoughEnergy(float cost) { return player.currentNRG >= cost; }

    public void SpendEnergy(float amount)
    {
        player.currentNRG = Mathf.Max(0f, player.currentNRG - amount);
    }

    public int CooldownRemaining(PlayerMove move)
    {
        int c;
        return cooldowns.TryGetValue(move, out c) ? c : 0;
    }

    public bool CanUse(PlayerMove move)
    {
        return HasEnoughEnergy(move.energyCost) && CooldownRemaining(move) == 0;
    }

    private void BeginMove(PlayerMove move)
    {
        SpendEnergy(move.energyCost);
        if (move.cooldown > 0) cooldowns[move] = move.cooldown + 1;
    }

    // actions
    public void PerformAttack(PlayerMove move, GameObject target)
    {
        BeginMove(move);
        pendingMove = move;
        enemyToAttack = target;
        currentState = TurnState.ACTION;
    }

    public void PerformBuff(PlayerMove move)
    {
        BeginMove(move);
        Debug.Log(gameObject.name + " used " + move.moveName);

        switch (move.effect)
        {
            case MoveEffect.Aim:
                statuses.Apply(StatusId.Aim, "Aim", false, 1,
                    s => "Your next attacking move is a guaranteed critical hit.");
                break;

            case MoveEffect.Chamber:
                statuses.Apply(StatusId.Chamber, "Chambered", false, Combat.MaxChamberShells,
                    s => s + "/" + Combat.MaxChamberShells + " shells loaded. Your next attacking move deals +"
                         + Mathf.RoundToInt(s * Combat.ChamberBonusPerShell * 100f) + "% damage.");
                break;

            case MoveEffect.CoatToggle:
                if (statuses.Has(StatusId.Coated)) statuses.Remove(StatusId.Coated);
                else statuses.Apply(StatusId.Coated, "Coated", false, 1,
                    s => "Your blade is coated: attacks apply Poison instead of Bleed.");
                break;
        }

        BSM.onActionComplete();
    }

    public void UseItem(ItemData item)
    {
        if (!PlayerProgress.UseItem(item))
        {
            BSM.onActionComplete(); // none left, nothing happens
            return;
        }

        Debug.Log(gameObject.name + " used " + item.itemName);

        switch (item.effect)
        {
            case ItemEffect.Heal:
                player.currentHP = Mathf.Min(player.currentHP + item.amount, player.baseHP);
                break;

            case ItemEffect.Adrenaline:
                statuses.ApplyTimed(StatusId.Adrenaline, "Adrenaline", false, item.amount, item.duration,
                    "Gain " + Mathf.RoundToInt(item.amount) + " extra energy at the start of each turn.");
                break;
        }

        BSM.onActionComplete();
    }

    public void DoGuard()
    {
        Debug.Log(gameObject.name + " used Guard");
        statuses.Apply(StatusId.Guard, "Guard", false, 1,
            s => "Take " + Mathf.RoundToInt(guardDamageReduction * 100f) + "% less damage until your next turn.");
        BSM.onActionComplete();
    }

    public void DoFocus()
    {
        Debug.Log(gameObject.name + " used Focus");
        statuses.Apply(StatusId.Focus, "Focus", true, 1,
            s => "Take " + Mathf.RoundToInt(focusDamageIncrease * 100f) + "% more damage until your next turn.");
        GainEnergy(focusExtraEnergy);
        BSM.onActionComplete();
    }

    private IEnumerator TimeForAction()
    {
        if (actionStarted) yield break;
        actionStarted = true;

        // permanent damage boost applies to every damaging move; one crit roll covers all hits
        float damage = pendingMove.damage * (1f + PlayerProgress.damageBonus);
        AttackRoll roll = Combat.PrepareAttack(damage, player.critChance, statuses);
        int hits = Mathf.Max(1, pendingMove.hits);

        if (pendingMove.target == MoveTarget.AllEnemies)
        {
            yield return new WaitForSeconds(0.5f);

            for (int h = 0; h < hits; h++)
            {
                // copy the list, enemies remove themselves when they die
                foreach (GameObject enemyObj in new List<GameObject>(BSM.EnemyInBattle))
                    DealDamage(enemyObj, roll);

                if (h < hits - 1) yield return new WaitForSeconds(0.25f);
            }

            yield return new WaitForSeconds(0.5f);
        }
        else
        {
            Vector3 enemyPosition = new Vector3(enemyToAttack.transform.position.x - 1.5f, enemyToAttack.transform.position.y, enemyToAttack.transform.position.z);
            while (MoveTo(enemyPosition)) { yield return null; }

            yield return new WaitForSeconds(0.5f);

            for (int h = 0; h < hits; h++)
            {
                DealDamage(enemyToAttack, roll);
                if (h < hits - 1) yield return new WaitForSeconds(0.25f);
            }

            while (MoveTo(startPosition)) { yield return null; }
        }

        actionStarted = false;
        currentState = TurnState.WAITING;
        pendingMove = null;

        BSM.onActionComplete();
    }

    private void DealDamage(GameObject enemyObj, AttackRoll roll)
    {
        EnemyState target = enemyObj.GetComponent<EnemyState>();
        if (target == null || !target.IsAlive) return;

        Debug.Log(gameObject.name + " used " + pendingMove.moveName + " on " + enemyObj.name);
        bool landed = target.TakeDamage(roll.damage, roll.isCrit);

        // weapon passive (Machete): every hit that lands adds a stack
        if (landed && target.IsAlive && weapon != null && weapon.passive == WeaponPassive.BleedPoison)
        {
            if (statuses.Has(StatusId.Coated)) target.ApplyPoisonStack();
            else target.ApplyBleedStack();
        }
    }

    public void TakeDamage(float amount, bool wasCrit = false)
    {
        if (Combat.RollDodge(player.dodgeChance))
        {
            Debug.Log(player.name + " dodged the attack!");
            return;
        }

        float finalDamage = amount;
        if (statuses.Has(StatusId.Guard)) { finalDamage *= (1f - guardDamageReduction); }
        if (statuses.Has(StatusId.Focus)) { finalDamage *= (1f + focusDamageIncrease); }

        player.currentHP -= finalDamage;
        Debug.Log(player.name + (wasCrit ? " took a CRITICAL hit: " : " took ") + finalDamage + " damage. HP now: " + player.currentHP);

        if (player.currentHP <= 0)
        {
            player.currentHP = 0;
            currentState = TurnState.DEAD;
            Debug.Log(player.name + " has died.");
            BSM.PlayerInBattle.Remove(this.gameObject);
            BSM.PlayersToManage.Remove(this.gameObject);
            gameObject.SetActive(false);
        }
    }

    private bool MoveTo(Vector3 target)
    {
        return target != (transform.position = Vector3.MoveTowards(transform.position, target, animSpeed));
    }
}