using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;

public class NpcInteract : MonoBehaviour
{
    [SerializeField] private GameObject prompt;
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private ZoneMapUI zoneMap;
    [SerializeField] private float promptHeight = 2.1f;

    [SerializeField, TextArea] private string repeatLine = "The Hub is open. Check your map with M. Go.";
    // Which dialogue.json section the NPC speaks. S3 is Minerva's first meeting.
    [SerializeField] private string section ="S3";

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
        var gm = GameManager.Instance;
        bool alreadyHelped = gm != null && gm.hubUnlocked && gm.parasiteActive;
        if (alreadyHelped) current = new[] { repeatLine };
        else current=BuildLines();
        lineIndex = 0;
        talking = true;
        dialogueText.text = current[0];
        dialoguePanel.SetActive(true);
        LockPlayer(true);
    }

    // Turns the section's lines into "Speaker: text" strings, in order.
    private string[] BuildLines()
    {
        var result=new List<string>();
        foreach(var l in DialogueDatabase.GetSection(section))
        {
            result.Add(l.speaker+ ": " + l.text);
        }
        // Empty section (a typo in "section"): never return an empty list.
        if(result.Count==0) return new[] { repeatLine };


        return result.ToArray();
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
        if (!finished || gm == null) return;

        if (!gm.hubUnlocked)
        {
            gm.UnlockHubAndGiveMap();
            if (zoneMap != null) zoneMap.AnnounceMap();
        }
        // Checked on its own so the meter still starts if the hub was opened another way (e.g. the F8 debug key).
        if (!gm.parasiteActive) gm.ActivateParasite();
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
    // Public so the keypad and pod prompts place themselves with the same camera.
    public static Camera ViewCamera()
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
