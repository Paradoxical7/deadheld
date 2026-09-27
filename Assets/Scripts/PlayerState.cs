using UnityEngine;

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
    private float maxCooldown = 2f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
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
}
