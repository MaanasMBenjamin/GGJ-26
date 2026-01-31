
using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    [SerializeField] private float maximumRadius = 10f;
    [SerializeField] private float triggerRadius = 5f;
    [SerializeField] private float dangerRadius = 1f;
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float chaseSpeed = 4f;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform[] patrolPoints;

    private NavMeshAgent agent;
    private int currentPatrolPointIndex;
    private bool isChasingPlayer;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError("NavMeshAgent not found on Enemy.");
            enabled = false;
            return;
        }
        agent.speed = patrolSpeed;
        agent.stoppingDistance = 0.1f;
        if (patrolPoints.Length > 0)
        {
            agent.SetDestination(patrolPoints[currentPatrolPointIndex].position);
        }
        else
        {
            Debug.LogWarning("No patrol points assigned to Enemy. Enemy will remain idle.");
        }
    }

    void Update()
    {
        if (playerTransform == null)
        {
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer <= dangerRadius)
        {
            Debug.Log("Player Failed: Entered Danger Radius!");
        }
        else if (distanceToPlayer <= triggerRadius)
        {
            isChasingPlayer = true;
            agent.speed = chaseSpeed;
            agent.SetDestination(playerTransform.position);
        }
        else if (isChasingPlayer && distanceToPlayer > maximumRadius)
        {
            isChasingPlayer = false;
            agent.speed = patrolSpeed;
            if (patrolPoints.Length > 0)
            {
                agent.SetDestination(patrolPoints[currentPatrolPointIndex].position);
            }
        }

        if (!isChasingPlayer)
        {
            Patrol();
        }
    }

    void Patrol()
    {
        if (patrolPoints.Length == 0)
        {
            return;
        }

        if (!agent.pathPending && agent.remainingDistance < agent.stoppingDistance)
        {
            currentPatrolPointIndex = (currentPatrolPointIndex + 1) % patrolPoints.Length;
            agent.SetDestination(patrolPoints[currentPatrolPointIndex].position);
        }
    }
}
