using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class Villain_AI_Controller : MonoBehaviour
{
    [Header("Detection Setup")]
    public string rabbitTag = "Rabbit"; // Make sure your rabbit prefabs have this tag!
    public float detectionRadius = 15f;
    public float attackRadius = 2f;

    [Header("Movement Speeds")]
    public float walkSpeed = 2f;
    public float runSpeed = 5.5f;

    private NavMeshAgent agent;
    private Animator animator;
    private Transform currentTarget;
    private bool isAttacking = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        
        // Start by patrolling or idling at normal walk speed
        agent.speed = walkSpeed;
    }

    void Update()
    {
        // Don't interrupt the attack/sound sequence with movement logic
        if (isAttacking) return;

        FindNearestRabbit();

        if (currentTarget != null)
        {
            float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);

            if (distanceToTarget <= attackRadius)
            {
                // We reached the rabbit! Trigger the attack chain
                StartCoroutine(ExecuteAttackSequence());
            }
            else
            {
                // Chase the rabbit at full run speed
                agent.speed = runSpeed;
                agent.SetDestination(currentTarget.position);
            }
        }
        else
        {
            // No rabbits found, slow down to idle/walk pace
            agent.speed = walkSpeed;
            // Optional: Insert your patrol point logic here if desired
        }

        // Update the Animator speed parameter based on actual NavMesh movement velocity
        float currentVelocity = agent.velocity.magnitude;
        animator.SetFloat("Speed", currentVelocity);
    }

    void FindNearestRabbit()
    {
        GameObject[] rabbits = GameObject.FindGameObjectsWithTag(rabbitTag);
        float shortestDistance = Mathf.Infinity;
        Transform nearestRabbit = null;

        foreach (GameObject rabbit in rabbits)
        {
            float distanceToRabbit = Vector3.Distance(transform.position, rabbit.transform.position);
            if (distanceToRabbit < shortestDistance && distanceToRabbit <= detectionRadius)
            {
                shortestDistance = distanceToRabbit;
                nearestRabbit = rabbit.transform;
            }
        }

        currentTarget = nearestRabbit;
    }

    IEnumerator ExecuteAttackSequence()
    {
        isAttacking = true;
        agent.isStopped = true; // Stop moving instantly during combat
        animator.SetFloat("Speed", 0f);

        // 1. Trigger the Hit Animation
        animator.SetInteger("ActionTrigger", 1);
        
        // Wait a brief moment for the strike to connect (adjust time to match your clip length)
        yield return new WaitForSeconds(1.0f);

        // 2. Trigger the Sound/Roar Animation right after
        animator.SetInteger("ActionTrigger", 2);

        // Wait for the roar animation loop to completely play out
        yield return new WaitForSeconds(1.5f);

        // 3. Reset the state parameters back to idle
        animator.SetInteger("ActionTrigger", 0);
        currentTarget = null;
        agent.isStopped = false;
        isAttacking = false;
    }

    // Visualizes the detection zones inside the Scene window for easy balancing
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRadius);
    }
}