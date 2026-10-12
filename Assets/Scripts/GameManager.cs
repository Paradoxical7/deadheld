using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;   // the one global instance

    [Header("Enemy that triggered combat")]
    public EnemyIdentity.EnemyRole pendingEnemyRole;
    public EnemyIdentity.EnemyStage pendingEnemyStage;
    public bool hasPendingEnemy = false;

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
}