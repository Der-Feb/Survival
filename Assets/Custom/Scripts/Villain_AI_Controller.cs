using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class Villain_AI_Controller : MonoBehaviour
{
    [Header("Debugging")]
    [SerializeField] private bool showDebugLogs = true;

    [Header("Detection Setup")]
    public string rabbitTag = "Rabbit";
    public float attackRadius = 2f;
    public float scanInterval = 0.3f; // Scan 3 times a second instead of 60+

    [Header("Movement Speeds")]
    public float walkSpeed = 2f;
    public float runSpeed = 5.5f;

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

        if (!agent.isOnNavMesh && showDebugLogs)
            Debug.LogError($"[Tiger] CRITICAL: {gameObject.name} is NOT on a baked NavMesh!");
    }

    void Update()
    {
        if (isAttacking) return;

        // Throttle the global search using the scan interval timer
        scanTimer += Time.deltaTime;
        if (scanTimer >= scanInterval)
        {
            scanTimer = 0f;
            FindNearestInteractableRabbit();
        }

        // If we have an active target from our search, hunt it down!
        if (currentTargetObject != null)
        {
            if (!hadTargetLastFrame && showDebugLogs)
            {
                Debug.Log($"[Tiger AI] Target Acquired → '{currentTargetObject.GetItemName()}'");
                hadTargetLastFrame = true;
            }

            Vector3 targetPosition = currentTargetObject.transform.position;
            float distanceToTarget = Vector3.Distance(transform.position, targetPosition);

            // Check if we are close enough to strike
            if (distanceToTarget <= attackRadius)
            {
                StartCoroutine(ExecuteAttackSequence(currentTargetObject));
            }
            else
            {
                // Full sprint towards the rabbit's position
                agent.speed = runSpeed;
                agent.SetDestination(targetPosition);
            }
        }
        else
        {
            if (hadTargetLastFrame && showDebugLogs)
            {
                Debug.Log("[Tiger AI] Target lost or no rabbits left in the world. Returning to baseline idle loop.");
                hadTargetLastFrame = false;
            }

            agent.speed = walkSpeed;
        }

        // Seamless animator updates
        animator.SetFloat("Speed", agent.velocity.magnitude);
    }

    void FindNearestInteractableRabbit()
    {
        // 1. Gather every single rabbit currently alive in the game world
        GameObject[] rawRabbits = GameObject.FindGameObjectsWithTag(rabbitTag);
        
        // TARGET LOCK PROTECTION:
        // If we already have a target, check if it's still alive/valid in the world.
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

            // If our locked target is still alive, stick with it! Don't look at anything else.
            if (targetStillExists)
            {
                return; 
            }
            else
            {
                // Target was likely destroyed or caught, clear the lock to find a new one
                currentTargetObject = null;
            }
        }

        // 2. GLOBAL SEARCH: Find the absolute closest rabbit, ignoring any radius limits
        float shortestDistance = Mathf.Infinity;
        InteractableObject nearestRabbitObject = null;

        foreach (GameObject rabbitGo in rawRabbits)
        {
            InteractableObject interactable = rabbitGo.GetComponent<InteractableObject>();
            if (interactable == null) continue; // Skip if it doesn't have your naming script

            float distanceToRabbit = Vector3.Distance(transform.position, rabbitGo.transform.position);

            // If this rabbit is closer than any other rabbit we've checked so far, save it
            if (distanceToRabbit < shortestDistance)
            {
                shortestDistance = distanceToRabbit;
                nearestRabbitObject = interactable;
            }
        }

        // 3. Lock onto the absolute closest rabbit found across the entire map
        if (nearestRabbitObject != null)
        {
            currentTargetObject = nearestRabbitObject;
        }
    }

    IEnumerator ExecuteAttackSequence(InteractableObject target)
    {
        isAttacking = true;
        agent.isStopped = true;
        animator.SetFloat("Speed", 0f);

        string rabbitName = target.GetItemName();
        
        if (showDebugLogs)
            Debug.Log($"[Tiger Combat] Striking distance initialized at coordinates: {target.transform.position}");

        // 1. Hit animation
        animator.SetInteger("ActionTrigger", 1);
        yield return new WaitForSeconds(1.0f);

        Debug.Log($"[Tiger Combat] SUCCESS! Tiger beat {rabbitName} at close range!");

        // 2. Sound/Roar animation
        animator.SetInteger("ActionTrigger", 2);
        yield return new WaitForSeconds(1.5f);

        // 3. Reset loop variables cleanly
        animator.SetInteger("ActionTrigger", 0);
        currentTargetObject = null;
        hadTargetLastFrame = false; 
        agent.isStopped = false;
        isAttacking = false;
    }

    private void OnDrawGizmosSelected()
    {
        // Draw the attack striking zone in red
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRadius);
    }
}