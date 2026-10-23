using UnityEngine;
using UnityEngine.SceneManagement;

public class BattleOutcome : MonoBehaviour
{
    private BattleStateMachine bsm;
    private bool battleStarted = false;
    private bool resolved = false;

    void Start()
    {
        bsm = FindAnyObjectByType<BattleStateMachine>();
        if (bsm == null)
            Debug.LogWarning("BattleOutcome: no BattleStateMachine in scene.");
    }

    void Update()
    {
        if (bsm == null || resolved) return;

        // wait until the battle has populated its unit lists before judging
        if (!battleStarted)
        {
            if (bsm.EnemyInBattle.Count > 0 && bsm.PlayerInBattle.Count > 0)
                battleStarted = true;
            return;
        }

        if (bsm.EnemyInBattle.Count == 0)
        {
            resolved = true;
            Debug.Log("BattleOutcome: PLAYER WON");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.lastOutcome = GameManager.Outcome.Won;
                GameManager.Instance.MarkEnemyDefeated(GameManager.Instance.pendingEnemyId);
            }
            SavePlayerHP(false);
            ReturnFromBattle();
        }
        else if (bsm.PlayerInBattle.Count == 0)
        {
            resolved = true;
            Debug.Log("BattleOutcome: PLAYER LOST");
            if (GameManager.Instance != null)
                GameManager.Instance.lastOutcome = GameManager.Outcome.Lost;
            SavePlayerHP(true);
            ReturnFromBattle();
        }
    }

    // write the player's remaining HP back to GameManager before leaving the battle
    private void SavePlayerHP(bool lost)
    {
        if (GameManager.Instance == null) return;
        // a defeated player has already been deactivated, so include inactive objects
        PlayerState ps = FindAnyObjectByType<PlayerState>(FindObjectsInactive.Include);
        if (ps != null) GameManager.Instance.SetHPFromBattle(ps.player.currentHP, lost);
    }

    private void ReturnFromBattle()
    {
        // go back to whatever scene we fought in, not a hardcoded room
        string back = (GameManager.Instance != null && !string.IsNullOrEmpty(GameManager.Instance.returnScene))
            ? GameManager.Instance.returnScene
            : "Room_01";
        SceneManager.LoadScene(back);
    }
}
