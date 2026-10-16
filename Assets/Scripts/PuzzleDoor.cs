using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class PuzzleDoor : MonoBehaviour
{
    [SerializeField] private Collider blocker;
    [SerializeField] private NavMeshObstacle obstacle;
    [SerializeField] private Vector3 slideOffset = new Vector3(0f, 0f, -2.05f);
    [SerializeField] private float slideTime = 1f;

    private bool opened;

    private void Start()
    {
        if (GameManager.Instance != null && GameManager.Instance.doorUnlocked)
            OpenInstant();
    }

    public void Open()
    {
        if (opened) return;
        Unblock();
        StartCoroutine(Slide());
    }

    private void OpenInstant()
    {
        if (opened) return;
        Unblock();
        transform.position += slideOffset;
    }

    private void Unblock()
    {
        opened = true;
        if (blocker != null) blocker.enabled = false;
        if (obstacle != null) obstacle.enabled = false;
    }

    private IEnumerator Slide()
    {
        Vector3 start = transform.position;
        Vector3 end = start + slideOffset;

        for (float t = 0f; t < slideTime; t += Time.deltaTime)
        {
            transform.position = Vector3.Lerp(start, end, t / slideTime);
            yield return null;
        }
        transform.position = end;
    }
}