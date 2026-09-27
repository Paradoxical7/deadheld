using UnityEngine;
using System.Collections.Generic;
using UnityEditor.Networking.PlayerConnection;
using UnityEngine.UI;
using TMPro;
using System.Linq;

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

    public enum PlayerGUI
    {
        ACTIVATE,
        WAITING,
        INPUT1, // basic attack
        INPUT2, // select enemy
        DONE
    }

    public PlayerGUI playerInput;
    public List<GameObject> PlayersToManage = new List<GameObject>();
    private HandleTurn playerChoice;
    public GameObject enemyButton;
    public Transform Spacer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        battleState = performAction.WAIT;
        // EnemyInBattle = EnemyInBattle.OrderBy(e => e.transform.position.x).ToList(); I wanted to make the buttons appear in a set order but ill implement later
        EnemyInBattle.AddRange(GameObject.FindGameObjectsWithTag("Enemy"));
        
        PlayerInBattle.AddRange(GameObject.FindGameObjectsWithTag("Player"));

        EnemyButtons();

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

    void EnemyButtons()
    {
        foreach (GameObject enemy in EnemyInBattle)
        {
            GameObject newButton = Instantiate(enemyButton) as GameObject;
            EnemySelectButton button = newButton.GetComponent<EnemySelectButton>();

            EnemyState currentEnemy = enemy.GetComponent<EnemyState>();

            TMP_Text buttonText = newButton.transform.Find("Text (TMP)").gameObject.GetComponent<TMP_Text>();
            buttonText.text = currentEnemy.enemy.name;

            button.EnemyPrefab = enemy;

            newButton.transform.SetParent(Spacer, false);

        }
    }
}
