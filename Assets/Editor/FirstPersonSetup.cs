using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// One-click setup for the first-person exploration experiment in Zone 1.
// Adds a head camera under the scene's Player instance (an instance override; Player.prefab is untouched).
// The fixed CameraZone rigs stay in the scene: FirstPersonLook just switches them off while it is enabled,
// so Remove (or unticking FirstPersonLook) puts the old camera system straight back.
public static class FirstPersonSetup
{
    private const string CameraName = "FirstPersonCamera";
    private const string IntroName = "CryoIntro";
    private const string ScenePath = "Assets/Scenes/Zone1_Greybox_Workspace.unity";
    private const float EyeHeight = 1.6f;    // player capsule is 1.7 m tall, pivot at the feet

    [MenuItem("Tools/Deadheld/Build First-Person Camera")]
    public static void Build()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            Debug.LogError("FirstPersonSetup: open " + ScenePath + " first.");
            return;
        }
        if (Object.FindFirstObjectByType<FirstPersonLook>(FindObjectsInactive.Include) != null)
        {
            Debug.LogWarning("FirstPersonSetup: " + CameraName + " already exists - use Remove first to rebuild.");
            return;
        }
        var player = Object.FindFirstObjectByType<TankController>();
        if (player == null)
        {
            Debug.LogError("FirstPersonSetup: no TankController (Player) in the scene.");
            return;
        }

        var go = new GameObject(CameraName);
        Undo.RegisterCreatedObjectUndo(go, "Build First-Person Camera");
        go.tag = "MainCamera";
        go.transform.SetParent(player.transform, false);
        go.transform.localPosition = new Vector3(0f, EyeHeight, 0f);
        go.transform.localRotation = Quaternion.identity;

        var cam = go.AddComponent<Camera>();
        var main = GameObject.Find("Main Camera");
        if (main != null && main.TryGetComponent<Camera>(out var mainCam)) cam.CopyFrom(mainCam);
        go.transform.localPosition = new Vector3(0f, EyeHeight, 0f);   // CopyFrom also copies the transform
        go.transform.localRotation = Quaternion.identity;
        cam.depth = 0f;
        cam.fieldOfView = 70f;
        cam.nearClipPlane = 0.05f;    // walls right in front of your face shouldn't clip

        go.AddComponent<AudioListener>();
        go.AddComponent<FirstPersonLook>();

        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = go;
        Debug.Log("FirstPersonSetup: built " + CameraName + " under " + player.name + ". Save the scene to keep it.");
    }

    [MenuItem("Tools/Deadheld/Remove First-Person Camera")]
    public static void Remove()
    {
        var look = Object.FindFirstObjectByType<FirstPersonLook>(FindObjectsInactive.Include);
        if (look == null)
        {
            Debug.LogWarning("FirstPersonSetup: nothing to remove.");
            return;
        }
        var scene = look.gameObject.scene;
        Undo.DestroyObjectImmediate(look.gameObject);
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("FirstPersonSetup: removed " + CameraName + ". Save the scene to keep it.");
    }

    // The cryo wake-up intro: one scene object holding CryoIntro, which builds its overlay at runtime.
    [MenuItem("Tools/Deadheld/Build Cryo Intro")]
    public static void BuildIntro()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            Debug.LogError("FirstPersonSetup: open " + ScenePath + " first.");
            return;
        }
        if (Object.FindFirstObjectByType<CryoIntro>(FindObjectsInactive.Include) != null)
        {
            Debug.LogWarning("FirstPersonSetup: " + IntroName + " already exists.");
            return;
        }

        var go = new GameObject(IntroName, typeof(CryoIntro));
        Undo.RegisterCreatedObjectUndo(go, "Build Cryo Intro");
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = go;
        Debug.Log("FirstPersonSetup: built " + IntroName + ". Save the scene to keep it.");
    }

    [MenuItem("Tools/Deadheld/Remove Cryo Intro")]
    public static void RemoveIntro()
    {
        var intro = Object.FindFirstObjectByType<CryoIntro>(FindObjectsInactive.Include);
        if (intro == null)
        {
            Debug.LogWarning("FirstPersonSetup: no " + IntroName + " to remove.");
            return;
        }
        var scene = intro.gameObject.scene;
        Undo.DestroyObjectImmediate(intro.gameObject);
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("FirstPersonSetup: removed " + IntroName + ". Save the scene to keep it.");
    }
}
