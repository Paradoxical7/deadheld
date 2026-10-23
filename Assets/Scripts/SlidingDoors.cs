using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class SlidingDoors : MonoBehaviour
{
    [Header("Door Panels")]
    [SerializeField] private Transform leftDoor;
    [SerializeField] private Transform rightDoor;

    [Header("Door Settings")]
    [SerializeField] private float slideDistance = 1f;
    [SerializeField] private float slideSpeed = 2f;
    [SerializeField] private float closingDelay = 1.5f;

    [Header("Keypad")]
    [SerializeField] private bool requiresUnlock = false;
    [SerializeField] private bool openByProximity = true;
    [Tooltip("Which GameManager flag unlocks this door.")]
    [SerializeField] private UnlockFlag unlockFlag = UnlockFlag.RoomOneDoor;

    public enum UnlockFlag { RoomOneDoor, MainHub }

    private Vector3 leftClosed;
    private Vector3 rightClosed;
    private Vector3 leftOpen;
    private Vector3 rightOpen;

    private readonly HashSet<Collider> nearby = new();
    private float lastOccupiedTime;
    private bool heldOpen;
    private bool sawPlayer;
    public bool IsOpen => heldOpen;
    public bool IsUnlocked => Unlocked;

    public void Open() { heldOpen = true; }
    public void Close() { heldOpen = false; }

    private bool Unlocked => !requiresUnlock || FlagSet();

    private bool FlagSet()
    {
        var gm = GameManager.Instance;
        if (gm == null) return false;
        return unlockFlag == UnlockFlag.MainHub ? gm.hubUnlocked : gm.doorUnlocked;
    }

    private void Start()
    {
        if (leftDoor != null)
        {
            leftClosed = leftDoor.localPosition;
            leftOpen = leftClosed + Vector3.left * slideDistance;
        }
        if (rightDoor != null)
        {
            rightClosed = rightDoor.localPosition;
            rightOpen = rightClosed + Vector3.right * slideDistance;
        }

        lastOccupiedTime = -closingDelay;
    }

    private void Update()
    {
        if (requiresUnlock && Debug.isDebugBuild)
        {
            var kb = Keyboard.current;
            if (kb != null && kb.f8Key.wasPressedThisFrame && GameManager.Instance != null)
            {
                if (unlockFlag == UnlockFlag.MainHub) GameManager.Instance.hubUnlocked = true;
                else GameManager.Instance.doorUnlocked = true;
            }
        }

        nearby.RemoveWhere(c => c == null ||
            !c.enabled || !c.gameObject.activeInHierarchy);

        if (nearby.Count > 0)
            lastOccupiedTime = Time.time;



        if (nearby.Count > 0) sawPlayer = true;
        else if (heldOpen && sawPlayer
                 && Time.time - lastOccupiedTime > closingDelay)
        {
            heldOpen = false;
            sawPlayer = false;
        }


        bool wantsOpen = heldOpen || (openByProximity && (nearby.Count > 0 ||
            Time.time - lastOccupiedTime < closingDelay));
        bool shouldOpen = Unlocked && wantsOpen;

        if (leftDoor != null)
            leftDoor.localPosition = Vector3.MoveTowards(
                leftDoor.localPosition,
                shouldOpen ? leftOpen : leftClosed,
                slideSpeed * Time.deltaTime
            );

        if (rightDoor != null)
            rightDoor.localPosition = Vector3.MoveTowards(
                rightDoor.localPosition,
                shouldOpen ? rightOpen : rightClosed,
                slideSpeed * Time.deltaTime
            );
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsAllowed(other))
            nearby.Add(other);
    }

    private void OnTriggerStay(Collider other)
    {
        if (IsAllowed(other))
            nearby.Add(other);
    }

    private void OnTriggerExit(Collider other)
    {
        nearby.Remove(other);
    }

    private bool IsAllowed(Collider other)
    {
        Transform root = other.transform.root;

        return root.CompareTag("Player") ||
               root.CompareTag("Enemy");
    }
}