using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class NpcInteract : MonoBehaviour
{
    [SerializeField] private GameObject prompt;
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private ZoneMapUI zoneMap;
    [SerializeField] private float promptHeight = 2.1f;

    [SerializeField, TextArea] private string[] lines =
    {
        "You're awake... good. Not many come out of cryo anymore.",
        "The Main Hub is sealed. I've patched my access into its doors - they'll open for you now.",
        "Take this map of Zone 1. Press M to look at it.",
        "And stay out of the dark corners."
    };
    [SerializeField, TextArea] private string repeatLine = "The Hub is open. Check your map with M. Go.";

    private bool playerInside;
    private bool talking;
    private int lineIndex;
    private string[] current;
    private TankController player;

    // NpcPatrol stops walking while the player is close or talking.
    public bool Busy => playerInside || talking;
    public Transform Player => player != null ? player.transform : null;

    // Play Mode Options skips the reload, so these keep their last-session values.
    private void OnEnable() { playerInside = false; talking = false; lineIndex = 0; }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInside = true;
        player = other.GetComponentInParent<TankController>();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) playerInside = false;
    }

    private void Update()
    {
        if (prompt != null)
        {
            bool show = playerInside && !talking;
            prompt.SetActive(show);
            if (show) PlacePrompt();
        }

        var kb = Keyboard.current;
        if (kb == null) return;

        if (talking)
        {
            if (kb.eKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame) NextLine();
            else if (kb.escapeKey.wasPressedThisFrame) EndTalk(false);
        }
        else if (playerInside && kb.eKey.wasPressedThisFrame)
        {
            StartTalk();
        }
    }

    private void StartTalk()
    {
        bool alreadyHelped = GameManager.Instance != null && GameManager.Instance.hubUnlocked;
        current = alreadyHelped ? new[] { repeatLine } : lines;
        lineIndex = 0;
        talking = true;
        dialogueText.text = current[0];
        dialoguePanel.SetActive(true);
        LockPlayer(true);
    }

    private void NextLine()
    {
        lineIndex++;
        if (lineIndex < current.Length) dialogueText.text = current[lineIndex];
        else EndTalk(true);
    }

    // Leaving early with Escape doesn't hand anything over; the player has to hear him out.
    private void EndTalk(bool finished)
    {
        talking = false;
        dialoguePanel.SetActive(false);
        LockPlayer(false);

        var gm = GameManager.Instance;
        if (!finished || gm == null || gm.hubUnlocked) return;

        gm.UnlockHubAndGiveMap();
        if (zoneMap != null) zoneMap.AnnounceMap();
    }

    private void PlacePrompt()
    {
        Camera cam = ViewCamera();
        if (cam == null) return;
        Vector3 screen = cam.WorldToScreenPoint(transform.position + Vector3.up * promptHeight);
        if (screen.z < 0f) { prompt.SetActive(false); return; }
        prompt.transform.position = screen;
    }

    // The fixed zone cameras sit above the Main Camera by depth, so the deepest enabled one is what's on screen.
    private static Camera ViewCamera()
    {
        Camera best = null;
        foreach (var c in Camera.allCameras)
            if (best == null || c.depth > best.depth) best = c;
        return best;
    }

    private void LockPlayer(bool locked)
    {
        if (player == null) return;
        player.enabled = !locked;

        if (locked)
        {
            var rb = player.GetComponent<Rigidbody>();
            rb.linearVelocity = Vector3.zero;
            var anim = player.GetComponentInChildren<Animator>();
            if (anim != null) anim.SetFloat("Speed", 0f);
        }
    }
}
