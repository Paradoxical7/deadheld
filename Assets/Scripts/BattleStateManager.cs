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
                if (PerformList.Count > 0)
                {
                    battleState = performAction.TAKEACTION;
                }

                break;

            case (performAction.TAKEACTION):
                GameObject performer = GameObject.Find(PerformList[0].Attacker);

                if (PerformList[0].Type == "Enemy")
                {
                    EnemyState ES = performer.GetComponent<EnemyState>();
                    ES.playerToAttack = PerformList[0].AttackersTarget;
                    ES.currentState = EnemyState.TurnState.ACTION;
                }

                if (PerformList[0].Type == "Player")
                {

                }
                battleState = performAction.PERFORMACTION;

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
