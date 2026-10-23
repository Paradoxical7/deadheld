using UnityEngine;

// Purely visual: gives every "Enemy"-tagged object in the battle (scene-placed, spawned or summoned)
// the glowing battle material. Enemies spawn after Start, so it keeps checking a few times a second.
public class BattleEnemyLook : MonoBehaviour
{
    [SerializeField] private Material enemyMaterial;
    [SerializeField] private float checkInterval = 0.25f;

    private float nextCheck;

    // Play Mode Options skips the reload, so this keeps its last-session value.
    private void OnEnable() { nextCheck = 0f; }

    private void Update()
    {
        if (enemyMaterial == null || Time.unscaledTime < nextCheck) return;
        nextCheck = Time.unscaledTime + checkInterval;

        foreach (var enemy in GameObject.FindGameObjectsWithTag("Enemy"))
            foreach (var r in enemy.GetComponentsInChildren<Renderer>())
                if (r.sharedMaterial != enemyMaterial) r.sharedMaterial = enemyMaterial;
    }
}
