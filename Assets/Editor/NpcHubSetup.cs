using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// One-click setup for the guide NPC, the Main Hub door lock and the Zone 1 map UI.
// Everything goes under a single "NPC_HubLock" root so Remove can take it all out again.
public static class NpcHubSetup
{
    private const string RootName = "NPC_HubLock";
    private const string ScenePath = "Assets/Scenes/Zone1_Greybox_Workspace.unity";
    private const string DoorPrefabPath = "Assets/Prefabs/Doors/SlidingDoor_Double.prefab";
    private const string MapPath = "Assets/Art/UI/Zone1_Map.png";
    private const string BodyMatPath = "Assets/Materials/NPC_Guide.mat";
    private const string VisorMatPath = "Assets/Materials/NPC_Guide_Visor.mat";

    // Must match the area Zone1_Map.png was drawn over (and ZoneMapUI's defaults).
    private static readonly Vector2 WorldMin = new Vector2(-17f, -21.5f);
    private static readonly Vector2 WorldMax = new Vector2(63f, 78f);

    // Main Hub doorways are 2.24 m wide x 3 m tall, centred in 0.3 m walls.
    private static readonly (string name, Vector3 pos, float yaw)[] HubDoors =
    {
        ("HubDoor_South", new Vector3(26f, 0f, 13.65f), 0f),
        ("HubDoor_North", new Vector3(26f, 0f, 29.35f), 0f),
        ("HubDoor_West", new Vector3(18.15f, 0f, 21.5f), 90f),
        ("HubDoor_East", new Vector3(33.85f, 0f, 21.5f), 90f),
    };
    private static readonly Vector3 DoorScale = new Vector3(1.12f, 1.2f, 1f);   // 2 x 2.5 m door -> fills the doorway

    // West long hallway (HallwayStraightLong); NpcPatrol walks him up and down it.
    private static readonly Vector3 NpcPosition = new Vector3(-14.09f, 0f, 41.99f);
    private static readonly Vector3 NpcFaces = new Vector3(-14.09f, 0f, 50f);

    private static readonly (string text, Vector2 world)[] MapLabels =
    {
        ("CRYO\n(START)", new Vector2(0.2f, 0f)),
        ("MAIN HUB", new Vector2(26f, 21.5f)),
        ("STORAGE", new Vector2(48f, -3f)),
        ("MAINTENANCE", new Vector2(26f, -3f)),
        ("MAINTENANCE", new Vector2(46f, 53f)),
        ("LAB", new Vector2(-0.5f, 56f)),
        ("OBSERVATION", new Vector2(22f, 73f)),
        ("???", new Vector2(48f, 21.5f)),
    };

    [MenuItem("Tools/Deadheld/Build NPC + Hub Lock")]
    public static void Build()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            Debug.LogError("NpcHubSetup: open " + ScenePath + " first.");
            return;
        }
        if (GameObject.Find(RootName) != null)
        {
            Debug.LogWarning("NpcHubSetup: " + RootName + " already exists - use Remove first to rebuild.");
            return;
        }

        var root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Build NPC + Hub Lock");

        BuildDoors(root.transform);
        var ui = BuildUI(root.transform, out var prompt, out var panel, out var text);
        BuildNpc(root.transform, prompt, panel, text, ui);

        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = root;
        Debug.Log("NpcHubSetup: built " + RootName + ". Save the scene to keep it.");
    }

    [MenuItem("Tools/Deadheld/Remove NPC + Hub Lock")]
    public static void Remove()
    {
        var root = GameObject.Find(RootName);
        if (root == null)
        {
            Debug.Log("NpcHubSetup: nothing to remove.");
            return;
        }
        var scene = root.scene;
        Undo.DestroyObjectImmediate(root);
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("NpcHubSetup: removed " + RootName + ". Save the scene to keep it removed.");
    }

    // For -batchmode -executeMethod: open the zone scene, build, save.
    public static void BuildInZoneSceneBatch()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);
        Build();
        EditorSceneManager.SaveScene(scene);
    }

    private static void BuildDoors(Transform parent)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DoorPrefabPath);
        var group = new GameObject("MainHub_Doors");
        group.transform.SetParent(parent, false);

        foreach (var d in HubDoors)
        {
            var door = (GameObject)PrefabUtility.InstantiatePrefab(prefab, group.transform);
            door.name = d.name;
            door.transform.SetPositionAndRotation(d.pos, Quaternion.Euler(0f, d.yaw, 0f));
            door.transform.localScale = DoorScale;

            var so = new SerializedObject(door.GetComponentInChildren<SlidingDoors>());
            so.FindProperty("requiresUnlock").boolValue = true;
            so.FindProperty("openByProximity").boolValue = true;
            so.FindProperty("unlockFlag").enumValueIndex = (int)SlidingDoors.UnlockFlag.MainHub;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void BuildNpc(Transform parent, GameObject prompt, GameObject panel, TMP_Text text, ZoneMapUI map)
    {
        var npc = new GameObject("NPC_Guide");
        npc.transform.SetParent(parent, false);
        Vector3 facing = NpcFaces - NpcPosition;
        facing.y = 0f;
        npc.transform.SetPositionAndRotation(NpcPosition, Quaternion.LookRotation(facing));

        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(npc.transform, false);
        body.transform.localPosition = new Vector3(0f, 0.85f, 0f);
        body.transform.localScale = new Vector3(0.5f, 0.85f, 0.5f);   // 1.7 m tall, same as the player
        body.GetComponent<Renderer>().sharedMaterial =
            GetMaterial(BodyMatPath, new Color(0.25f, 0.55f, 0.5f), Color.black);

        // A glowing visor so you can tell which way he's facing.
        var visor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visor.name = "Visor";
        Object.DestroyImmediate(visor.GetComponent<Collider>());
        visor.transform.SetParent(npc.transform, false);
        visor.transform.localPosition = new Vector3(0f, 1.45f, 0.22f);
        visor.transform.localScale = new Vector3(0.32f, 0.08f, 0.06f);
        visor.GetComponent<Renderer>().sharedMaterial =
            GetMaterial(VisorMatPath, new Color(0.4f, 1f, 0.8f), new Color(0.4f, 1f, 0.8f) * 2f);

        var lightGO = new GameObject("GuideLight");
        lightGO.transform.SetParent(npc.transform, false);
        lightGO.transform.localPosition = new Vector3(0f, 2.2f, 0.4f);
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(0.5f, 1f, 0.85f);
        light.range = 3.5f;
        light.intensity = 2f;

        var trigger = npc.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 1.8f;
        trigger.center = new Vector3(0f, 1f, 0f);

        var so = new SerializedObject(npc.AddComponent<NpcInteract>());
        so.FindProperty("prompt").objectReferenceValue = prompt;
        so.FindProperty("dialoguePanel").objectReferenceValue = panel;
        so.FindProperty("dialogueText").objectReferenceValue = text;
        so.FindProperty("zoneMap").objectReferenceValue = map;
        so.ApplyModifiedPropertiesWithoutUndo();

        AddPatrol(npc, false);
    }

    [MenuItem("Tools/Deadheld/Apply NPC Patrol + Player Tag Fix")]
    public static void ApplyPatrolAndTagFix()
    {
        var npc = GameObject.Find(RootName + "/NPC_Guide");
        if (npc == null) Debug.LogWarning("NpcHubSetup: no " + RootName + "/NPC_Guide in the open scene.");
        else AddPatrol(npc, true);

        // Only the real player (the one with TankController) should carry the Player tag.
        foreach (var go in GameObject.FindGameObjectsWithTag("Player"))
        {
            if (go.GetComponentInChildren<TankController>() != null || go.GetComponentInParent<TankController>() != null) continue;
            Undo.RecordObject(go, "Fix Player tag");
            go.tag = "Untagged";
            Debug.Log("NpcHubSetup: removed stray Player tag from " + go.name, go);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    // Kinematic body so the moving capsule is treated as a moving collider, not a static one.
    private static void AddPatrol(GameObject npc, bool undo)
    {
        if (npc.GetComponent<NpcPatrol>() == null)
        {
            if (undo) Undo.AddComponent<NpcPatrol>(npc);
            else npc.AddComponent<NpcPatrol>();
        }
        var rb = npc.GetComponent<Rigidbody>();
        if (rb == null) rb = undo ? Undo.AddComponent<Rigidbody>(npc) : npc.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        Debug.Log("NpcHubSetup: NPC_Guide now patrols its hallway.", npc);
    }

    private static ZoneMapUI BuildUI(Transform parent, out GameObject prompt, out GameObject panel, out TMP_Text dialogueText)
    {
        var canvasGO = new GameObject("NPC_UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasGO.transform.SetParent(parent, false);
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        var root = canvasGO.transform;

        // "E - TALK" prompt; NpcInteract moves it over the NPC's head each frame.
        var promptText = Text("TalkPrompt", root, "E - TALK", 34f, TextAlignmentOptions.Center, Color.white);
        promptText.fontStyle = FontStyles.Bold;
        promptText.rectTransform.sizeDelta = new Vector2(320f, 60f);
        prompt = promptText.gameObject;
        prompt.SetActive(false);

        // Dialogue box along the bottom of the screen.
        var box = Panel("DialoguePanel", root, new Color(0f, 0f, 0f, 0.82f));
        Anchor(box.rectTransform, new Vector2(0.15f, 0.04f), new Vector2(0.85f, 0.26f));
        dialogueText = Text("DialogueText", box.transform, "", 34f, TextAlignmentOptions.TopLeft, Color.white);
        Anchor(dialogueText.rectTransform, Vector2.zero, Vector2.one, new Vector2(36f, 50f), new Vector2(-36f, -28f));
        var cont = Text("ContinueHint", box.transform, "[E] CONTINUE", 22f, TextAlignmentOptions.BottomRight, new Color(0.7f, 0.7f, 0.7f));
        Anchor(cont.rectTransform, Vector2.zero, Vector2.one, new Vector2(36f, 14f), new Vector2(-24f, -14f));
        panel = box.gameObject;
        panel.SetActive(false);

        // "Map acquired" toast near the top.
        var toast = Text("MapToast", root, "", 36f, TextAlignmentOptions.Center, new Color(1f, 0.75f, 0.3f));
        toast.fontStyle = FontStyles.Bold;
        Anchor(toast.rectTransform, new Vector2(0.5f, 0.86f), new Vector2(0.5f, 0.86f));
        toast.rectTransform.sizeDelta = new Vector2(1200f, 70f);
        toast.gameObject.SetActive(false);

        // Full-screen map, opened with M.
        var mapPanel = Panel("MapPanel", root, new Color(0f, 0f, 0f, 0.88f));
        Anchor(mapPanel.rectTransform, Vector2.zero, Vector2.one);
        var title = Text("Title", mapPanel.transform, "ZONE 1", 42f, TextAlignmentOptions.Center, new Color(0.67f, 0.84f, 0.93f));
        title.fontStyle = FontStyles.Bold;
        Anchor(title.rectTransform, new Vector2(0.5f, 0.955f), new Vector2(0.5f, 0.955f));
        title.rectTransform.sizeDelta = new Vector2(600f, 60f);
        var close = Text("CloseHint", mapPanel.transform, "[M] CLOSE", 24f, TextAlignmentOptions.Center, new Color(0.7f, 0.7f, 0.7f));
        Anchor(close.rectTransform, new Vector2(0.5f, 0.035f), new Vector2(0.5f, 0.035f));
        close.rectTransform.sizeDelta = new Vector2(400f, 40f);

        var sprite = LoadMapSprite();
        var map = Panel("Map", mapPanel.transform, Color.white);
        map.sprite = sprite;
        map.preserveAspect = true;
        float aspect = sprite != null ? sprite.rect.width / sprite.rect.height : 0.8f;
        map.rectTransform.sizeDelta = new Vector2(900f * aspect, 900f);
        Vector2 mapSize = map.rectTransform.sizeDelta;

        foreach (var l in MapLabels)
        {
            var label = Text("Label_" + l.text.Split('\n')[0], map.transform, l.text, 20f, TextAlignmentOptions.Center, Color.white);
            label.fontStyle = FontStyles.Bold;
            label.rectTransform.sizeDelta = new Vector2(200f, 60f);
            label.rectTransform.anchoredPosition = MapPoint(l.world, mapSize);
        }

        var marker = Panel("PlayerMarker", map.transform, new Color(1f, 0.3f, 0.3f));
        marker.rectTransform.sizeDelta = new Vector2(18f, 18f);
        mapPanel.gameObject.SetActive(false);

        var ui = canvasGO.AddComponent<ZoneMapUI>();
        var so = new SerializedObject(ui);
        so.FindProperty("mapPanel").objectReferenceValue = mapPanel.gameObject;
        so.FindProperty("mapImage").objectReferenceValue = map.rectTransform;
        so.FindProperty("playerMarker").objectReferenceValue = marker.rectTransform;
        so.FindProperty("toast").objectReferenceValue = toast;
        so.FindProperty("worldMin").vector2Value = WorldMin;
        so.FindProperty("worldMax").vector2Value = WorldMax;
        so.ApplyModifiedPropertiesWithoutUndo();
        return ui;
    }

    private static Vector2 MapPoint(Vector2 world, Vector2 mapSize)
    {
        Vector2 uv = new Vector2(
            Mathf.InverseLerp(WorldMin.x, WorldMax.x, world.x),
            Mathf.InverseLerp(WorldMin.y, WorldMax.y, world.y));
        return (uv - new Vector2(0.5f, 0.5f)) * mapSize;
    }

    private static TextMeshProUGUI Text(string name, Transform parent, string text, float size, TextAlignmentOptions align, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null) t.font = TMP_Settings.defaultFontAsset;
        t.text = text;
        t.fontSize = size;
        t.alignment = align;
        t.color = color;
        t.raycastTarget = false;
        return t;
    }

    private static Image Panel(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private static void Anchor(RectTransform rt, Vector2 min, Vector2 max)
    {
        Anchor(rt, min, max, Vector2.zero, Vector2.zero);
    }

    private static void Anchor(RectTransform rt, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
    {
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
    }

    private static Sprite LoadMapSprite()
    {
        AssetDatabase.ImportAsset(MapPath);
        var importer = AssetImporter.GetAtPath(MapPath) as TextureImporter;
        if (importer != null && importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(MapPath);
        if (sprite == null) Debug.LogWarning("NpcHubSetup: couldn't load the map sprite at " + MapPath);
        return sprite;
    }

    private static Material GetMaterial(string path, Color color, Color emission)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        if (emission != Color.black && mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emission);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();
        return mat;
    }
}
