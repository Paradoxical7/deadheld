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
    public float protectThreshold = 0.4f;      // how often the Guard protects when it can
    [HideInInspector] public GameObject protector; // set on the enemy being covered
    private GameObject protectedAlly;       // set on the Guard
    private int stageLevel = 0; // 0 = Stage1, 1 = Stage2, 2 = Stage3

    public float focusExtraEnergy = 1f;
    public float focusDamageIncrease = 0.15f;
    public float guardDamageReduction = 0.5f;

    public StatusEffects statuses = new StatusEffects();
    private PlayerMove currentMove;

    public bool justSummoned; // set by BSM, makes a new arrival skip its first turn

    void Start()
    {
        currentState = TurnState.WAITING;
        BSM = GameObject.Find("BattleManager").GetComponent<BattleStateMachine>();
        startPosition = transform.position;

        // EnemyIdentity is on the child (EnemyObject), so search children
        EnemyIdentity id = GetComponentInChildren<EnemyIdentity>();
        if (id != null)
        {
            // feel free to tune level buffs
            stageLevel = (int)id.stage;
            enemy.baseNRG += stageLevel * 1f; // +1 max energy per stage
            enemy.baseATK += stageLevel * 2f; // +2 ATK per stage 
        }
        Debug.Log(gameObject.name + " stage level: " + (id != null ? stageLevel.ToString() : "no EnemyIdentity found"));

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

    // guard protection
    GameObject FindAllyToProtect()
    {
        GameObject best = null;
        float lowest = protectThreshold;
        foreach (GameObject ally in BSM.EnemyInBattle)
        {
            if (ally == null || ally == gameObject) continue;
            EnemyState es = ally.GetComponent<EnemyState>();
            if (es == null || es.enemy.baseHP <= 0) continue;
            float pct = es.enemy.currentHP / es.enemy.baseHP;
            if (pct < lowest) { lowest = pct; best = ally; }
        }
        return best; // weakest ally under the threshold, or null
    }

    public void ClearProtection()
    {
        if (protectedAlly != null)
        {
            EnemyState es = protectedAlly.GetComponent<EnemyState>();
            if (es != null && es.protector == gameObject) es.protector = null;
        }
        protectedAlly = null;
    }

    public void DoProtect(GameObject ally)
    {
        ClearProtection();
        if (ally != null)
        {
            EnemyState es = ally.GetComponent<EnemyState>();
            if (es != null)
            {
                es.protector = gameObject;
                protectedAlly = ally;
                Debug.Log(gameObject.name + " is protecting " + ally.name);
            }
        }
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

        if (justSummoned)
        {
            justSummoned = false;
            Debug.Log(gameObject.name + " just arrived and sits this turn out");
            BSM.onActionComplete();
            return;
        }

        GainEnergy(1f);
        statuses.Remove(StatusId.Guard);
        statuses.Remove(StatusId.Focus);

        ClearProtection();

        List<PlayerMove> affordable = attackMoves.Where(m => HasEnoughEnergy(m.energyCost)).ToList();
        List<PlayerMove> summons = affordable.Where(m => m.kind == MoveKind.Summon && m.summonPrefab != null).ToList();
        List<PlayerMove> heals = affordable.Where(m => m.kind == MoveKind.Heal).ToList();
        List<PlayerMove> attacks = affordable.Where(m => m.kind == MoveKind.Attack).ToList();
        GameObject healTarget = FindMostHurtAlly();
        List<PlayerMove> protects = affordable.Where(m => m.kind == MoveKind.Protect).ToList();
        GameObject protectTarget = FindAllyToProtect();

        // when a fellow enemy falls into protect threshold, filter out attacks for this turn that
        // would make 'protect' not affordable the next turn (only use free attacks for right now)
        if (protectTarget != null && protects.Count == 0 && attackMoves.Any(m => m.kind == MoveKind.Protect))
            attacks = attacks.Where(m => m.energyCost == 0).ToList();

        // saving up: if there's a summon we can't afford yet (and room for it),
        // don't burn energy on paid attacks
        PlayerMove savingFor = attackMoves.FirstOrDefault(m =>
            m.kind == MoveKind.Summon && m.summonPrefab != null
            && !HasEnoughEnergy(m.energyCost)
            && m.energyCost <= enemy.baseNRG
            && BSM.HasFreeEnemySlot());

        if (savingFor != null)
            attacks = attacks.Where(m => m.energyCost <= 0f).ToList();

        HandleTurn myTurn = new HandleTurn();
        myTurn.Attacker = gameObject.name;
        myTurn.Type = "Enemy";
        myTurn.AttackersGameObject = gameObject;

        if (summons.Count > 0 && BSM.HasFreeEnemySlot())
        {
            myTurn.Action = "Summon";
            myTurn.Move = summons[Random.Range(0, summons.Count)];
            myTurn.AttackersTarget = null;
        }
        else if (healTarget != null && heals.Count > 0)
        {
            myTurn.Action = "Heal";
            myTurn.Move = heals[Random.Range(0, heals.Count)];
            myTurn.AttackersTarget = healTarget;
        }
        else if (protects.Count > 0 && protectTarget != null)
        {
            PlayerMove prot = protects[Random.Range(0, protects.Count)];
            currentMove = prot;
            myTurn.Action = "Protect";
            myTurn.AttackersTarget = protectTarget;
            myTurn.MoveEnergyCost = prot.energyCost;
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

        Debug.Log(gameObject.name + " | energy=" + enemy.currentNRG + "/" + enemy.baseNRG
            + " | chose " + myTurn.Action + (myTurn.Move != null ? "/" + myTurn.Move.moveName : ""));

        BSM.CollectActions(myTurn);
    }

    // actions
    public void PerformAttack(PlayerMove move, GameObject target)
    {
        SpendEnergy(move.energyCost);
        currentMove = move;
        playerToAttack = target;
        currentState = TurnState.ACTION;
    }

    public void PerformSummon(PlayerMove move)
    {
        SpendEnergy(move.energyCost);
        StartCoroutine(SummonRoutine(move));
    }

    private IEnumerator SummonRoutine(PlayerMove move)
    {
        Debug.Log(gameObject.name + " used " + move.moveName);
        yield return new WaitForSeconds(0.5f);

        GameObject spawned = BSM.SummonEnemy(move.summonPrefab);
        Debug.Log("Summon result: " + (spawned != null ? spawned.name : "NOTHING SPAWNED"));

        yield return new WaitForSeconds(0.5f);
        BSM.onActionComplete();
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

            BSM.OnEnemyDied(gameObject);
            ClearProtection();

            gameObject.SetActive(false);
        }
    }

    private bool MoveTo(Vector3 target)
    {
        return target != (transform.position = Vector3.MoveTowards(transform.position, target, animSpeed));
    }
}