using UnityEngine;
using System.Collections.Generic;

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
}
