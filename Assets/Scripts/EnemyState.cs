using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

public class EnemyState : MonoBehaviour
{
    private BattleStateMachine BSM;
    public TurnBasedPlayer enemy;

    public enum TurnState { WAITING, ACTION, DEAD }
    public TurnState currentState;

    private Vector3 startPosition;
    private bool actionStarted = false;
    public GameObject playerToAttack;
    private float animSpeed = 0.3f;

    [FormerlySerializedAs("moves")]
    public List<PlayerMove> attackMoves = new List<PlayerMove>();
    public float healThreshold = 0.6f; // heal allies below this fraction of max HP

    public float focusExtraEnergy = 1f;
    public float focusDamageIncrease = 0.15f;
    public float guardDamageReduction = 0.5f;

    public StatusEffects statuses = new StatusEffects();
    private PlayerMove currentMove;

    void Start()
    {
        currentState = TurnState.WAITING;
        BSM = GameObject.Find("BattleManager").GetComponent<BattleStateMachine>();
        startPosition = transform.position;

        enemy.currentHP = enemy.baseHP;
        enemy.currentNRG = 0f;
    }

    void Update()
    {
        if (currentState == TurnState.ACTION)
        {
            StartCoroutine(TimeForAction());
        }
    }

    // energy
    public void GainEnergy(float amount)
    {
        enemy.currentNRG = Mathf.Min(enemy.currentNRG + amount, enemy.baseNRG);
    }

    public bool HasEnoughEnergy(float cost) { return enemy.currentNRG >= cost; }

    public void SpendEnergy(float amount)
    {
        enemy.currentNRG = Mathf.Max(0f, enemy.currentNRG - amount);
    }

    // healing
    GameObject FindMostHurtAlly()
    {
        GameObject best = null;
        float lowest = healThreshold;
        foreach (GameObject ally in BSM.EnemyInBattle)
        {
            if (ally == null) continue;
            EnemyState es = ally.GetComponent<EnemyState>();
            if (es == null || es.enemy.baseHP <= 0) continue;
            float pct = es.enemy.currentHP / es.enemy.baseHP;
            if (pct < lowest)
            {
                lowest = pct;
                best = ally;
            }
        }
        return best;
    }

    public void Heal(float amount)
    {
        enemy.currentHP = Mathf.Min(enemy.currentHP + amount, enemy.baseHP);
        Debug.Log(enemy.name + " healed " + amount + ". HP now: " + enemy.currentHP);
    }

    public void DoHeal(GameObject target, float amount)
    {
        StartCoroutine(HealRoutine(target, amount));
    }

    private IEnumerator HealRoutine(GameObject target, float amount)
    {
        yield return new WaitForSeconds(0.5f);
        if (target != null)
        {
            EnemyState es = target.GetComponent<EnemyState>();
            if (es != null) es.Heal(amount);
        }
        yield return new WaitForSeconds(0.3f);
        BSM.onActionComplete();
    }

    // enemy AI
    public void TakeTurn()
    {
        if (BSM.PlayerInBattle.Count == 0)
        {
            Debug.Log("All players are dead. Game over.");
            return;
        }

        GainEnergy(1f);
        statuses.Remove(StatusId.Guard);
        statuses.Remove(StatusId.Focus);

        List<PlayerMove> affordable = attackMoves.Where(m => HasEnoughEnergy(m.energyCost)).ToList();
        List<PlayerMove> heals = affordable.Where(m => m.kind == MoveKind.Heal).ToList();
        List<PlayerMove> attacks = affordable.Where(m => m.kind == MoveKind.Attack).ToList();
        GameObject healTarget = FindMostHurtAlly();

        HandleTurn myTurn = new HandleTurn();
        myTurn.Attacker = gameObject.name;
        myTurn.Type = "Enemy";
        myTurn.AttackersGameObject = gameObject;

        if (healTarget != null && heals.Count > 0)
        {
            myTurn.Action = "Heal";
            myTurn.Move = heals[Random.Range(0, heals.Count)];
            myTurn.AttackersTarget = healTarget;
        }
        else if (attacks.Count > 0)
        {
            myTurn.Action = "Attack";
            myTurn.Move = attacks[Random.Range(0, attacks.Count)];
            myTurn.AttackersTarget = BSM.PlayerInBattle[Random.Range(0, BSM.PlayerInBattle.Count)];
        }
        else
        {
            myTurn.Action = (Random.value < 0.5f) ? "Focus" : "Guard";
            myTurn.AttackersTarget = null;
        }

        BSM.CollectActions(myTurn);
    }

    // ---- Actions (called by BSM) ----
    public void PerformAttack(PlayerMove move, GameObject target)
    {
        SpendEnergy(move.energyCost);
        currentMove = move;
        playerToAttack = target;
        currentState = TurnState.ACTION;
    }

    public void DoGuard()
    {
        Debug.Log(gameObject.name + " used Guard");
        statuses.Apply(StatusId.Guard, "Guard", false, 1,
            s => "Take " + Mathf.RoundToInt(guardDamageReduction * 100f) + "% less damage until its next turn.");
        BSM.onActionComplete();
    }

    public void DoFocus()
    {
        Debug.Log(gameObject.name + " used Focus");
        statuses.Apply(StatusId.Focus, "Focus", true, 1,
            s => "Take " + Mathf.RoundToInt(focusDamageIncrease * 100f) + "% more damage until its next turn.");
        GainEnergy(focusExtraEnergy);
        BSM.onActionComplete();
    }

    private IEnumerator TimeForAction()
    {
        if (actionStarted) yield break;
        actionStarted = true;

        AttackRoll roll = Combat.PrepareAttack(currentMove.damage, enemy.critChance, statuses);

        Vector3 playerPosition = new Vector3(playerToAttack.transform.position.x + 1.5f, playerToAttack.transform.position.y, playerToAttack.transform.position.z);
        while (MoveTo(playerPosition)) { yield return null; }

        yield return new WaitForSeconds(0.5f);

        PlayerState target = playerToAttack.GetComponent<PlayerState>();
        if (target != null)
        {
            Debug.Log(gameObject.name + " used " + currentMove.moveName + " on " + playerToAttack.name);
            target.TakeDamage(roll.damage, roll.isCrit);
        }

        while (MoveTo(startPosition)) { yield return null; }

        actionStarted = false;
        currentState = TurnState.WAITING;
        BSM.onActionComplete();
    }

    public void TakeDamage(float amount, bool wasCrit = false)
    {
        if (Combat.RollDodge(enemy.dodgeChance))
        {
            Debug.Log(enemy.name + " dodged the attack!");
            return;
        }

        float finalDamage = amount;
        if (statuses.Has(StatusId.Guard)) { finalDamage *= (1f - guardDamageReduction); }
        if (statuses.Has(StatusId.Focus)) { finalDamage *= (1f + focusDamageIncrease); }

        enemy.currentHP -= finalDamage;
        Debug.Log(enemy.name + (wasCrit ? " took a CRITICAL hit: " : " took ") + finalDamage + " damage. HP now: " + enemy.currentHP);

        if (enemy.currentHP <= 0)
        {
            enemy.currentHP = 0;
            currentState = TurnState.DEAD;
            Debug.Log(enemy.name + " has died.");
            BSM.EnemyInBattle.Remove(this.gameObject);
            gameObject.SetActive(false);
        }
    }

    private bool MoveTo(Vector3 target)
    {
        return target != (transform.position = Vector3.MoveTowards(transform.position, target, animSpeed));
    }
}