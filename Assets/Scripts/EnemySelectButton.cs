using UnityEngine;

public class EnemySelectButton : MonoBehaviour
{
    public GameObject EnemyPrefab;

    void Update()
    {
        // enemy died, remove its button
        if (EnemyPrefab == null || !EnemyPrefab.activeInHierarchy)
        {
            Destroy(gameObject);
        }
    }

    public void SelectEnemy()
    {
        // ignore clicks on a dead enemy
        if (EnemyPrefab == null || !EnemyPrefab.activeInHierarchy) return;

        GameObject.Find("BattleManager").GetComponent<BattleStateMachine>().Input2(EnemyPrefab);
    }
}