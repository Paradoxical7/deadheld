using UnityEngine;
using System.Collections.Generic;
using UnityEditor.Networking.PlayerConnection;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using Unity.VisualScripting;

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

    public enum BattlePhase
    {
        PLAYER_PHASE,
        ENEMY_PHASE
    }
    public BattlePhase currentPhase;
    private int currentEnemyIndex = 0;

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
    public GameObject attackPanel;
    public GameObject enemySelectPanel;
    public GameObject moveSelectPanel;
    public List<Button> attackMoveButtons;
    public List<TMP_Text> attackMoveLabels;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        battleState = performAction.WAIT;
        EnemyInBattle.AddRange(GameObject.FindGameObjectsWithTag("Enemy"));
        PlayerInBattle.AddRange(GameObject.FindGameObjectsWithTag("Player"));

        attackPanel.SetActive(false);
        enemySelectPanel.SetActive(false);
        moveSelectPanel.SetActive(false);

        EnemyButtons();
        StartPlayerPhase();
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
                battleState = performAction.PERFORMACTION;

                //if (PerformList.Count == 0) break;

                GameObject performer = GameObject.Find(PerformList[0].Attacker);

                if (PerformList[0].Type == "Enemy")
                {
                    EnemyState ES = performer.GetComponent<EnemyState>();

                    switch (PerformList[0].Action)
                    {
                        case "Attack":
                            ES.SpendEnergy(PerformList[0].MoveEnergyCost);
                            ES.SetPendingDamageMultiplier(PerformList[0].MoveDamageMultiplier);
                            ES.playerToAttack = PerformList[0].AttackersTarget;
                            ES.currentState = EnemyState.TurnState.ACTION;
                            break;

                        case "Guard":
                            ES.DoGuard();
                            break;

                        case "Focus":
                            ES.DoFocus();
                            break;
                    }
                }

                if (PerformList[0].Type == "Player")
                {
                    // Debug.Log("Player is performing");
                    PlayerState PS = performer.GetComponent<PlayerState>();

                    switch (PerformList[0].Action)
                    {
                        case "Attack":
                            PS.SpendEnergy(PerformList[0].MoveEnergyCost);
                            PS.SetPendingDamageMultiplier(PerformList[0].MoveDamageMultiplier);
                            PS.enemyToAttack = PerformList[0].AttackersTarget;
                            PS.currentState = PlayerState.TurnState.ACTION;
                            break;

                        case "Guard":
                            PS.DoGuard();
                            break;

                        case "Focus":
                            PS.DoFocus();
                            break;
                    }
                }
                break;

            case (performAction.PERFORMACTION):
                //idle

                break;
        }

        switch (playerInput)
        {
            case (PlayerGUI.ACTIVATE):
                if (PlayersToManage.Count > 0)
                {
                    PlayerState PS = PlayersToManage[0].GetComponent<PlayerState>();
                    PS.OnTurnStart();
                    PlayersToManage[0].transform.Find("Selector").gameObject.SetActive(true);
                    playerChoice = new HandleTurn();
                    attackPanel.SetActive(true);
                    playerInput = PlayerGUI.WAITING;
                }
                
              
                break;

            case (PlayerGUI.WAITING):
                //idle
                break;

            case (PlayerGUI.DONE):
                playerInputDone();
                break;
        }
    }

    void StartPlayerPhase()
    {
        currentPhase = BattlePhase.PLAYER_PHASE;
        PlayersToManage.Clear();
        PlayersToManage.AddRange(PlayerInBattle);
        playerInput = PlayerGUI.ACTIVATE;
    }

    void StartEnemyPhase()
    {
        currentPhase = BattlePhase.ENEMY_PHASE;
        currentEnemyIndex = 0;
        TakeNextEnemyTurn();
    }

    void TakeNextEnemyTurn()
    {
        if (currentEnemyIndex >= EnemyInBattle.Count)
        {
            StartPlayerPhase();
            return;
        }

        EnemyState ES = EnemyInBattle[currentEnemyIndex].GetComponent<EnemyState>();
        ES.TakeTurn();
    }

    public void onActionComplete()
    {
        if (PerformList.Count > 0)
        {
            PerformList.RemoveAt(0);
        }
        battleState = performAction.WAIT;

        if (currentPhase == BattlePhase.ENEMY_PHASE)
        {
            currentEnemyIndex++;
            TakeNextEnemyTurn();
        }
        else if (currentPhase == BattlePhase.PLAYER_PHASE && PlayersToManage.Count == 0 && PerformList.Count == 0)
        {
            StartEnemyPhase();
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

    public void OpenAttackMenu()
    {
        attackPanel.SetActive(false);
        moveSelectPanel.SetActive(true);

        PlayerState PS = PlayersToManage[0].GetComponent<PlayerState>();

        for (int i = 0; i < attackMoveButtons.Count; i++)
        {
            if (i >= PS.attackMoves.Count)
            {
                attackMoveButtons[i].gameObject.SetActive(false); // hide unused slots
                continue;
            }

            PlayerMove move = PS.attackMoves[i];
            attackMoveButtons[i].gameObject.SetActive(true);
            attackMoveButtons[i].interactable = PS.HasEnoughEnergy(move.energyCost);
            attackMoveLabels[i].text = move.moveName + "\n(" + move.energyCost + " NRG)";
        }
    }

    public void InputAttack(int moveIndex) // attack button
    {
        PlayerState PS = PlayersToManage[0].GetComponent<PlayerState>();
        PlayerMove move = PS.attackMoves[moveIndex];

        if (!PS.HasEnoughEnergy(move.energyCost))
        {
            Debug.Log("Not enough energy for " + move.moveName);
            return;
        }

        playerChoice.Attacker = PlayersToManage[0].name;
        playerChoice.AttackersGameObject = PlayersToManage[0];
        playerChoice.Type = "Player";
        playerChoice.Action = "Attack";
        playerChoice.MoveEnergyCost = move.energyCost;
        playerChoice.MoveDamageMultiplier = move.dmgMultiplier;

        attackPanel.SetActive(false);
        moveSelectPanel.SetActive(false);
        enemySelectPanel.SetActive(true);
    }

    public void InputGuard()
    {
        playerChoice.Attacker = PlayersToManage[0].name;
        playerChoice.AttackersGameObject = PlayersToManage[0];
        playerChoice.Type = "Player";
        playerChoice.Action = "Guard";
        playerChoice.AttackersTarget = null;

        attackPanel.SetActive(false);
        playerInput = PlayerGUI.DONE;
    }

    public void InputFocus()
    {
        playerChoice.Attacker = PlayersToManage[0].name;
        playerChoice.AttackersGameObject = PlayersToManage[0];
        playerChoice.Type = "Player";
        playerChoice.Action = "Focus";
        playerChoice.AttackersTarget = null;

        attackPanel.SetActive(false);
        playerInput = PlayerGUI.DONE;
    }

    public void Input2(GameObject chosenEnemy) // enemy selection
    {
        playerChoice.AttackersTarget = chosenEnemy;
        playerInput = PlayerGUI.DONE;
    }

    void playerInputDone()
    {
        PerformList.Add(playerChoice);
        enemySelectPanel.SetActive(false);
        PlayersToManage[0].transform.Find("Selector").gameObject.SetActive(false);
        PlayersToManage.RemoveAt(0);
        playerInput = PlayerGUI.ACTIVATE;
    }
}
