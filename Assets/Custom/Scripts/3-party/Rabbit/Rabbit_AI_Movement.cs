using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI; // MUST include this for pathfinding

[RequireComponent(typeof(NavMeshAgent))]
public class Rabbit_AI_Movement : MonoBehaviour
{
    Animator animator;
    NavMeshAgent agent; // Control movement via the agent

    public float moveSpeed = 2.0f; // Higher default so it doesn't crawl
    public float wanderRadius = 5f; // How far the rabbit can choose to wander

    float walkTime;
    public float walkCounter;
    float waitTime;
    public float waitCounter;

    public bool isWalking;

    void Start()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();

        // Apply speeds directly to the pathfinding vehicle
        agent.speed = moveSpeed;

        walkTime = Random.Range(3, 6);
        waitTime = Random.Range(5, 7);

        waitCounter = waitTime;
        walkCounter = walkTime;

        ChooseDirection();
    }

    void Update()
    {
        if (isWalking)
        {
            animator.SetBool("isRunning", true);
            walkCounter -= Time.deltaTime;

            // Enforce that the agent stays active and moving
            agent.isStopped = false;

            if (walkCounter <= 0)
            {
                isWalking = false;
                agent.isStopped = true; // Safely stop NavMesh agent pathing
                agent.velocity = Vector3.zero; // Kill momentum instantly
                animator.SetBool("isRunning", false);
                waitCounter = waitTime;
            }
        }
        else
        {
            waitCounter -= Time.deltaTime;

            if (waitCounter <= 0)
            {
                ChooseDirection();
            }
        }
    }

    public void ChooseDirection()
    {
        // Calculate a safe, random point directly ON the baked NavMesh floor
        Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
        randomDirection += transform.position;
        
        NavMeshHit hit;
        // Sample within radius to guarantee the coordinate is reachable
        if (NavMesh.SamplePosition(randomDirection, out hit, wanderRadius, 1))
        {
            agent.SetDestination(hit.position);
        }

        isWalking = true;
        walkCounter = walkTime;
    }
}