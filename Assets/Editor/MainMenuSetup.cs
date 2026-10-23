using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Builds Assets/Scenes/MainMenu.unity (title + Start/Options/Exit) and puts it first in Build Settings.
// Everything is generated: plain UI shapes plus three procedural textures in Assets/Art/UI/Menu.
public static class MainMenuSetup
{
    private const string MenuScenePath = "Assets/Scenes/MainMenu.unity";
    private const string GameScenePath = "Assets/Scenes/Zone1_Greybox_Workspace.unity";
    private const string TexDir = "Assets/Art/UI/Menu/";

    private static readonly Color Void = new Color(0.035f, 0.038f, 0.045f);
    private static readonly Color Bone = new Color(0.86f, 0.84f, 0.78f);
    private static readonly Color Dim = new Color(0.5f, 0.5f, 0.52f);
    private static readonly Color Blood = new Color(0.7f, 0.1f, 0.08f);
    private static readonly Color DarkBlood = new Color(0.45f, 0.04f, 0.03f, 0.9f);

    [MenuItem("Tools/Deadheld/Create or Rebuild Main Menu Scene")]
    public static void Create()
    {
        if (File.Exists(MenuScenePath) && !Application.isBatchMode &&
            !EditorUtility.DisplayDialog("Rebuild main menu?",
                MenuScenePath + " already exists. Rebuild it? Any hand edits to that scene will be lost.", "Rebuild", "Cancel"))
            return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var vignette = LoadSprite("Menu_Vignette.png", false);
        var glow = LoadSprite("Menu_Glow.png", false);
        var scanlines = LoadSprite("Menu_Scanlines.png", true);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Void;
        camGO.AddComponent<AudioListener>();

        // The project uses the new Input System only, so the UI needs its input module, not StandaloneInputModule.
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        var canvasGO = new GameObject("MainMenuCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        var root = canvasGO.transform;
        var menu = canvasGO.AddComponent<MainMenu>();

        // Red haze behind the title.
        var haze = MakeImage("TitleGlow", root, glow, new Color(0.7f, 0.08f, 0.06f, 0.32f));
        Place(haze.rectTransform, new Vector2(0.5f, 0.72f), new Vector2(1500f, 560f), Vector2.zero);

        // Title with a blood-red drop shadow; the group flickers like a failing light.
        var titleGroup = new GameObject("TitleGroup", typeof(RectTransform), typeof(CanvasGroup));
        titleGroup.transform.SetParent(root, false);
        Place((RectTransform)titleGroup.transform, new Vector2(0.5f, 0.72f), new Vector2(1400f, 240f), Vector2.zero);
        titleGroup.AddComponent<MenuFlicker>();
        var shadow = MakeText("TitleShadow", titleGroup.transform, "Deadheld", 150f, DarkBlood);
        Stretch(shadow.rectTransform, new Vector2(5f, -7f));
        TitleStyle(shadow);
        var title = MakeText("Title", titleGroup.transform, "Deadheld", 150f, Bone);
        Stretch(title.rectTransform, Vector2.zero);
        TitleStyle(title);

        var rule = MakeImage("TitleRule", root, null, Blood);
        Place(rule.rectTransform, new Vector2(0.5f, 0.72f), new Vector2(560f, 3f), new Vector2(0f, -108f));
        var sub = MakeText("Subtitle", root, "ZONE 1   //   GREYBOX PROTOTYPE", 22f, Dim);
        sub.characterSpacing = 18f;
        Place(sub.rectTransform, new Vector2(0.5f, 0.72f), new Vector2(1000f, 40f), new Vector2(0f, -140f));

        var start = MakeButton("StartButton", root, "Start", new Vector2(0f, -30f));
        var options = MakeButton("OptionsButton", root, "Options", new Vector2(0f, -120f));
        var exit = MakeButton("ExitButton", root, "Exit", new Vector2(0f, -210f));
        UnityEventTools.AddPersistentListener(start.onClick, menu.StartGame);
        UnityEventTools.AddPersistentListener(options.onClick, menu.OpenOptions);
        UnityEventTools.AddPersistentListener(exit.onClick, menu.QuitGame);

        var footer = MakeText("Version", root, "v0.1  -  PROTOTYPE BUILD", 18f, new Color(0.38f, 0.38f, 0.4f));
        footer.characterSpacing = 10f;
        footer.alignment = TextAlignmentOptions.BottomRight;
        Place(footer.rectTransform, new Vector2(1f, 0f), new Vector2(600f, 40f), new Vector2(-330f, 40f));

        // Screen-wide overlays: darkened edges and faint CRT scanlines.
        var vig = MakeImage("Vignette", root, vignette, Color.white);
        Stretch(vig.rectTransform, Vector2.zero);
        var lines = MakeImage("Scanlines", root, scanlines, new Color(0f, 0f, 0f, 0.22f));
        lines.type = Image.Type.Tiled;
        Stretch(lines.rectTransform, Vector2.zero);

        // Options: a dimmed full-screen blocker with a window on top (hidden by default; MainMenu.Start also hides it).
        var panel = MakeImage("OptionsPanel", root, null, new Color(0f, 0f, 0f, 0.72f));
        panel.raycastTarget = true;   // stops clicks reaching the main buttons behind it
        Stretch(panel.rectTransform, Vector2.zero);

        var window = MakeImage("Window", panel.transform, null, new Color(0.06f, 0.065f, 0.075f, 0.98f));
        Place(window.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(760f, 460f), Vector2.zero);
        var bar = MakeImage("TopBar", window.transform, null, Blood);
        bar.rectTransform.anchorMin = new Vector2(0f, 1f);
        bar.rectTransform.anchorMax = new Vector2(1f, 1f);
        bar.rectTransform.offsetMin = new Vector2(0f, -4f);
        bar.rectTransform.offsetMax = Vector2.zero;

        var optTitle = MakeText("OptionsTitle", window.transform, "Options", 48f, Bone);
        optTitle.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
        optTitle.characterSpacing = 20f;
        Place(optTitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(600f, 70f), new Vector2(0f, 160f));

        var volLabel = MakeText("VolumeLabel", window.transform, "MASTER VOLUME", 26f, Dim);
        volLabel.characterSpacing = 12f;
        volLabel.alignment = TextAlignmentOptions.Left;
        Place(volLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(560f, 40f), new Vector2(0f, 60f));
        var volValue = MakeText("VolumeValue", window.transform, "100%", 26f, Bone);
        volValue.alignment = TextAlignmentOptions.Right;
        Place(volValue.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(560f, 40f), new Vector2(0f, 60f));

        var slider = MakeSlider("MasterVolumeSlider", window.transform);
        Place((RectTransform)slider.transform, new Vector2(0.5f, 0.5f), new Vector2(560f, 22f), new Vector2(0f, 10f));
        UnityEventTools.AddPersistentListener(slider.onValueChanged, menu.SetMasterVolume);

        var back = MakeButton("BackButton", window.transform, "Back", new Vector2(0f, -140f));
        UnityEventTools.AddPersistentListener(back.onClick, menu.CloseOptions);
        panel.gameObject.SetActive(false);

        var so = new SerializedObject(menu);
        so.FindProperty("optionsPanel").objectReferenceValue = panel.gameObject;
        so.FindProperty("volumeSlider").objectReferenceValue = slider;
        so.FindProperty("volumeValue").objectReferenceValue = volValue;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, MenuScenePath);
        AddToBuildSettings();
        AssetDatabase.SaveAssets();
        Debug.Log("MainMenuSetup: built " + MenuScenePath + " and set it as build scene 0.");
    }

    // For -batchmode -executeMethod.
    public static void CreateBatch()
    {
        Create();
    }

    private static void AddToBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        scenes.RemoveAll(s => s.path == MenuScenePath);
        scenes.Insert(0, new EditorBuildSettingsScene(MenuScenePath, true));
        if (!scenes.Exists(s => s.path == GameScenePath))
            scenes.Add(new EditorBuildSettingsScene(GameScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void TitleStyle(TextMeshProUGUI t)
    {
        t.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
        t.characterSpacing = 28f;
    }

    // Dark translucent bar with a red edge; hovering floods it red.
    private static Button MakeButton(string name, Transform parent, string label, Vector2 offset)
    {
        var img = MakeImage(name, parent, null, Color.white);   // tint comes from the button's color block
        img.raycastTarget = true;
        Place(img.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(420f, 68f), offset);

        var button = img.gameObject.AddComponent<Button>();
        var colors = button.colors;
        colors.normalColor = new Color(0.07f, 0.07f, 0.08f, 0.6f);
        colors.highlightedColor = new Color(0.55f, 0.07f, 0.06f, 0.9f);
        colors.selectedColor = new Color(0.45f, 0.06f, 0.05f, 0.85f);
        colors.pressedColor = new Color(0.3f, 0.04f, 0.04f, 1f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        var edge = MakeImage("Edge", img.transform, null, Blood);
        edge.rectTransform.anchorMin = Vector2.zero;
        edge.rectTransform.anchorMax = new Vector2(0f, 1f);
        edge.rectTransform.offsetMin = Vector2.zero;
        edge.rectTransform.offsetMax = new Vector2(4f, 0f);

        var text = MakeText("Label", img.transform, label, 30f, Bone);
        text.fontStyle = FontStyles.UpperCase;
        text.characterSpacing = 16f;
        Stretch(text.rectTransform, Vector2.zero);
        return button;
    }

    private static Slider MakeSlider(string name, Transform parent)
    {
        var resources = new DefaultControls.Resources
        {
            standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
            background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
            knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
        };
        var go = DefaultControls.CreateSlider(resources);
        go.name = name;
        go.transform.SetParent(parent, false);
        var slider = go.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;

        Tint(go.transform, "Background", new Color(0.16f, 0.16f, 0.18f));
        Tint(go.transform, "Fill Area/Fill", Blood);
        Tint(go.transform, "Handle Slide Area/Handle", Bone);
        return slider;
    }

    private static void Tint(Transform root, string path, Color color)
    {
        var t = root.Find(path);
        if (t != null && t.TryGetComponent<Image>(out var img)) img.color = color;
    }

    private static Image MakeImage(string name, Transform parent, Sprite sprite, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private static TextMeshProUGUI MakeText(string name, Transform parent, string text, float size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null) t.font = TMP_Settings.defaultFontAsset;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;
        return t;
    }

    private static void Place(RectTransform rt, Vector2 anchor, Vector2 size, Vector2 offset)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.sizeDelta = size;
        rt.anchoredPosition = offset;
    }

    private static void Stretch(RectTransform rt, Vector2 offset)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = offset;
        rt.offsetMax = offset;
    }

    // Scanlines tile, so they need a full-rect, repeating, point-filtered sprite.
    private static Sprite LoadSprite(string file, bool tiled)
    {
        string path = TexDir + file;
        AssetDatabase.ImportAsset(path);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null && importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            if (tiled)
            {
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Point;
            }
            importer.SaveAndReimport();
        }
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null) Debug.LogWarning("MainMenuSetup: couldn't load " + path);
        return sprite;
    }
}
