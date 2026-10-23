using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;   // the one global instance

    [Header("Enemy that triggered combat")]
    public EnemyIdentity.EnemyRole pendingEnemyRole;
    public EnemyIdentity.EnemyStage pendingEnemyStage;
    public bool hasPendingEnemy = false;

    public string pendingEnemyId;                 // which enemy triggered combat
    private HashSet<string> defeatedEnemies = new HashSet<string>();

    public enum Outcome { None, Won, Lost }
    public Outcome lastOutcome = Outcome.None;

    [Header("Room 01 puzzle")]
    public bool codeFound = false;
    public bool doorUnlocked = false;
    public string puzzleCode;                     // generated once per run

    [Header("Zone 1 progress")]
    public bool hubUnlocked = false;              // set by the guide NPC; opens the Main Hub doors
    public bool hasZoneMap = false;               // player can view the Zone 1 map (M)

    [Header("Battle return")]
    public string returnScene;            // scene to come back to after a battle
    public Vector3 returnPosition;        // where the player stood when combat started
    public bool hasReturnPoint = false;

    [Header("Player stats (persist across scenes)")]
    public float maxHP = 100f;                    // matches the battle player's baseHP
    public float currentHP = 100f;
    [Range(0f, 1f)] public float lossRecoverFraction = 0.25f;   // HP you come back with after losing a fight

    [Header("Exploration stamina (separate from battle NRG)")]
    public float maxEnergy = 100f;
    public float currentEnergy = 100f;
    public float sprintDrainPerSecond = 20f;
    public float energyRegenPerSecond = 12f;
    public float regenDelay = 0.75f;              // pause after sprinting before energy refills
    public float sprintResumeEnergy = 15f;        // after running dry, wait for this much before sprinting again
    private bool sprintExhausted;
    private float lastSprintTime;

    void Awake()
    {
        // singleton: keep only one, and survive scene loads
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        puzzleCode = Random.Range(0, 10000).ToString("D4");
        ResetPlayerStats();
    }

    // fresh run: full health and stamina
    public void ResetPlayerStats()
    {
        currentHP = maxHP;
        currentEnergy = maxEnergy;
        sprintExhausted = false;
    }

    // called by the battle scene when a fight ends
    public void SetHPFromBattle(float remainingHP, bool lost)
    {
        currentHP = lost
            ? Mathf.Max(1f, maxHP * lossRecoverFraction)   // no game over yet, so a loss sends you back weakened
            : Mathf.Clamp(remainingHP, 1f, maxHP);
        Debug.Log("GameManager: player HP after battle " + currentHP + "/" + maxHP);
    }

    // called every frame by the exploration controller; returns whether the player may sprint this frame
    public bool TickSprint(bool wantsToSprint, float deltaTime)
    {
        if (currentEnergy <= 0f) sprintExhausted = true;
        else if (sprintExhausted && currentEnergy >= sprintResumeEnergy) sprintExhausted = false;

        bool sprinting = wantsToSprint && !sprintExhausted;
        if (sprinting)
        {
            currentEnergy = Mathf.Max(0f, currentEnergy - sprintDrainPerSecond * deltaTime);
            lastSprintTime = Time.time;
        }
        else if (Time.time - lastSprintTime >= regenDelay)
        {
            currentEnergy = Mathf.Min(maxEnergy, currentEnergy + energyRegenPerSecond * deltaTime);
        }
        return sprinting;
    }

    void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // after returning from a battle, drop the player back where they were
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!hasReturnPoint) return;
        if (!string.IsNullOrEmpty(returnScene) && scene.name != returnScene) return;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            CharacterController cc = p.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;        // CC fights direct position sets
            p.transform.position = returnPosition;
            Rigidbody rb = p.GetComponent<Rigidbody>();
            if (rb != null) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
            if (cc != null) cc.enabled = true;
        }
        hasReturnPoint = false;
    }

    public void SetReturnPoint(string sceneName, Vector3 pos)
    {
        returnScene = sceneName;
        returnPosition = pos;
        hasReturnPoint = true;
    }

    public void SetPendingEnemy(EnemyIdentity.EnemyRole role, EnemyIdentity.EnemyStage stage)
    {
        pendingEnemyRole = role;
        pendingEnemyStage = stage;
        hasPendingEnemy = true;
        Debug.Log("GameManager: battle queued vs " + role + " (" + stage + ")");
    }

    public void MarkEnemyDefeated(string id)
    {
        if (!string.IsNullOrEmpty(id))
            defeatedEnemies.Add(id);
    }

    public bool IsEnemyDefeated(string id)
    {
        return !string.IsNullOrEmpty(id) && defeatedEnemies.Contains(id);
    }

    public void MarkCodeFound()
    {
        codeFound = true;
        Debug.Log("GameManager: puzzle code found");
    }

    public void UnlockHubAndGiveMap()
    {
        hubUnlocked = true;
        hasZoneMap = true;
        Debug.Log("GameManager: Main Hub unlocked, Zone 1 map given");
    }

    public bool TryUnlockDoor(string entered)
    {
        if (entered != puzzleCode) return false;
        doorUnlocked = true;
        return true;
    }
}
