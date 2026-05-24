using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using Rabbits;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class Villain_AI_Controller : MonoBehaviour
{
    [Header("Debugging")]
    [SerializeField] private bool showDebugLogs = true;

    [Header("Detection Setup")]
    public string rabbitTag = "Rabbit";
    public float attackRadius = 3.5f; 
    public float scanInterval = 0.3f; 

    [Header("Movement Speeds")]
    public float walkSpeed = 2f;
    public float runSpeed = 10.5f;

    private NavMeshAgent agent;
    private Animator animator;
    private InteractableObject currentTargetObject; 
    
    private bool isAttacking = false;
    private bool hadTargetLastFrame = false;
    private float scanTimer = 0f;

    // PERFORMANCE FIX: Cache Animator Hashes to avoid expensive string lookups under the hood
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int ActionTriggerHash = Animator.StringToHash("ActionTrigger");

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        agent.speed = walkSpeed;

        // Configure stopping distance dynamically to prevent physical collision push locks
        agent.stoppingDistance = attackRadius - 0.4f;

        if (!agent.isOnNavMesh && showDebugLogs)
        {
            Debug.LogError($"[Tiger] CRITICAL: {gameObject.name} is NOT on a baked NavMesh!");
        }
    }

    void Update()
    {
        // If the tiger is hitting or roaring, exit IMMEDIATELY.
        if (isAttacking) 
        {
            animator.SetFloat(SpeedHash, 0f);
            return;
        }

        // 1. Handle detection intervals safely
        scanTimer += Time.deltaTime;
        if (scanTimer >= scanInterval)
        {
            scanTimer = 0f;
            FindNearestInteractableRabbit();
        }

        // 2. Handle chasing and attacking logic
        if (currentTargetObject != null)
        {
            if (!hadTargetLastFrame && showDebugLogs)
            {
                Debug.Log($"[Tiger AI] Target Acquired → '{currentTargetObject.GetItemName()}'");
                hadTargetLastFrame = true;
            }

            Vector3 targetPosition = currentTargetObject.transform.position;
            
            if (agent.isOnNavMesh)
            {
                agent.speed = runSpeed;
                agent.SetDestination(targetPosition);
            }

            // Check if the agent has arrived at its stopping boundary
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                if (showDebugLogs) Debug.Log($"[Tiger Logic] Proximity target met! Initiating strike!");
                
                // CRITICAL BUG FIX: Lock immediately on this frame before starting the coroutine 
                // to prevent multiple overlapping coroutines from firing on consecutive frames.
                isAttacking = true; 
                StartCoroutine(ExecuteAttackSequence(currentTargetObject));
            }
        }
        else
        {
            if (hadTargetLastFrame && showDebugLogs)
            {
                Debug.Log("[Tiger AI] Target lost or no rabbits left. Returning to baseline loop.");
                hadTargetLastFrame = false;
            }

            if (agent.isOnNavMesh)
            {
                agent.speed = walkSpeed;
            }
        }

        // Update animation movement parameter using high-performance hash
        animator.SetFloat(SpeedHash, agent.velocity.magnitude);
    }

    void FindNearestInteractableRabbit()
    {
        // NOTE: For ultimate scalability, replace this line with a call to a dedicated spawn manager
        GameObject[] rawRabbits = GameObject.FindGameObjectsWithTag(rabbitTag);
        
        if (currentTargetObject != null)
        {
            bool targetStillExists = false;
            foreach (GameObject rabbitGo in rawRabbits)
            {
                if (rabbitGo != null && rabbitGo.GetComponent<InteractableObject>() == currentTargetObject)
                {
                    targetStillExists = true;
                    break;
                }
            }

            if (targetStillExists) return; 
            
            currentTargetObject = null;
        }

        float shortestDistance = Mathf.Infinity;
        InteractableObject nearestRabbitObject = null;

        foreach (GameObject rabbitGo in rawRabbits)
        {
            InteractableObject interactable = rabbitGo.GetComponent<InteractableObject>();
            if (interactable == null) continue; 

            float distanceToRabbit = Vector3.Distance(transform.position, rabbitGo.transform.position);

            if (distanceToRabbit < shortestDistance)
            {
                shortestDistance = distanceToRabbit;
                nearestRabbitObject = interactable;
            }
        }

        if (nearestRabbitObject != null)
        {
            currentTargetObject = nearestRabbitObject;
        }
    }

    IEnumerator ExecuteAttackSequence(InteractableObject target)
    {
        // HARD LOCK out of tracking loop
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        animator.SetFloat(SpeedHash, 0f);

        string rabbitName = target != null ? target.GetItemName() : "Unknown Target";
        if (showDebugLogs) Debug.Log($"[Tiger Combat] HARD LOCK active. Striking {rabbitName}...");

        // Play Hit Animation using int hash
        animator.SetInteger(ActionTriggerHash, 1);
        yield return new WaitForSeconds(1.0f); 

        // Deliver the fatal hit context safely
        if (target != null)
        {
            RabbitHealth rabbitHealth = target.GetComponentInChildren<RabbitHealth>();
            if (rabbitHealth != null)
            {
                rabbitHealth.TakeFatalHit(transform);
            }
            else
            {
                Destroy(target.gameObject);
                Rabbits.RabbitHealth.OnRabbitDestroyed?.Invoke();
            }
        }

        // Play Roar Animation 
        animator.SetInteger(ActionTriggerHash, 2);
        yield return new WaitForSeconds(1.5f);

        // CLEAN UP & RELEASE LOCK
        animator.SetInteger(ActionTriggerHash, 0);
        currentTargetObject = null;
        hadTargetLastFrame = false; 
        
        agent.ResetPath(); 
        agent.isStopped = false;
        
        // Re-enable tracking safely at the end of the sequence
        isAttacking = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRadius);
    }

    private void OnEnable()
    {
        RabbitHealth.OnRabbitDestroyed += HandleTargetVaporized;
    }

    private void OnDisable()
    {
        RabbitHealth.OnRabbitDestroyed -= HandleTargetVaporized;
    }

    private void HandleTargetVaporized()
    {
        if (currentTargetObject == null || currentTargetObject.gameObject == null)
        {
            currentTargetObject = null;
            hadTargetLastFrame = false;
        }
    }
}