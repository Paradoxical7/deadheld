using System.Collections;
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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentState = TurnState.WAITING;
        BSM = GameObject.Find("BattleManager").GetComponent<BattleStateMachine>();
        startPosition = transform.position;

        // Initialize live stats from base values
        enemy.currentHP = enemy.baseHP;
        enemy.currentATK = enemy.baseATK;
        enemy.currentNRG = enemy.baseNRG;
    }

    // Update is called once per frame
    void Update()
    {
        if (currentState == TurnState.ACTION)
        {
            StartCoroutine(TimeForAction());
        }
    }

    public void TakeTurn()
    {
        if (BSM.PlayerInBattle.Count == 0)
        {
            Debug.Log("All players are dead. Game over.");
            return;
        }

        HandleTurn myAttack = new HandleTurn();
        myAttack.Attacker = this.gameObject.name;
        myAttack.Type = "Enemy";
        myAttack.AttackersGameObject = this.gameObject;
        myAttack.AttackersTarget = BSM.PlayerInBattle[Random.Range(0, BSM.PlayerInBattle.Count)];
        BSM.CollectActions(myAttack);
    }

    private IEnumerator TimeForAction()
    {
        if (actionStarted)
        {
            yield break;
        }

        actionStarted = true;

        //simple slide to player to animate attacking
        Vector3 playerPosition = new Vector3(playerToAttack.transform.position.x+1.5f, playerToAttack.transform.position.y, playerToAttack.transform.position.z);
        while (MoveTowardsEnemy(playerPosition)){yield return null;}

        
        // wait 
        yield return new WaitForSeconds(0.5f);

        // do dmg
        PlayerState target = playerToAttack.GetComponent<PlayerState>();
        if (target != null)
        {
            target.TakeDamage(enemy.currentATK);
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
