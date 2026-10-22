using UnityEngine;

// Walks the NPC back and forth between two points, pausing at each end.
// Stops and turns to the player while they're close enough to talk.
[RequireComponent(typeof(NpcInteract))]
public class NpcPatrol : MonoBehaviour
{
    [Tooltip("World positions of the two ends of the walk.")]
    [SerializeField] private Vector3 pointA = new Vector3(-14.09f, 0f, 28.5f);
    [SerializeField] private Vector3 pointB = new Vector3(-14.09f, 0f, 44.5f);
    [SerializeField] private float walkSpeed = 0.6f;
    [SerializeField] private float turnSpeed = 180f;
    [SerializeField] private float pauseAtEnds = 2f;

    private NpcInteract npc;
    private bool towardB;
    private float pauseUntil;

    private void Awake() { npc = GetComponent<NpcInteract>(); }

    // Play Mode Options skips the reload, so these keep their last-session values.
    private void OnEnable() { towardB = true; pauseUntil = 0f; }

    private void Update()
    {
        if (npc.Busy)
        {
            if (npc.Player != null) TurnTowards(npc.Player.position - transform.position);
            return;
        }
        if (Time.time < pauseUntil) return;

        Vector3 target = towardB ? pointB : pointA;
        target.y = transform.position.y;
        Vector3 toTarget = target - transform.position;

        if (toTarget.magnitude < 0.05f)
        {
            towardB = !towardB;
            pauseUntil = Time.time + pauseAtEnds;
            return;
        }

        TurnTowards(toTarget);
        transform.position = Vector3.MoveTowards(transform.position, target, walkSpeed * Time.deltaTime);
    }

    private void TurnTowards(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return;
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation, Quaternion.LookRotation(direction), turnSpeed * Time.deltaTime);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(pointA, pointB);
        Gizmos.DrawWireSphere(pointA, 0.3f);
        Gizmos.DrawWireSphere(pointB, 0.3f);
    }
}
