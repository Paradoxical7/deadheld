using UnityEngine;
using System.Collections;

public class PlayerState : MonoBehaviour
{
    public TurnBasedPlayer player;
    private BattleStateMachine BSM;

    public enum TurnState
    {
        WAITING,
        ACTION,
        DEAD
    }
    public TurnState currentState;

    public GameObject selector;
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
        currentState = TurnState.WAITING;
        
    }

    // Update is called once per frame
    void Update()
    {
        if (currentState == TurnState.ACTION)
        {
            StartCoroutine(TimeForAction());
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

        actionStarted = false;
        currentState = TurnState.WAITING;

        BSM.onActionComplete();
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
