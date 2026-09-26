using UnityEngine;
using System.Collections.Generic;

public class BattleStateMachine : MonoBehaviour
{
    // Loop: Wait -> Take Action -> Perform Action -> Loop back to Wait
    public enum performAction
    {
        WAIT,
        TAKEACTION,
        PERFORMACTION
    }

    public performAction battleState;

    public List<HandleTurn> PerformList = new List<HandleTurn>();
    public List<GameObject> PlayerInBattle = new List<GameObject>();
    public List<GameObject> EnemyInBattle = new List<GameObject>();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        battleState = performAction.WAIT;
        EnemyInBattle.AddRange(GameObject.FindGameObjectsWithTag("Enemy"));
        PlayerInBattle.AddRange(GameObject.FindGameObjectsWithTag("Player"));

    }

    // Update is called once per frame
    void Update()
    {
        switch (battleState)
        {
            case (performAction.WAIT):

                break;

            case (performAction.TAKEACTION):

                break;

            case (performAction.PERFORMACTION):

                break;

        }
    }

    public void CollectActions(HandleTurn input)
    {
        PerformList.Add(input);
    }
}
