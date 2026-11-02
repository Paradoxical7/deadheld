using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class BattleOutcome : MonoBehaviour
{
    public RewardScreen rewards;
    public float winDelay = 1.0f;

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
            bsm.FreezeBattle();
            Debug.Log("BattleOutcome: PLAYER WON");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.lastOutcome = GameManager.Outcome.Won;
                GameManager.Instance.MarkEnemyDefeated(GameManager.Instance.pendingEnemyId);
            }
            StartCoroutine(WinSequence());
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

    private IEnumerator WinSequence()
    {
        yield return new WaitForSeconds(winDelay);

        // fallback: find the reward screen even if the field is empty or the object is disabled
        if (rewards == null)
            rewards = FindAnyObjectByType<RewardScreen>(FindObjectsInactive.Include);

        if (rewards != null)
        {
            Debug.Log("BattleOutcome: showing the reward screen");
            rewards.Show(FinishWin);
        }
        else
        {
            Debug.LogWarning("BattleOutcome: no RewardScreen found in the scene, skipping rewards");
            FinishWin();
        }
    }

    // runs after the player picks a reward
    private void FinishWin()
    {
        SavePlayerHP(false); // after the reward, so a health boost is included
        ReturnFromBattle();
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
