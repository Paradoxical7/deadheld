using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;   // the one global instance

    [Header("Enemy that triggered combat")]
    public EnemyIdentity.EnemyRole pendingEnemyRole;
    public EnemyIdentity.EnemyStage pendingEnemyStage;
    public bool hasPendingEnemy = false;

    public string pendingEnemyId;                 // which enemy triggered combat
    private HashSet<string> defeatedEnemies = new HashSet<string>();

    public enum Outcome { None, Won, Lost }
    public Outcome lastOutcome = Outcome.None;

    [Header("Room 01 puzzle")]
    public bool codeFound = false;
    public bool doorUnlocked = false;
    public string puzzleCode;                     // generated once per run

    [Header("Zone 1 progress")]
    public bool hubUnlocked = false;              // set by the guide NPC; opens the Main Hub doors
    public bool hasZoneMap = false;               // player can view the Zone 1 map (M)

    [Header("Battle return")]
    public string returnScene;            // scene to come back to after a battle
    public Vector3 returnPosition;        // where the player stood when combat started
    public bool hasReturnPoint = false;

    [Header("Player stats (persist across scenes)")]
    public float maxHP = 100f;                    // matches the battle player's baseHP
    public float currentHP = 100f;
    [Range(0f, 1f)] public float lossRecoverFraction = 0.25f;   // HP you come back with after losing a fight

    [Header("Exploration stamina (separate from battle NRG)")]
    public float maxEnergy = 100f;
    public float currentEnergy = 100f;
    public float sprintDrainPerSecond = 20f;
    public float energyRegenPerSecond = 12f;
    public float regenDelay = 0.75f;              // pause after sprinting before energy refills
    public float sprintResumeEnergy = 15f;        // after running dry, wait for this much before sprinting again
    private bool sprintExhausted;
    private float lastSprintTime;

    [Header("Parasite meter (granted by the guide NPC)")]
    public bool parasiteActive = false;
    public float parasiteMax = 900f;              // seconds of life when full (15 min)
    public float parasiteCurrent = 900f;
    public float parasiteDrainPerSecond = 1f;     // 1 = the meter counts down in real seconds
    public float parasiteRestorePerKill = 120f;   // time bought back per fight won
    public string battleSceneName = "TurnBasedScene";   // the meter is paused while this scene is active
    public string mainMenuScene = "MainMenu";
    public float deathScreenSeconds = 3f;
    private bool dying;

    void Awake()
    {
        // singleton: keep only one, and survive scene loads
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        puzzleCode = Random.Range(0, 10000).ToString("D4");
        ResetPlayerStats();
    }

    // fresh run: full health and stamina
    public void ResetPlayerStats()
    {
        currentHP = maxHP;
        currentEnergy = maxEnergy;
        sprintExhausted = false;
    }

    // called by the battle scene when a fight ends
    public void SetHPFromBattle(float remainingHP, bool lost)
    {
        currentHP = lost
            ? Mathf.Max(1f, maxHP * lossRecoverFraction)   // no game over yet, so a loss sends you back weakened
            : Mathf.Clamp(remainingHP, 1f, maxHP);
        Debug.Log("GameManager: player HP after battle " + currentHP + "/" + maxHP);
    }

    // called every frame by the exploration controller; returns whether the player may sprint this frame
    public bool TickSprint(bool wantsToSprint, float deltaTime)
    {
        if (currentEnergy <= 0f) sprintExhausted = true;
        else if (sprintExhausted && currentEnergy >= sprintResumeEnergy) sprintExhausted = false;

        bool sprinting = wantsToSprint && !sprintExhausted;
        if (sprinting)
        {
            currentEnergy = Mathf.Max(0f, currentEnergy - sprintDrainPerSecond * deltaTime);
            lastSprintTime = Time.time;
        }
        else if (Time.time - lastSprintTime >= regenDelay)
        {
            currentEnergy = Mathf.Min(maxEnergy, currentEnergy + energyRegenPerSecond * deltaTime);
        }
        return sprinting;
    }

    void OnDestroy()
    {
        // unsubscribing a handler that was never added is harmless, and a dying GameManager clears Instance first
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (dying) Time.timeScale = 1f;
    }

    void Update()
    {
        if (!parasiteActive || dying) return;
        if (SceneManager.GetActiveScene().name == battleSceneName) return;   // the parasite waits out battles

        parasiteCurrent = Mathf.Max(0f, parasiteCurrent - parasiteDrainPerSecond * Time.deltaTime);
        if (parasiteCurrent <= 0f) StartCoroutine(ParasiteDeath());
    }

    // the guide NPC hands the parasite over: the meter starts full and begins draining
    public void ActivateParasite()
    {
        parasiteActive = true;
        parasiteCurrent = parasiteMax;
        Debug.Log("GameManager: parasite meter active (" + parasiteMax + "s)");
    }

    private System.Collections.IEnumerator ParasiteDeath()
    {
        dying = true;
        Debug.Log("GameManager: parasite meter hit zero - the player is dead");

        ShowDeathScreen();
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(deathScreenSeconds);
        Time.timeScale = 1f;

        // Fresh run: drop this GameManager so the one in the level scene starts clean
        // (parasite off, full HP, puzzles and enemies reset).
        Instance = null;
        SceneManager.LoadScene(mainMenuScene);
        Destroy(gameObject);
    }

    // A plain "they got you" card; it lives under this object, so it goes when the GameManager does.
    private void ShowDeathScreen()
    {
        var canvasGO = new GameObject("ParasiteDeathScreen", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler));
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        var scaler = canvasGO.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        var bg = new GameObject("Black", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        bg.transform.SetParent(canvasGO.transform, false);
        var bgRect = (RectTransform)bg.transform;
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        bg.GetComponent<UnityEngine.UI.Image>().color = new Color(0.02f, 0f, 0f, 0.96f);

        DeathText(canvasGO.transform, "THEY GOT YOU", 110f, new Color(0.75f, 0.08f, 0.06f), 40f);
        DeathText(canvasGO.transform, "the parasite took over", 32f, new Color(0.55f, 0.55f, 0.55f), -60f);
    }

    private static void DeathText(Transform parent, string text, float size, Color color, float y)
    {
        var go = new GameObject("Text", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(1600f, 160f);
        rt.anchoredPosition = new Vector2(0f, y);
        var t = go.AddComponent<TMPro.TextMeshProUGUI>();
        if (TMPro.TMP_Settings.defaultFontAsset != null) t.font = TMPro.TMP_Settings.defaultFontAsset;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = TMPro.TextAlignmentOptions.Center;
        t.characterSpacing = 12f;
        t.raycastTarget = false;
    }

    // after returning from a battle, drop the player back where they were
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!hasReturnPoint) return;
        if (!string.IsNullOrEmpty(returnScene) && scene.name != returnScene) return;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            CharacterController cc = p.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;        // CC fights direct position sets
            p.transform.position = returnPosition;
            Rigidbody rb = p.GetComponent<Rigidbody>();
            if (rb != null) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
            if (cc != null) cc.enabled = true;
        }
        hasReturnPoint = false;
    }

    public void SetReturnPoint(string sceneName, Vector3 pos)
    {
        returnScene = sceneName;
        returnPosition = pos;
        hasReturnPoint = true;
    }

    public void SetPendingEnemy(EnemyIdentity.EnemyRole role, EnemyIdentity.EnemyStage stage)
    {
        pendingEnemyRole = role;
        pendingEnemyStage = stage;
        hasPendingEnemy = true;
        Debug.Log("GameManager: battle queued vs " + role + " (" + stage + ")");
    }

    // called once per won fight (BattleOutcome); also buys back parasite time
    public void MarkEnemyDefeated(string id)
    {
        if (parasiteActive)
            parasiteCurrent = Mathf.Min(parasiteMax, parasiteCurrent + parasiteRestorePerKill);

        if (!string.IsNullOrEmpty(id))
            defeatedEnemies.Add(id);
    }

    public bool IsEnemyDefeated(string id)
    {
        return !string.IsNullOrEmpty(id) && defeatedEnemies.Contains(id);
    }

    public void MarkCodeFound()
    {
        codeFound = true;
        Debug.Log("GameManager: puzzle code found");
    }

    public void UnlockHubAndGiveMap()
    {
        hubUnlocked = true;
        hasZoneMap = true;
        Debug.Log("GameManager: Main Hub unlocked, Zone 1 map given");
    }

    public bool TryUnlockDoor(string entered)
    {
        if (entered != puzzleCode) return false;
        doorUnlocked = true;
        return true;
    }
}
