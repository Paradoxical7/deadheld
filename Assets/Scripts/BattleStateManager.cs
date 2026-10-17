using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleStateMachine : MonoBehaviour
{
    // Loop: Wait -> Take Action -> Perform Action -> Loop back to Wait
    public enum performAction { WAIT, TAKEACTION, PERFORMACTION }
    public performAction battleState;

    public enum BattlePhase { PLAYER_PHASE, ENEMY_PHASE }
    public BattlePhase currentPhase;
    private int currentEnemyIndex = 0;

    public List<HandleTurn> PerformList = new List<HandleTurn>();
    public List<GameObject> PlayerInBattle = new List<GameObject>();
    public List<GameObject> EnemyInBattle = new List<GameObject>();

    public enum PlayerGUI { ACTIVATE, WAITING, INPUT1, INPUT2, DONE }
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
    public BuffBar buffBar;

    public bool randomizeEnemyCount = true;
    public int minEnemies = 1;
    public int maxEnemies = 4;

    void Start()
    {
        battleState = performAction.WAIT;

        EnemyInBattle.AddRange(GameObject.FindGameObjectsWithTag("Enemy"));
        EnemyInBattle = EnemyInBattle.OrderBy(e => TrailingNumber(e.name)).ThenBy(e => e.name).ToList();
        PlayerInBattle.AddRange(GameObject.FindGameObjectsWithTag("Player"));

        if (randomizeEnemyCount) RandomizeEnemyCount(); 

        attackPanel.SetActive(false);
        enemySelectPanel.SetActive(false);
        moveSelectPanel.SetActive(false);

        EnemyButtons();
        StartPlayerPhase();
    }

    static int TrailingNumber(string s)
    {
        System.Text.RegularExpressions.Match m = System.Text.RegularExpressions.Regex.Match(s, @"\d+$");
        return m.Success ? int.Parse(m.Value) : int.MaxValue;
    }

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

                HandleTurn turn = PerformList[0]; // cache: instant actions can change the list
                GameObject performer = GameObject.Find(turn.Attacker);

                if (turn.Type == "Enemy")
                {
                    EnemyState ES = performer.GetComponent<EnemyState>();

                    switch (turn.Action)
                    {
                        case "Attack":
                            {
                                GameObject target = turn.AttackersTarget;
                                if ((target == null || !target.activeInHierarchy) && PlayerInBattle.Count > 0)
                                    target = PlayerInBattle[Random.Range(0, PlayerInBattle.Count)];

                                ES.PerformAttack(turn.Move, target);
                                break;
                            }

                        case "Heal":
                            ES.SpendEnergy(turn.Move.energyCost);
                            ES.DoHeal(turn.AttackersTarget, turn.Move.healAmount);
                            break;

                        case "Guard":
                            ES.DoGuard();
                            break;

                        case "Focus":
                            ES.DoFocus();
                            break;
                    }
                }
                else if (turn.Type == "Player")
                {
                    PlayerState PS = performer.GetComponent<PlayerState>();

                    switch (turn.Action)
                    {
                        case "Attack":
                            PS.PerformAttack(turn.Move, turn.AttackersTarget);
                            break;

                        case "Buff":
                            PS.PerformBuff(turn.Move);
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
                break;
        }

        switch (playerInput)
        {
            case (PlayerGUI.ACTIVATE):
                if (PlayersToManage.Count > 0)
                {
                    PlayerState PS = PlayersToManage[0].GetComponent<PlayerState>();
                    PS.OnTurnStart();
                    if (buffBar != null) buffBar.Bind(PS.statuses);

                    PlayersToManage[0].transform.Find("Selector").gameObject.SetActive(true);
                    playerChoice = new HandleTurn();
                    attackPanel.SetActive(true);
                    playerInput = PlayerGUI.WAITING;
                }
                break;

            case (PlayerGUI.WAITING):
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
            if (i >= PS.Moves.Count)
            {
                attackMoveButtons[i].gameObject.SetActive(false); // hide unused slots
                continue;
            }

            PlayerMove move = PS.Moves[i];
            int cd = PS.CooldownRemaining(move);

            attackMoveButtons[i].gameObject.SetActive(true);
            attackMoveButtons[i].interactable = PS.CanUse(move);
            attackMoveLabels[i].text = move.moveName + " (" + move.energyCost + " nrg)"
                                       + (cd > 0 ? "\nCooldown: " + cd : "");
        }
    }

    public void InputAttack(int moveIndex) // move buttons
    {
        PlayerState PS = PlayersToManage[0].GetComponent<PlayerState>();
        PlayerMove move = PS.Moves[moveIndex];

        if (!PS.CanUse(move))
        {
            Debug.Log("Can't use " + move.moveName + " right now (energy or cooldown)");
            return;
        }

        playerChoice.Attacker = PlayersToManage[0].name;
        playerChoice.AttackersGameObject = PlayersToManage[0];
        playerChoice.Type = "Player";
        playerChoice.Move = move;
        playerChoice.Action = (move.kind == MoveKind.Buff) ? "Buff" : "Attack";

        attackPanel.SetActive(false);
        moveSelectPanel.SetActive(false);

        if (move.kind == MoveKind.Attack && move.target == MoveTarget.SingleEnemy)
        {
            enemySelectPanel.SetActive(true); // pick a target
        }
        else
        {
            playerInput = PlayerGUI.DONE; // self buffs and AOE need no target
        }
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

    void RandomizeEnemyCount()
    {
        // for ints, Random.Range's max is exclusive, so +1
        int count = UnityEngine.Random.Range(minEnemies, maxEnemies + 1);
        count = Mathf.Clamp(count, 1, EnemyInBattle.Count);

        while (EnemyInBattle.Count > count)
        {
            GameObject extra = EnemyInBattle[EnemyInBattle.Count - 1];
            EnemyInBattle.RemoveAt(EnemyInBattle.Count - 1);
            extra.SetActive(false);
        }

        Debug.Log("Fighting " + EnemyInBattle.Count + " enemies");
    }
}