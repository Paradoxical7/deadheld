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
    private HashSet<string> unlockedDoors = new HashSet<string>();  // per-door unlock, keyed by door id
    public const string PuzzleCode = "4172";

    [Header("Battle return")]
    public string returnScene;            // scene to come back to after a battle
    public Vector3 returnPosition;        // where the player stood when combat started
    public bool hasReturnPoint = false;

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

    public bool TryUnlockDoor(string entered)
    {
        if (entered != PuzzleCode) return false;
        doorUnlocked = true;
        return true;
    }
    // --- Per-door unlock (use these so one door doesn't unlock all) ---
    public void UnlockDoor(string doorId)
    {
        if (!string.IsNullOrEmpty(doorId))
            unlockedDoors.Add(doorId);
    }

    public bool IsDoorUnlocked(string doorId)
    {
        return !string.IsNullOrEmpty(doorId) && unlockedDoors.Contains(doorId);
    }

    // code-checked unlock for a specific door
    public bool TryUnlockDoor(string doorId, string entered)
    {
        if (entered != PuzzleCode) return false;
        UnlockDoor(doorId);
        return true;
    }
}
