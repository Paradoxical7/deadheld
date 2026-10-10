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

        // nothing affordable (or no moves set): skip the turn, but still advance the battle
        if (affordable.Count == 0)
        {
            Debug.Log(enemy.name + " can't afford a move and skips.");
            BSM.onActionComplete();
            return;
        }

        PlayerMove move = affordable[Random.Range(0, affordable.Count)];

        HandleTurn myAttack = new HandleTurn();
        myAttack.Attacker = gameObject.name;
        myAttack.Type = "Enemy";
        myAttack.Action = "Attack";
        myAttack.AttackersGameObject = gameObject;
        myAttack.AttackersTarget = BSM.PlayerInBattle[Random.Range(0, BSM.PlayerInBattle.Count)];
        myAttack.MoveEnergyCost = move.energyCost;
        myAttack.MoveDamageMultiplier = move.dmgMultiplier;
        BSM.CollectActions(myAttack);
    }

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