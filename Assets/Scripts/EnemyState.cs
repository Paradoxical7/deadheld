using System.Collections;
using System.Collections.Generic;
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

    // IeNumerator variables
    private Vector3 startPosition;
    private bool actionStarted = false;
    public GameObject playerToAttack;
    private float animSpeed = 0.3f;

    // using the player testing moveset for now, can change later once movesets are planned/made
    public List<PlayerMove> attackMoves = new List<PlayerMove>();
    public float focusExtraEnergy = 1f;
    public float focusDamageIncrease = 0.15f;
    public float guardDamageReduction = 0.5f;

    private bool isGuarding = false;
    private bool isFocusing = false;
    private float pendingDamageMultiplier = 1f;
    private PlayerMove currentMove;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentState = TurnState.WAITING;
        BSM = GameObject.Find("BattleManager").GetComponent<BattleStateMachine>();
        startPosition = transform.position;

        // Initialize live stats from base values
        enemy.currentHP = enemy.baseHP;
        enemy.currentATK = enemy.baseATK;
        enemy.currentNRG = 0f;
    }

    // Update is called once per frame
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

    public void SpendEnergy(float amount)
    {
        enemy.currentNRG = Mathf.Max(0f, enemy.currentNRG - amount);
    }

    public void SetPendingDamageMultiplier(float multiplier)
    {
        pendingDamageMultiplier = multiplier;
    }

    // turn decision
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

        HandleTurn myTurn = new HandleTurn();
        myTurn.Attacker = this.gameObject.name;
        myTurn.Type = "Enemy";
        myTurn.AttackersGameObject = this.gameObject;

        List<PlayerMove> affordable = new List<PlayerMove>();
        foreach (PlayerMove move in attackMoves)
        {
            if (enemy.currentNRG >= move.energyCost)
                affordable.Add(move);
        }

        if (affordable.Count > 0)
        {
            PlayerMove chosen = affordable[Random.Range(0, affordable.Count)];
            currentMove = chosen;
            myTurn.Action = "Attack";
            myTurn.MoveEnergyCost = chosen.energyCost;
            myTurn.MoveDamageMultiplier = chosen.dmgMultiplier;
            myTurn.AttackersTarget = BSM.PlayerInBattle[Random.Range(0, BSM.PlayerInBattle.Count)];
        }
        else
        {
            // this is if the enemy cant afford any moves.
            // Im having them focus/guard to experiment and see if its good for the game or not
            myTurn.Action = (Random.value < 0.5f) ? "Focus" : "Guard";
            myTurn.AttackersTarget = null;
        }

        BSM.CollectActions(myTurn);
    }

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

    private IEnumerator TimeForAction()
    {
        if (actionStarted)
        {
            yield break;
        }

        actionStarted = true;

        //simple slide to player to animate attacking
        Vector3 playerPosition = new Vector3(playerToAttack.transform.position.x + 1.5f, playerToAttack.transform.position.y, playerToAttack.transform.position.z);
        while (MoveTowardsEnemy(playerPosition)){yield return null;}

        
        // wait 
        yield return new WaitForSeconds(0.5f);

        PlayerState target = playerToAttack.GetComponent<PlayerState>();
        if (target != null)
        {
            Debug.Log(gameObject.name + " used " + currentMove.moveName + " on " + playerToAttack.name);
            target.TakeDamage(enemy.currentATK+ pendingDamageMultiplier);
        }

        // slide back
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
        if (isFocusing) { finalDamage *= (1f + guardDamageReduction); }
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
