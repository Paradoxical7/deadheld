using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

// Moves the existing Enemy_petrol instance (and its Patrol_A/B/C points) from Room_01 into the Main Hub,
// just inside the west doorway - the one the player reaches from Room_01's puzzle door via the T-junction.
// Only transforms change; every component and reference on the enemy stays as it is.
public static class MoveEnemyToHub
{
    private const string ScenePath = "Assets/Scenes/Zone1_Greybox_Workspace.unity";
    private const string EnemyName = "Enemy_petrol";

    private static readonly Vector3 HubWestDoor = new Vector3(18f, 0f, 21.5f);
    private static readonly Vector3 EnemySpot = new Vector3(21f, 0f, 21.5f);   // 3 m inside the west door

    // A small loop in the west half of the hub, clear of the centre table and wall props.
    private static readonly (string name, Vector3 pos)[] PatrolSpots =
    {
        ("Patrol_A", new Vector3(21.5f, 0f, 18.2f)),
        ("Patrol_B", new Vector3(21.5f, 0f, 25.0f)),
        ("Patrol_C", new Vector3(22.5f, 0f, 21.5f)),
    };

    private const float MaxSnap = 1f;   // a spot more than this from the NavMesh counts as "not on it"

    [MenuItem("Tools/Deadheld/Move Patrol Enemy To Main Hub")]
    public static void Move()
    {
        if (EditorSceneManager.GetActiveScene().path != ScenePath)
        {
            Debug.LogError("MoveEnemyToHub: open " + ScenePath + " first.");
            return;
        }

        GameObject enemy = null;
        foreach (var ai in Object.FindObjectsByType<EnemyAI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (ai.gameObject.name != EnemyName) continue;
            if (enemy != null) { Debug.LogError("MoveEnemyToHub: more than one " + EnemyName + " - not moving anything."); return; }
            enemy = ai.gameObject;
        }
        if (enemy == null) { Debug.LogError("MoveEnemyToHub: no " + EnemyName + " with EnemyAI in the scene."); return; }

        var points = new Transform[PatrolSpots.Length];
        for (int i = 0; i < PatrolSpots.Length; i++)
        {
            var go = GameObject.Find(PatrolSpots[i].name);
            if (go == null) { Debug.LogError("MoveEnemyToHub: can't find " + PatrolSpots[i].name + " - not moving anything."); return; }
            points[i] = go.transform;
        }

        // Check every target against the baked NavMesh before touching anything.
        if (!OnNavMesh(EnemySpot, "enemy", out var enemyHit)) return;
        var pointHits = new Vector3[PatrolSpots.Length];
        for (int i = 0; i < PatrolSpots.Length; i++)
            if (!OnNavMesh(PatrolSpots[i].pos, PatrolSpots[i].name, out pointHits[i])) return;

        // Keep the enemy's height above the floor (its pivot sits ~1 m up), face the doorway.
        var t = enemy.transform;
        float pivotHeight = NavMesh.SamplePosition(t.position, out var oldHit, 2f, NavMesh.AllAreas)
            ? t.position.y - oldHit.position.y : t.position.y;
        Vector3 face = HubWestDoor - EnemySpot;
        face.y = 0f;

        Undo.RecordObject(t, "Move enemy to Main Hub");
        Vector3 from = t.position;
        t.SetPositionAndRotation(enemyHit + Vector3.up * pivotHeight, Quaternion.LookRotation(face));
        Debug.Log($"MoveEnemyToHub: {EnemyName} {from} -> {t.position}, facing the west door.", enemy);

        for (int i = 0; i < points.Length; i++)
        {
            Undo.RecordObject(points[i], "Move enemy to Main Hub");
            Vector3 old = points[i].position;
            points[i].position = pointHits[i];   // on the floor, so the agent's SetDestination lands cleanly
            Debug.Log($"MoveEnemyToHub: {points[i].name} {old} -> {points[i].position}", points[i]);
        }

        EditorSceneManager.MarkSceneDirty(enemy.scene);
        Debug.Log("MoveEnemyToHub: done - save the scene to keep it.");
    }

    private static bool OnNavMesh(Vector3 spot, string what, out Vector3 snapped)
    {
        if (NavMesh.SamplePosition(spot, out var hit, MaxSnap, NavMesh.AllAreas))
        {
            snapped = hit.position;
            return true;
        }
        snapped = spot;
        Debug.LogError($"MoveEnemyToHub: no baked NavMesh within {MaxSnap} m of {spot} ({what}). " +
                       "Nothing was moved - re-bake the Zone 1 NavMeshSurface, then run this again.");
        return false;
    }
}
