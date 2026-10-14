
using UnityEngine;
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

    private Vector3 leftClosed;
    private Vector3 rightClosed;
    private Vector3 leftOpen;
    private Vector3 rightOpen;

    private readonly HashSet<Collider> nearby = new();
    private float lastOccupiedTime;
   
    private void Start()
    {
        leftClosed = leftDoor.localPosition;
        rightClosed = rightDoor.localPosition;

        leftOpen = leftClosed + Vector3.left * slideDistance;
        rightOpen = rightClosed + Vector3.right * slideDistance;

        lastOccupiedTime = -closingDelay;
    }

    private void Update()
    {
        nearby.RemoveWhere(c => c == null ||
            !c.enabled || !c.gameObject.activeInHierarchy);

        if (nearby.Count > 0)
            lastOccupiedTime = Time.time;

        bool shouldOpen = nearby.Count > 0 ||
            Time.time - lastOccupiedTime < closingDelay;

        leftDoor.localPosition = Vector3.MoveTowards(
            leftDoor.localPosition,
            shouldOpen ? leftOpen : leftClosed,
            slideSpeed * Time.deltaTime
        );

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


