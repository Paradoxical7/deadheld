using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

public class EnemyState : MonoBehaviour
{
    private BattleStateMachine BSM;
    public TurnBasedPlayer enemy;

    public enum TurnState
    {
        WAITING,
        ACTION,
        DEAD
    }
    public TurnState currentState;

    // IEnumerator variables
    private Vector3 startPosition;
    private bool actionStarted = false;
    public GameObject playerToAttack;
    private float animSpeed = 0.3f;

    // moveset, set per enemy/prefab in the Inspector
    [FormerlySerializedAs("moves")]
    public List<PlayerMove> attackMoves = new List<PlayerMove>();
    public float healThreshold = 0.6f; // heal allies below this fraction of max HP

    public float focusExtraEnergy = 1f;
    public float focusDamageIncrease = 0.15f;
    public float guardDamageReduction = 0.5f;

    private bool isGuarding = false;
    private bool isFocusing = false;
    private float pendingDamageMultiplier = 1f;
    private PlayerMove currentMove;

    void Start()
    {
        currentState = TurnState.WAITING;
        BSM = GameObject.Find("BattleManager").GetComponent<BattleStateMachine>();
        startPosition = transform.position;

        enemy.currentHP = enemy.baseHP;
        enemy.currentATK = enemy.baseATK;
        enemy.currentNRG = 0f;
    }

    void Update()
    {
        if (currentState == TurnState.ACTION)
        {
            StartCoroutine(TimeForAction());
        }
    }

    // ---- Energy ----
    public void GainEnergy(float amount)
    {
        enemy.currentNRG = Mathf.Min(enemy.currentNRG + amount, enemy.baseNRG);
    }

    public bool HasEnoughEnergy(float cost)
    {
        return enemy.currentNRG >= cost;
    }

    public void SpendEnergy(float amount)
    {
        enemy.currentNRG = Mathf.Max(0f, enemy.currentNRG - amount);
    }

    public void SetPendingDamageMultiplier(float multiplier)
    {
        pendingDamageMultiplier = multiplier;
    }

    // ---- Healing ----
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

    // ---- AI ----
    public void TakeTurn()
    {
        if (BSM.PlayerInBattle.Count == 0)
        {
            Debug.Log("All players are dead. Game over.");
            return;
        }

        GainEnergy(1f);
        isGuarding = false;
        isFocusing = false;

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
            PlayerMove heal = heals[Random.Range(0, heals.Count)];
            currentMove = heal;
            myTurn.Action = "Heal";
            myTurn.AttackersTarget = healTarget;
            myTurn.MoveEnergyCost = heal.energyCost;
            myTurn.MoveHealAmount = heal.healAmount;
        }
        else if (attacks.Count > 0)
        {
            PlayerMove atk = attacks[Random.Range(0, attacks.Count)];
            currentMove = atk;
            myTurn.Action = "Attack";
            myTurn.AttackersTarget = BSM.PlayerInBattle[Random.Range(0, BSM.PlayerInBattle.Count)];
            myTurn.MoveEnergyCost = atk.energyCost;
            myTurn.MoveDamageMultiplier = atk.dmgMultiplier;
        }
        else
        {
            // can't afford any move: Guard or Focus (kept from main)
            myTurn.Action = (Random.value < 0.5f) ? "Focus" : "Guard";
            myTurn.AttackersTarget = null;
        }

        BSM.CollectActions(myTurn);
    }

    // ---- Guard / Focus (from main) ----
    public void DoGuard()
    {
        Debug.Log(gameObject.name + " used Guard");
        isGuarding = true;
        BSM.onActionComplete();
    }

    public void DoFocus()
    {
        Debug.Log(gameObject.name + " used Focus");
        isFocusing = true;
        GainEnergy(focusExtraEnergy);
        BSM.onActionComplete();
    }

    // ---- Attack animation + damage ----
    private IEnumerator TimeForAction()
    {
        if (actionStarted)
        {
            yield break;
        }

        actionStarted = true;

        Vector3 playerPosition = new Vector3(playerToAttack.transform.position.x + 1.5f, playerToAttack.transform.position.y, playerToAttack.transform.position.z);
        while (MoveTowardsEnemy(playerPosition)) { yield return null; }

        yield return new WaitForSeconds(0.5f);

        PlayerState target = playerToAttack.GetComponent<PlayerState>();
        if (target != null)
        {
            Debug.Log(gameObject.name + " used " + (currentMove != null ? currentMove.moveName : "an attack") + " on " + playerToAttack.name);
            target.TakeDamage(enemy.currentATK * pendingDamageMultiplier);
        }

        Vector3 firstPosition = startPosition;
        while (MoveTowardsStart(firstPosition)) { yield return null; }

        actionStarted = false;
        currentState = TurnState.WAITING;
        pendingDamageMultiplier = 1f;
        BSM.onActionComplete();
    }

    public void TakeDamage(float amount)
    {
        float finalDamage = amount;
        if (isGuarding) { finalDamage *= (1f - guardDamageReduction); }
        if (isFocusing) { finalDamage *= (1f + focusDamageIncrease); }

        enemy.currentHP -= finalDamage;
        Debug.Log(enemy.name + " took " + finalDamage + " damage. HP now: " + enemy.currentHP);

        if (enemy.currentHP <= 0)
        {
            enemy.currentHP = 0;
            currentState = TurnState.DEAD;
            Debug.Log(enemy.name + " has died.");
            BSM.EnemyInBattle.Remove(this.gameObject);
            gameObject.SetActive(false);
        }
    }

    private bool MoveTowardsEnemy(Vector3 target)
    {
        return target != (transform.position = Vector3.MoveTowards(transform.position, target, animSpeed));
    }

    private bool MoveTowardsStart(Vector3 target)
    {
        return target != (transform.position = Vector3.MoveTowards(transform.position, target, animSpeed));
    }
}