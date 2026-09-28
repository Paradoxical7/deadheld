using UnityEngine;
using System.Collections;

public class PlayerState : MonoBehaviour
{
    public TurnBasedPlayer player;
    private BattleStateMachine BSM;

    public enum TurnState
    {
        PROCESSING,
        ADDTOLIST,
        WAITING,
        SELECT,
        ACTION,
        DEAD
    }

    public TurnState currentState;

    private float currentCooldown = 0f;
    private float maxCooldown = 1f;
    public GameObject selector;

    // IeNumerator variables
    public GameObject enemyToAttack;
    private bool actionStarted = false;
    private Vector3 startPosition;
    private float animSpeed = 0.3f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        startPosition = transform.position;
        selector.SetActive(false);
        BSM = GameObject.Find("BattleManager").GetComponent<BattleStateMachine>();
        currentState = TurnState.PROCESSING;
        
    }

    // Update is called once per frame
    void Update()
    {
        // Debug.Log(currentState);
        switch(currentState)
        {
            case (TurnState.PROCESSING):
                UpdateState();
                break;

            case (TurnState.ADDTOLIST):
                BSM.PlayersToManage.Add(this.gameObject);
                currentState = TurnState.WAITING;
                break;

            case (TurnState.WAITING):
                //idle
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
            currentState = TurnState.ADDTOLIST;
        }
    }

    private IEnumerator TimeForAction()
    {
        if (actionStarted)
        {
            yield break;
        }

        actionStarted = true;

        //simple slide to player to animate attacking
        Vector3 enemyPosition = new Vector3(enemyToAttack.transform.position.x - 1.5f, enemyToAttack.transform.position.y, enemyToAttack.transform.position.z);
        while (MoveTowardsEnemy(enemyPosition)) { yield return null;}


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
