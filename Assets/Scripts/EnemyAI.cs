using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    public Transform[] patrolPoints;
    public float catchDistance = 1.5f;   // how close counts as "reached" the player

    private NavMeshAgent agent;
    private EnemySight sight;             // reference to the sight script on this enemy
    private int currentPoint = 0;
    private bool combatStarted = false;  // so we only fire the stub once

    private enum State { Patrol, Chase }   // the two behaviors
    private State state = State.Patrol;    // start out patrolling

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        sight = GetComponent<EnemySight>();     // grab the sight component on the same object

        if (patrolPoints.Length > 0)
            agent.SetDestination(patrolPoints[0].position);

        if (sight == null)
            Debug.LogWarning("EnemyAI: no EnemySight component found, enemy will only patrol.");
    }

        void Update()
    {
        // no player found yet, just patrol and skip the rest
        if (sight == null || sight.player == null)
        {
            Patrol();
            return;
        }

        if (sight.canSeePlayer)
            state = State.Chase;
        else
            state = State.Patrol;

        if (state == State.Chase)
            Chase();
        else
            Patrol();
    }

    void Patrol()
    {
        if (patrolPoints.Length == 0) return;   // nothing to patrol, bail safely

        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            currentPoint = (currentPoint + 1) % patrolPoints.Length;
            agent.SetDestination(patrolPoints[currentPoint].position);
        }
    }

    void Chase()
    {
        agent.SetDestination(sight.player.position);  // walk straight at the player

        float dist = Vector3.Distance(transform.position, sight.player.position);
        if (dist <= catchDistance && !combatStarted)
        {
            StartCombat();
        }
    }

    void StartCombat()
    {
        combatStarted = true;
        Debug.Log("START COMBAT");   // stub: real turn-based transition hooks in here later
    }
}