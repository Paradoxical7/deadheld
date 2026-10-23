using Unity.AI.Navigation;
using Unity.AI.Navigation.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

// 1) Shrinks Enemy_petrol a bit, keeping its feet on the floor.
// 2) Marks the Main Hub props as Not Walkable so the NavMesh gets holes around them
//    (tables are under the agent's 0.75 m step height, so a plain re-bake would let it climb onto them).
// 3) Keeps the NPC/hub-door setup out of the bake, then re-bakes the Zone 1 NavMeshSurface.
// All scene changes are Undo-able; the prefabs themselves are not touched.
public static class FixHubEnemy
{
    private const string ScenePath = "Assets/Scenes/Zone1_Greybox_Workspace.unity";
    private const string EnemyName = "Enemy_petrol";
    private const string HubPropsPath = "Zone_1/Rooms/Room_06_MainHub/Props";
    private const string NpcRootName = "NPC_HubLock";
    private const float EnemyScale = 0.85f;   // 2 m capsule -> 1.7 m, the player's height

    [MenuItem("Tools/Deadheld/Shrink Enemy + Make Hub Props Block NavMesh")]
    public static void Run()
    {
        if (EditorSceneManager.GetActiveScene().path != ScenePath)
        {
            Debug.LogError("FixHubEnemy: open " + ScenePath + " first.");
            return;
        }

        var props = GameObject.Find(HubPropsPath);
        if (props == null) { Debug.LogError("FixHubEnemy: can't find " + HubPropsPath + " - nothing changed."); return; }

        NavMeshSurface surface = null;
        var surfaces = Object.FindObjectsByType<NavMeshSurface>(FindObjectsSortMode.None);
        if (surfaces.Length == 1) surface = surfaces[0];
        else { Debug.LogError("FixHubEnemy: expected one NavMeshSurface, found " + surfaces.Length + " - nothing changed."); return; }

        ShrinkEnemy();

        int notWalkable = NavMesh.GetAreaFromName("Not Walkable");
        var propsMod = GetOrAdd(props);
        Undo.RecordObject(propsMod, "Hub props not walkable");
        propsMod.overrideArea = true;
        propsMod.area = notWalkable;
        propsMod.applyToChildren = true;
        Debug.Log("FixHubEnemy: Main Hub props marked Not Walkable.", props);

        var npcRoot = GameObject.Find(NpcRootName);
        if (npcRoot != null)
        {
            // The NPC walks and the hub doors slide, so neither should cut fixed holes in the NavMesh.
            var npcMod = GetOrAdd(npcRoot);
            Undo.RecordObject(npcMod, "Ignore NPC in bake");
            npcMod.ignoreFromBuild = true;
            npcMod.applyToChildren = true;
        }

        EditorSceneManager.MarkSceneDirty(props.scene);
        NavMeshAssetManager.instance.StartBakingSurfaces(new Object[] { surface });
        Debug.Log("FixHubEnemy: re-baking the Zone 1 NavMesh - wait for the progress bar to finish, then save the scene.", surface);
    }

    private static void ShrinkEnemy()
    {
        GameObject enemy = null;
        foreach (var ai in Object.FindObjectsByType<EnemyAI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (ai.gameObject.name == EnemyName) enemy = ai.gameObject;
        if (enemy == null) { Debug.LogWarning("FixHubEnemy: no " + EnemyName + " found - skipping the resize."); return; }

        // The capsule is 2 m tall around its pivot, so its feet sit 1 m x scale below the pivot.
        var t = enemy.transform;
        float feet = t.position.y - t.localScale.y;
        Undo.RecordObject(t, "Shrink enemy");
        t.localScale = Vector3.one * EnemyScale;
        t.position = new Vector3(t.position.x, feet + EnemyScale, t.position.z);
        Debug.Log($"FixHubEnemy: {EnemyName} scaled to {EnemyScale} (about {2f * EnemyScale:0.0} m tall).", enemy);
    }

    private static NavMeshModifier GetOrAdd(GameObject go)
    {
        var mod = go.GetComponent<NavMeshModifier>();
        return mod != null ? mod : Undo.AddComponent<NavMeshModifier>(go);
    }
}
