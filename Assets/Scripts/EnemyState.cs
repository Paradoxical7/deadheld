using System.Collections;
using UnityEngine;

public class EnemyState : MonoBehaviour
{
    private BattleStateMachine BSM;
    public TurnBasedPlayer enemy;

    public enum TurnState
    {
        PROCESSING,
        CHOOSEACTION,
        WAITING,
        ACTION,
        DEAD
    }

    public TurnState currentState;

    private float currentCooldown = 0f;
    private float maxCooldown = 10f;

    // IeNumerator variables
    private Vector3 startPosition;
    private bool actionStarted = false;
    public GameObject playerToAttack;
    private float animSpeed = 0.3f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentState = TurnState.PROCESSING;
        BSM = GameObject.Find("BattleManager").GetComponent<BattleStateMachine>();
        startPosition = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        // Debug.Log(currentState);
        switch (currentState)
        {
            case (TurnState.PROCESSING):
                UpdateState();
                break;

            case (TurnState.CHOOSEACTION):
                ChooseAction();
                currentState = TurnState.WAITING;
                break;

            case (TurnState.WAITING):
                // idle state
                break;

            case (TurnState.ACTION):
                StartCoroutine(TimeForAction());
                break;

            case (TurnState.DEAD):
                break;
        }
    }

    void UpdateState()
    {
        currentCooldown = currentCooldown + Time.deltaTime;

        if (currentCooldown >= maxCooldown)
        {
            currentState = TurnState.CHOOSEACTION;
        }
    }

    void ChooseAction()
    {
        HandleTurn myAttack = new HandleTurn();
        myAttack.Attacker = enemy.name;
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

        // slide back
        Vector3 firstPosition = startPosition;
        while (MoveTowardsStart(firstPosition)) { yield return null; }


        // remove the performer from the list in BSM
        BSM.PerformList.RemoveAt(0);

        // reset BSM -> wait
        BSM.battleState = BattleStateMachine.performAction.WAIT;
        // end of coroutine

        actionStarted = false;

        //resets enemy state
        currentCooldown = 0f;
        currentState = TurnState.PROCESSING;
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
