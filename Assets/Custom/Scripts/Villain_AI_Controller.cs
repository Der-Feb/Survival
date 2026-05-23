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
    
    // INCREASED: Expanded radius to comfortably clear physical boundaries of colliding capsules
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

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        agent.speed = walkSpeed;

        // FIX: Configure stopping distance dynamically to prevent physical collision push locks
        agent.stoppingDistance = attackRadius - 0.4f;

        if (showDebugLogs)
        {
            // Debug.Log($"[Tiger Start] Initialized. attackRadius: {attackRadius} | Generated stoppingDistance: {agent.stoppingDistance}");
        }

        if (!agent.isOnNavMesh && showDebugLogs)
        {
            
            // Debug.LogError($"[Tiger] CRITICAL: {gameObject.name} is NOT on a baked NavMesh!");
        }
    }

    void Update()
    {
        // CRITICAL FIX: If the tiger is hitting or roaring, exit IMMEDIATELY.
        // This blocks the background scan and prevents it from ghost-killing nearby rabbits!
        if (isAttacking) 
        {
            // Force speed parameter to 0 so the agent doesn't slide if pushed by physics during a roar
            animator.SetFloat("Speed", 0f);
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
                if (showDebugLogs)
                {
                    Debug.Log($"[Tiger Logic] Proximity target met! Initiating strike!");
                }
                // This safely sets isAttacking = true inside the coroutine, which blocks this entire Update next frame
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

        // Update animation movement parameter
        animator.SetFloat("Speed", agent.velocity.magnitude);
    }

    void FindNearestInteractableRabbit()
    {
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

            if (targetStillExists)
            {
                return; 
            }
            else
            {
                currentTargetObject = null;
            }
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

    // REPLACE your current ExecuteAttackSequence inside Villain_AI_Controller.cs with this:
    IEnumerator ExecuteAttackSequence(InteractableObject target)
    {
        // 1. IMMEDIATE HARD LOCK out of the loop
        isAttacking = true;
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        animator.SetFloat("Speed", 0f);

        string rabbitName = target.GetItemName();
        if (showDebugLogs) Debug.Log($"[Tiger Combat] HARD LOCK active. Striking {rabbitName}...");

        // 2. Play Hit Animation
        animator.SetInteger("ActionTrigger", 1);
        yield return new WaitForSeconds(1.0f); 

        // 3. Deliver the fatal hit context safely
        if (target != null)
        {
            RabbitHealth rabbitHealth = target.GetComponentInChildren<RabbitHealth>();
            if (rabbitHealth != null)
            {
                // Pass the tiger's transform so it knows the attack vector direction
                rabbitHealth.TakeFatalHit(transform);
            }
            else
            {
                // Fallback if component is entirely missing
                Destroy(target.gameObject);
                Rabbits.RabbitHealth.OnRabbitDestroyed?.Invoke();
            }
        }

        // 4. Play Roar Animation (Tiger is STILL locked out of attacking here)
        animator.SetInteger("ActionTrigger", 2);
        yield return new WaitForSeconds(1.5f);

        // 5. CLEAN UP & RELEASE LOCK
        animator.SetInteger("ActionTrigger", 0);
        currentTargetObject = null;
        hadTargetLastFrame = false; 
        
        // Clear out any pending paths so it doesn't instantly snap to a close rabbit
        agent.ResetPath(); 
        agent.isStopped = false;
        
        // Re-enable tracking ONLY after the full sequence is dead and done
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