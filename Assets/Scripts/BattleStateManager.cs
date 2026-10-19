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

    // turn based logic
    public enum BattlePhase { PLAYER_PHASE, ENEMY_PHASE }
    public BattlePhase currentPhase;
    private int currentEnemyIndex = 0;

    // entities currently in scene + turn based logic
    public List<HandleTurn> PerformList = new List<HandleTurn>();
    public List<GameObject> PlayerInBattle = new List<GameObject>();
    public List<GameObject> EnemyInBattle = new List<GameObject>();

    // player input
    public enum PlayerGUI { ACTIVATE, WAITING, INPUT1, INPUT2, DONE }
    public PlayerGUI playerInput;
    public List<GameObject> PlayersToManage = new List<GameObject>();
    private HandleTurn playerChoice;

    // ui stuff
    public GameObject enemyButton;
    public Transform Spacer;
    public GameObject attackPanel;
    public GameObject enemySelectPanel;
    public GameObject moveSelectPanel;
    public List<Button> attackMoveButtons;
    public List<TMP_Text> attackMoveLabels;
    public BuffBar buffBar;

    // enemy spawning
    public bool randomizeEnemyCount = true;
    public int minEnemies = 1;
    public int maxEnemies = 4;
    public List<EnemyPrefabEntry> enemyRoster = new List<EnemyPrefabEntry>();
    public Transform[] enemySpawnPoints; // slot 0 = main enemy (spare slots are for summons)
    public GameObject debugMainEnemy;
    private GameObject[] slotOccupants;
    private readonly Dictionary<GameObject, GameObject> enemyButtonFor = new Dictionary<GameObject, GameObject>();
    private int enemySpawnCount = 0;

    void Start()
    {
        battleState = performAction.WAIT;
        PlayerInBattle.AddRange(GameObject.FindGameObjectsWithTag("Player"));
        slotOccupants = new GameObject[enemySpawnPoints.Length];

        attackPanel.SetActive(false);
        enemySelectPanel.SetActive(false);
        moveSelectPanel.SetActive(false);

        GameObject mainEnemy = null;
        EnemyIdentity.EnemyStage stage = EnemyIdentity.EnemyStage.Stage1;

        GameManager gm = GameManager.Instance;
        if (gm != null && gm.hasPendingEnemy)
        {
            // the overworld enemy that touched the player
            stage = gm.pendingEnemyStage;
            mainEnemy = FindEnemyPrefab(gm.pendingEnemyRole, stage);
            if (mainEnemy == null)
                Debug.LogWarning("No battle prefab in the roster for " + gm.pendingEnemyRole + " " + stage);
        }
        else
        {
            mainEnemy = debugMainEnemy; // playing the battle scene directly
        }

        if (mainEnemy != null && enemySpawnPoints.Length > 0)
        {
            SpawnEncounter(mainEnemy, stage);
        }
        else
        {

            Debug.LogWarning("FALLBACK: using scene-placed enemies. mainEnemy="
                + (mainEnemy != null ? mainEnemy.name : "none")
                + ", spawn points=" + enemySpawnPoints.Length);

            // fallback: enemies placed in the scene (old behavior)
            EnemyInBattle.AddRange(GameObject.FindGameObjectsWithTag("Enemy"));
            EnemyInBattle = EnemyInBattle.OrderBy(e => TrailingNumber(e.name)).ThenBy(e => e.name).ToList();
            if (randomizeEnemyCount) RandomizeEnemyCount();
            foreach (GameObject enemy in EnemyInBattle) CreateEnemyButton(enemy);
        }

        StartPlayerPhase();
    }

    static int TrailingNumber(string s)
    {
        System.Text.RegularExpressions.Match m = System.Text.RegularExpressions.Regex.Match(s, @"\d+$");
        return m.Success ? int.Parse(m.Value) : int.MaxValue;
    }

    GameObject FindEnemyPrefab(EnemyIdentity.EnemyRole role, EnemyIdentity.EnemyStage stage)
    {
        GameObject sameRole = null;
        foreach (EnemyPrefabEntry e in enemyRoster)
        {
            if (e.role != role || e.prefab == null) continue;
            if (e.stage == stage) return e.prefab;
            if (sameRole == null) sameRole = e.prefab;
        }
        return sameRole;
    }

    GameObject PickRandomExtra(EnemyIdentity.EnemyStage stage)
    {
        // prefer extras at the same stage as the touched enemy
        List<EnemyPrefabEntry> options = enemyRoster.Where(e => e.inRandomPool && e.prefab != null && e.stage == stage).ToList();
        if (options.Count == 0)
            options = enemyRoster.Where(e => e.inRandomPool && e.prefab != null).ToList();
        if (options.Count == 0) return null;

        return options[UnityEngine.Random.Range(0, options.Count)].prefab;
    }

    void SpawnEncounter(GameObject mainPrefab, EnemyIdentity.EnemyStage stage)
    {
        // hide any enemies placed in the scene so only the spawned ones fight
        foreach (GameObject placed in GameObject.FindGameObjectsWithTag("Enemy"))
            placed.SetActive(false);

        // random total, including the enemy that touched the player
        int total = UnityEngine.Random.Range(minEnemies, maxEnemies + 1);
        total = Mathf.Clamp(total, 1, enemySpawnPoints.Length);

        SpawnEnemyInSlot(mainPrefab, 0);

        for (int slot = 1; slot < total; slot++)
        {
            GameObject extra = PickRandomExtra(stage);
            if (extra == null) break;
            SpawnEnemyInSlot(extra, slot);
        }

        Debug.Log("Fighting " + EnemyInBattle.Count + " enemies");
    }

    GameObject SpawnEnemyInSlot(GameObject prefab, int slot)
    {
        Transform point = enemySpawnPoints[slot];
        GameObject enemy = Instantiate(prefab, point.position, point.rotation);

        enemySpawnCount++;
        enemy.name = prefab.name + " " + enemySpawnCount; // unique names: "Engineer 1", "Medic 2"

        slotOccupants[slot] = enemy;
        EnemyInBattle.Add(enemy);
        CreateEnemyButton(enemy);
        return enemy;
    }

    int FreeSlotIndex()
    {
        for (int i = 0; i < slotOccupants.Length; i++)
        {
            if (slotOccupants[i] == null || !slotOccupants[i].activeInHierarchy) return i;
        }
        return -1;
    }

    public bool HasFreeEnemySlot() { return FreeSlotIndex() >= 0; }

    // called by an enemy's Summon move
    public GameObject SummonEnemy(GameObject prefab)
    {
        int slot = FreeSlotIndex();
        if (slot < 0 || prefab == null) return null;

        GameObject enemy = SpawnEnemyInSlot(prefab, slot);
        enemy.GetComponent<EnemyState>().justSummoned = true;
        return enemy;
    }

    void CreateEnemyButton(GameObject enemy)
    {
        GameObject newButton = Instantiate(enemyButton) as GameObject;
        EnemySelectButton button = newButton.GetComponent<EnemySelectButton>();
        TMP_Text buttonText = newButton.transform.Find("Text (TMP)").gameObject.GetComponent<TMP_Text>();
        buttonText.text = enemy.name;
        button.EnemyPrefab = enemy;
        newButton.transform.SetParent(Spacer, false);
        enemyButtonFor[enemy] = newButton;
    }

    // called by EnemyState when an enemy dies (also removes its select button)
    public void OnEnemyDied(GameObject enemy)
    {
        EnemyInBattle.Remove(enemy);

        GameObject btn;
        if (enemyButtonFor.TryGetValue(enemy, out btn))
        {
            Destroy(btn);
            enemyButtonFor.Remove(enemy);
        }
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
                GameObject performer = turn.AttackersGameObject;

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

                        case "Summon":
                            ES.PerformSummon(turn.Move);
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