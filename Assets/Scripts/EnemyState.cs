using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

    // Moveset, filled in per enemy/prefab in the Inspector
    public List<PlayerMove> moves = new List<PlayerMove>();
    public float healThreshold = 0.6f; // heal allies below this fraction of max HP
    private float pendingDamageMultiplier = 1f;

    // IEnumerator variables
    private Vector3 startPosition;
    private bool actionStarted = false;
    public GameObject playerToAttack;
    private float animSpeed = 0.3f;

    void Start()
    {
        currentState = TurnState.WAITING;
        BSM = GameObject.Find("BattleManager").GetComponent<BattleStateMachine>();
        startPosition = transform.position;

        // Initialize live stats from base values
        enemy.currentHP = enemy.baseHP;
        enemy.currentATK = enemy.baseATK;
        enemy.currentNRG = 0; // starts empty like players
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
        return best; // null if nobody is hurt enough
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

        List<PlayerMove> affordable = moves.Where(m => HasEnoughEnergy(m.energyCost)).ToList();
        List<PlayerMove> heals = affordable.Where(m => m.kind == MoveKind.Heal).ToList();
        List<PlayerMove> attacks = affordable.Where(m => m.kind == MoveKind.Attack).ToList();
        GameObject healTarget = FindMostHurtAlly();

        HandleTurn turn = new HandleTurn();
        turn.Attacker = gameObject.name;
        turn.Type = "Enemy";
        turn.AttackersGameObject = gameObject;

        if (healTarget != null && heals.Count > 0)
        {
            PlayerMove heal = heals[Random.Range(0, heals.Count)];
            turn.Action = "Heal";
            turn.AttackersTarget = healTarget;
            turn.MoveEnergyCost = heal.energyCost;
            turn.MoveHealAmount = heal.healAmount;
        }
        else if (attacks.Count > 0)
        {
            PlayerMove atk = attacks[Random.Range(0, attacks.Count)];
            turn.Action = "Attack";
            turn.AttackersTarget = BSM.PlayerInBattle[Random.Range(0, BSM.PlayerInBattle.Count)];
            turn.MoveEnergyCost = atk.energyCost;
            turn.MoveDamageMultiplier = atk.dmgMultiplier;
        }
        else
        {
            Debug.Log(enemy.name + " can't afford a move and skips.");
            BSM.onActionComplete();
            return;
        }

        BSM.CollectActions(turn);
    }

    // ---- Attack animation + damage ----
    private IEnumerator TimeForAction()
    {
        if (actionStarted)
        {
            yield break;
        }

        actionStarted = true;

        // simple slide to player to animate attacking
        Vector3 playerPosition = new Vector3(playerToAttack.transform.position.x + 1.5f, playerToAttack.transform.position.y, playerToAttack.transform.position.z);
        while (MoveTowardsEnemy(playerPosition)) { yield return null; }

        // wait
        yield return new WaitForSeconds(0.5f);

        // do dmg
        PlayerState target = playerToAttack.GetComponent<PlayerState>();
        if (target != null)
        {
            target.TakeDamage(enemy.currentATK * pendingDamageMultiplier);
        }

        // slide back
        Vector3 firstPosition = startPosition;
        while (MoveTowardsStart(firstPosition)) { yield return null; }

        actionStarted = false;
        currentState = TurnState.WAITING;

        BSM.onActionComplete();
    }

    public void TakeDamage(float amount)
    {
        enemy.currentHP -= amount;
        Debug.Log(enemy.name + " took " + amount + " damage. HP now: " + enemy.currentHP);

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