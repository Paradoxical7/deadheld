using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class EnemyAI : MonoBehaviour
{
    public Transform[] patrolPoints;
    public float catchDistance = 1.5f;   // how close counts as "reached" the player

    private NavMeshAgent agent;
    private EnemySight sight;             // reference to the sight script on this enemy
    private int currentPoint = 0;
    private bool combatStarted = false;  // so we only fire the stub once
    public float spawnGraceTime = 3f;    // after (re)spawn, combat won't trigger for this long (lets you run after a loss)
    private float graceTimer = 0f;
    public float leashRange = 15f;       // how far from its post it will chase before giving up
    private Vector3 homePos;             // where it started / the area it guards

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

        graceTimer = spawnGraceTime;
        homePos = transform.position;
    }

        void Update()
    {
        // no player found yet, just patrol and skip the rest
        if (sight == null || sight.player == null)
        {
            Patrol();
            return;
        }

        // count down the post-spawn grace window
        if (graceTimer > 0f) graceTimer -= Time.deltaTime;

        // during grace (e.g. just respawned after a loss) ignore the player and back off,
        // so you get a real chance to run instead of being instantly re-caught
        if (graceTimer > 0f)
        {
            ReturnOrPatrol();
            return;
        }

        // proximity trigger: touching the enemy starts combat even if it can't "see" you
        float distToPlayer = Vector3.Distance(transform.position, sight.player.position);
        if (distToPlayer <= catchDistance && !combatStarted && graceTimer <= 0f)
        {
            StartCombat();
            return;
        }

        // leash: only chase while within range of its post; otherwise return home and patrol
        float distFromHome = Vector3.Distance(transform.position, homePos);
        if (sight.canSeePlayer && distFromHome <= leashRange)
        {
            state = State.Chase;
            Chase();
        }
        else
        {
            state = State.Patrol;
            ReturnOrPatrol();
        }
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

    void ReturnOrPatrol()
    {
        // if we chased out past our leash, walk back to our post before patrolling again
        if (Vector3.Distance(transform.position, homePos) > 2f)
        {
            agent.SetDestination(homePos);
            return;
        }
        Patrol();
    }

    void Chase()
    {
        agent.SetDestination(sight.player.position);  // walk straight at the player

        float dist = Vector3.Distance(transform.position, sight.player.position);
        if (dist <= catchDistance && !combatStarted && graceTimer <= 0f)
        {
            StartCombat();
        }
    }

    void StartCombat()
    {
        combatStarted = true;

        // read this enemy's identity and hand it to the GameManager
        EnemyIdentity identity = GetComponent<EnemyIdentity>();
        if (identity != null && GameManager.Instance != null)
        {
            GameManager.Instance.SetPendingEnemy(identity.role, identity.stage);
        }

        EnemyID enemyId = GetComponent<EnemyID>();
        if (enemyId != null && GameManager.Instance != null)
            GameManager.Instance.pendingEnemyId = enemyId.id;

        // remember where to come back to, and where the player was standing
        if (GameManager.Instance != null)
            GameManager.Instance.SetReturnPoint(SceneManager.GetActiveScene().name, sight.player.position);

        SceneManager.LoadScene("TurnBasedScene");
    }
}