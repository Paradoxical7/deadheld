using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class PuzzleDoor : MonoBehaviour
{
    [SerializeField] private Collider blocker;
    [SerializeField] private NavMeshObstacle obstacle;
    [SerializeField] private Vector3 slideOffset = new Vector3(0f, 0f, -2.05f);
    [SerializeField] private float slideTime = 1f;
    [SerializeField] private float closeDelay = 4f;
    [SerializeField] private float senseRadius = 3f;

    private Vector3 closedPos;
    private Vector3 openPos;
    private bool unlocked;
    private bool isOpen;
    private float emptyTimer;
    private Transform player;
    private Coroutine moving;

    private void Start()
    {
        closedPos = transform.position;
        openPos = closedPos + slideOffset;

        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        if (GameManager.Instance != null && GameManager.Instance.doorUnlocked)
            unlocked = true;
    }

    private void Update()
    {
        if (Debug.isDebugBuild)
        {
            var kb = Keyboard.current;
            if (kb != null && kb.f9Key.wasPressedThisFrame && !unlocked)
            {
                if (GameManager.Instance != null) GameManager.Instance.doorUnlocked = true;
                Open();
            }
        }

        if (!isOpen || player == null) return;

        bool inDoorway = Vector3.Distance(player.position, closedPos) <= senseRadius;
        if (inDoorway)
        {
            emptyTimer = 0f;
            return;
        }

        emptyTimer += Time.deltaTime;
        if (emptyTimer >= closeDelay) SetOpen(false);
    }

    public void Open()
    {
        unlocked = true;
        emptyTimer = 0f;
        if (!isOpen) SetOpen(true);
    }
    
    public bool IsOpen => isOpen;

    public void Close()
    {
        if (isOpen) SetOpen(false);
    }

    private void SetOpen(bool open)
    {
        isOpen = open;
        emptyTimer = 0f;
        if (open) SetBlocked(false);

        if (moving != null) StopCoroutine(moving);
        moving = StartCoroutine(Slide(open ? openPos : closedPos, open));
    }

    private void SetBlocked(bool blocked)
    {
        if (blocker != null) blocker.enabled = blocked;
        if (obstacle != null) obstacle.enabled = blocked;
    }

    private IEnumerator Slide(Vector3 end, bool open)
    {
        Vector3 start = transform.position;

        for (float t = 0f; t < slideTime; t += Time.deltaTime)
        {
            transform.position = Vector3.Lerp(start, end, t / slideTime);
            yield return null;
        }
        transform.position = end;

        if (!open) SetBlocked(true);
    }
}