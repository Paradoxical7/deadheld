using UnityEngine;

public class EnemyID : MonoBehaviour
{
    public string id;

    void Start()
    {
        // if this enemy was already defeated, don't exist on room return
        if (GameManager.Instance != null && GameManager.Instance.IsEnemyDefeated(id))
            Destroy(gameObject);
    }
}
