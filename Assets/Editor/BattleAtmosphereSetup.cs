using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// One-click horror look for TurnBasedScene: dark fog/ambient, spotlights, a global post-processing Volume,
// a dark floor, a readable player and faintly glowing enemies. Visual only - no battle scripts or UI are touched.
// Re-running it updates the existing setup with the values below, so tune here and run again.
// New assets go in Assets/BattleAtmosphere; scene objects go under a "BattleAtmosphere" root.
// To undo: git restore Assets/Scenes/TurnBasedScene.unity and delete Assets/BattleAtmosphere.
public static class BattleAtmosphereSetup
{
    private const string ScenePath = "Assets/Scenes/TurnBasedScene.unity";
    private const string Dir = "Assets/BattleAtmosphere/";
    private const string RootName = "BattleAtmosphere";

    // ---- tuning ----
    // URP spot/point lights fall off with distance squared, so lights ~6-7 m away need intensities in the tens.
    private static readonly Color Background = new Color(0.03f, 0.03f, 0.04f);
    private static readonly Color Ambient = new Color(0.12f, 0.12f, 0.145f);
    private const float FogDensity = 0.03f;            // exponential squared
    private const float ReflectionIntensity = 0.3f;

    private const float KeyIntensity = 0.55f;          // directional "moonlight" - lights the floor, casts the shadows
    private static readonly Color KeyColor = new Color(0.6f, 0.68f, 0.88f);
    private const float PlayerSpotIntensity = 60f;
    private const float EnemySpotIntensity = 70f;
    private const float RimIntensity = 4f;

    private static readonly Color FloorColor = new Color(0.2f, 0.2f, 0.22f);
    private const float FloorSmoothness = 0.2f;
    private static readonly Color PlayerColor = new Color(0.66f, 0.76f, 0.88f);
    private static readonly Color EnemyColor = new Color(0.3f, 0.05f, 0.05f);
    private static readonly Color EnemyEmission = new Color(0.35f, 0.03f, 0.02f);   // below the bloom threshold: a faint glow

    private const float VignetteIntensity = 0.28f;
    private const float Exposure = 0.05f;
    private const float Contrast = 12f;
    private const float Saturation = -18f;
    private const float BloomIntensity = 0.12f;
    private const float BloomThreshold = 1.2f;
    private const float GrainIntensity = 0.12f;
    // ----------------

    [MenuItem("Tools/Deadheld/Apply or Update Battle Atmosphere")]
    public static void Apply()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Directory.CreateDirectory(Dir);

        // --- materials ---
        var floorMat = Lit("Battle_Floor.mat", FloorColor, FloorSmoothness, Color.black);
        var playerMat = Lit("Battle_Player.mat", PlayerColor, 0.45f, Color.black);
        var enemyMat = Lit("Battle_Enemy.mat", EnemyColor, 0.35f, EnemyEmission);

        var ground = GameObject.Find("Ground");
        if (ground != null) SetMaterial(ground, floorMat);
        var player = GameObject.Find("Player");
        if (player != null) SetMaterial(player, playerMat);

        // --- camera: dark background, post-processing on ---
        var cam = Camera.main;
        if (cam != null)
        {
            Undo.RecordObject(cam, "Battle atmosphere");
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Background;
            var data = cam.GetUniversalAdditionalCameraData();
            Undo.RecordObject(data, "Battle atmosphere");
            data.renderPostProcessing = true;
        }

        // --- environment: low ambient, dark exponential fog ---
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = Ambient;
        RenderSettings.reflectionIntensity = ReflectionIntensity;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = Background;
        RenderSettings.fogDensity = FogDensity;

        // --- lighting: cool key for the floor and shadows, plus pools of light on the fighters ---
        var sun = GameObject.Find("Directional Light");
        if (sun != null && sun.TryGetComponent<Light>(out var key))
        {
            Undo.RecordObject(key, "Battle atmosphere");
            key.intensity = KeyIntensity;
            key.color = KeyColor;
            key.shadows = LightShadows.Soft;
        }

        var root = GameObject.Find(RootName);
        if (root == null)
        {
            root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Battle atmosphere");
        }

        Spot(root.transform, "Spot_Player", new Vector3(-4.2f, 6.5f, -2.2f), new Vector3(-5.12f, 0.9f, 1.14f),
             new Color(0.82f, 0.88f, 1f), PlayerSpotIntensity, 15f, 42f);
        Spot(root.transform, "Spot_Enemies", new Vector3(2.2f, 7.5f, -2.5f), new Vector3(4.9f, 0.8f, 1.4f),
             new Color(1f, 0.55f, 0.42f), EnemySpotIntensity, 18f, 74f);

        var rimLight = Child<Light>(root.transform, "Rim_Enemies");
        rimLight.transform.position = new Vector3(7.5f, 1.6f, 1.4f);   // behind the enemy line, for a red edge
        rimLight.type = LightType.Point;
        rimLight.color = new Color(0.9f, 0.12f, 0.08f);
        rimLight.intensity = RimIntensity;
        rimLight.range = 7f;

        // --- post-processing ---
        var volume = Child<Volume>(root.transform, "GlobalVolume");
        volume.isGlobal = true;
        volume.priority = 10f;
        volume.sharedProfile = BuildProfile();

        var look = root.GetComponent<BattleEnemyLook>();
        if (look == null) look = root.AddComponent<BattleEnemyLook>();
        var so = new SerializedObject(look);
        so.FindProperty("enemyMaterial").objectReferenceValue = enemyMat;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("BattleAtmosphereSetup: applied and saved " + ScenePath + ". Press Play to see it.");
    }

    // For -batchmode -executeMethod.
    public static void ApplyBatch()
    {
        Apply();
    }

    private static VolumeProfile BuildProfile()
    {
        string path = Dir + "BattleVolumeProfile.asset";
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);
        }

        var vignette = Get<Vignette>(profile);
        vignette.color.value = Color.black;
        vignette.intensity.value = VignetteIntensity;
        vignette.smoothness.value = 0.4f;

        var color = Get<ColorAdjustments>(profile);
        color.postExposure.value = Exposure;
        color.contrast.value = Contrast;
        color.saturation.value = Saturation;
        color.colorFilter.value = new Color(0.92f, 0.96f, 1f);   // slight cold cast

        var bloom = Get<Bloom>(profile);
        bloom.threshold.value = BloomThreshold;
        bloom.intensity.value = BloomIntensity;
        bloom.scatter.value = 0.5f;
        bloom.highQualityFiltering.value = true;   // avoids the blocky, pixelated halo

        var grain = Get<FilmGrain>(profile);
        grain.type.value = FilmGrainLookup.Medium1;
        grain.intensity.value = GrainIntensity;
        grain.response.value = 0.8f;

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        return profile;
    }

    // Reuses the profile's existing override if it has one, so re-running only changes the values.
    private static T Get<T>(VolumeProfile profile) where T : VolumeComponent
    {
        if (profile.TryGet<T>(out var existing))
        {
            existing.SetAllOverridesTo(true);
            EditorUtility.SetDirty(existing);
            return existing;
        }
        var comp = profile.Add<T>(true);   // overrides on for every parameter
        comp.name = typeof(T).Name;
        AssetDatabase.AddObjectToAsset(comp, profile);
        return comp;
    }

    private static void Spot(Transform parent, string name, Vector3 pos, Vector3 target, Color color, float intensity, float range, float angle)
    {
        var l = Child<Light>(parent, name);
        l.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(target - pos));
        l.type = LightType.Spot;
        l.color = color;
        l.intensity = intensity;
        l.range = range;
        l.spotAngle = angle;
        l.innerSpotAngle = angle * 0.55f;
        l.shadows = LightShadows.Soft;
    }

    // Finds (or creates) a child with this name and returns its component of type T.
    private static T Child<T>(Transform parent, string name) where T : Component
    {
        var t = parent.Find(name);
        if (t == null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            t = go.transform;
        }
        var c = t.GetComponent<T>();
        if (c == null) c = t.gameObject.AddComponent<T>();
        Undo.RecordObject(c, "Battle atmosphere");
        return c;
    }

    private static void SetMaterial(GameObject go, Material mat)
    {
        foreach (var r in go.GetComponentsInChildren<Renderer>())
        {
            Undo.RecordObject(r, "Battle atmosphere");
            r.sharedMaterial = mat;
        }
    }

    private static Material Lit(string file, Color baseColor, float smoothness, Color emission)
    {
        string path = Dir + file;
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.SetColor("_BaseColor", baseColor);
        mat.SetFloat("_Smoothness", smoothness);
        mat.SetFloat("_Metallic", 0f);
        if (emission != Color.black)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emission);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        EditorUtility.SetDirty(mat);
        return mat;
    }
}
