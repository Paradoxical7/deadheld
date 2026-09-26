using UnityEngine;

public class PlayerState : MonoBehaviour
{
    public TurnBasedPlayer player;

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
    private float maxCooldown = 5f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
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
                break;

            case (TurnState.WAITING):
                break;

            case (TurnState.SELECT):
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
