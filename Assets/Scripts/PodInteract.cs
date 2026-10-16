using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PodInteract : MonoBehaviour
{
    [SerializeField] private Pod pod;
    [SerializeField] private GameObject prompt;
    [SerializeField] private Camera inspectCamera;
    [SerializeField] private GameObject readPrompt;
    [SerializeField] private GameObject notePanel;

    private bool playerInside;
    private bool inspecting;
    private bool noteOpen;
    private TankController player;
    private readonly List<Camera> hiddenCameras = new List<Camera>();

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
        bool canInspect = playerInside && pod.State == Pod.PodState.Pulse;

        if (prompt != null) prompt.SetActive(canInspect && !inspecting);

        var kb = Keyboard.current;
        if (kb == null) return;

        if (inspecting)
        {
            if (readPrompt != null) readPrompt.SetActive(!noteOpen);

            if (kb.eKey.wasPressedThisFrame && !noteOpen) OpenNote();
            else if (kb.eKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame) EndInspect();
        }
        else if (canInspect && kb.eKey.wasPressedThisFrame)
        {
            StartInspect();
        }
    }

    private void StartInspect()
    {
        inspecting = true;
        LockPlayer(true);

        hiddenCameras.Clear();
        foreach (var cam in Camera.allCameras)
        {
            if (cam == inspectCamera) continue;
            hiddenCameras.Add(cam);
            cam.enabled = false;
        }
        inspectCamera.enabled = true;
    }

    private void EndInspect()
    {
        noteOpen = false;
        if (notePanel != null) notePanel.SetActive(false);
        if (readPrompt != null) readPrompt.SetActive(false);
        inspectCamera.enabled = false;
        foreach (var cam in hiddenCameras) cam.enabled = true;
        hiddenCameras.Clear();

        LockPlayer(false);
        inspecting = false;
    }

    private void OpenNote()
    {
        noteOpen = true;
        if (readPrompt != null) readPrompt.SetActive(false);
        if (notePanel != null) notePanel.SetActive(true);
        if (GameManager.Instance != null) GameManager.Instance.MarkCodeFound();
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