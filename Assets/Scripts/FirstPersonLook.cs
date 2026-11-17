using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

// First-person exploration view. Lives on a camera parented to the player at head height:
// mouse X turns the player, mouse Y tilts this camera, and TankController walks relative to it.
// While enabled it switches every other camera off and suspends CameraZone; disabling it hands the view back.
[RequireComponent(typeof(Camera))]
public class FirstPersonLook : MonoBehaviour
{
    [SerializeField] private float mouseSensitivity = 0.1f;   // degrees per pixel of mouse movement
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;

    // The prompts ask for this to know which camera to project through.
    public static FirstPersonLook Active { get; private set; }
    public Camera Camera => cam;

    private Camera cam;
    private AudioListener listener;
    private TankController player;
    private float pitch;
    private readonly List<Camera> hiddenCameras = new List<Camera>();
    private readonly List<AudioListener> hiddenListeners = new List<AudioListener>();
    private readonly List<(Renderer renderer, ShadowCastingMode mode)> hiddenBody = new List<(Renderer, ShadowCastingMode)>();

    // Play Mode Options skips the reload, so statics keep their last-session values.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetActive() => Active = null;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        listener = GetComponent<AudioListener>();
        player = GetComponentInParent<TankController>();
    }

    private void OnEnable()
    {
        Active = this;
        CameraZone.Suspended = true;
        cam.enabled = true;
        if (listener != null) listener.enabled = true;

        pitch = 0f;
        transform.localRotation = Quaternion.identity;
        if (player != null) player.lookBasis = transform;

        HideOtherCameras();
        HideBody();
        LockCursor();
    }

    // Again in Start: CameraZone.Awake picks the scene's starting zone camera, and that can run after our OnEnable.
    private void Start() => HideOtherCameras();

    private void OnDisable()
    {
        if (Active == this) Active = null;
        CameraZone.Suspended = false;
        cam.enabled = false;
        if (listener != null) listener.enabled = false;
        if (player != null) player.lookBasis = null;

        foreach (var c in hiddenCameras) if (c != null) c.enabled = true;
        foreach (var l in hiddenListeners) if (l != null) l.enabled = true;
        hiddenCameras.Clear();
        hiddenListeners.Clear();
        foreach (var (r, mode) in hiddenBody) if (r != null) r.shadowCastingMode = mode;
        hiddenBody.Clear();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update()
    {
        // Escape in the editor drops the lock; a click takes it back. Not while the game is paused (inventory) or the player is frozen.
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            if (CanLook() && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) LockCursor();
            return;
        }
        if (!CanLook()) return;

        var mouse = Mouse.current;
        if (mouse == null) return;

        Vector2 delta = mouse.delta.ReadValue() * mouseSensitivity;
        pitch = Mathf.Clamp(pitch - delta.y, minPitch, maxPitch);
        transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        if (player != null) player.transform.Rotate(0f, delta.x, 0f, Space.World);
    }

    // Dialogue, keypad and pod inspect freeze the player by disabling TankController; the view freezes with it.
    private bool CanLook() => Time.timeScale > 0f && (player == null || player.enabled);

    private static void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Exactly one camera and one AudioListener: ours. Covers the Main Camera and any zone camera left on.
    private void HideOtherCameras()
    {
        foreach (var c in FindObjectsByType<Camera>())
        {
            if (c == cam || !c.enabled) continue;
            c.enabled = false;
            hiddenCameras.Add(c);
        }
        foreach (var l in FindObjectsByType<AudioListener>())
        {
            if (l == listener || !l.enabled) continue;
            l.enabled = false;
            hiddenListeners.Add(l);
        }
    }

    // The head camera sits inside the character model; keep its shadow but don't draw it. Leaves the held lighter visible.
    private void HideBody()
    {
        if (player == null || hiddenBody.Count > 0) return;
        foreach (var r in player.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            hiddenBody.Add((r, r.shadowCastingMode));
            r.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
        }
    }
}
