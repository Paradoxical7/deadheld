using UnityEngine;

[System.Serializable]
public class EnemyPrefabEntry
{
    public EnemyIdentity.EnemyRole role;
    public EnemyIdentity.EnemyStage stage;
    public GameObject prefab;   // turn based version of the enemy
    public bool inRandomPool = true;    // determines if enemy can show up as a random extra
}
