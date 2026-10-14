using UnityEngine;

public class EnemySight : MonoBehaviour
{
    public float viewRadius = 8f;        // how far the enemy can see
    public float viewAngle = 90f;        // width of the vision cone, in degrees
    public LayerMask obstacleMask;       // what blocks line of sight (walls)

    public Transform player;             // found automatically by tag
    public bool canSeePlayer;            // true when player is currently visible

    void Start()
    {
        GameObject p = GameObject.FindWithTag("Player");
        if (p != null) player = p.transform;
    }

    void Update()
    {
        canSeePlayer = CheckForPlayer();
    }

        bool CheckForPlayer()
    {
        if (player == null) return false;

        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        float distToPlayer = Vector3.Distance(transform.position, player.position);

        if (distToPlayer > viewRadius) return false;

        float angle = Vector3.Angle(transform.forward, dirToPlayer);
        if (angle > viewAngle / 2f) return false;

        if (Physics.Raycast(transform.position, dirToPlayer, distToPlayer, obstacleMask))
            return false;

        return true;
    }
}