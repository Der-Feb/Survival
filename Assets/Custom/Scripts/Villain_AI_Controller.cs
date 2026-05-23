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
        if (isAttacking) return;

        scanTimer += Time.deltaTime;
        if (scanTimer >= scanInterval)
        {
            scanTimer = 0f;
            FindNearestInteractableRabbit();
        }

        if (currentTargetObject != null)
        {
            if (!hadTargetLastFrame && showDebugLogs)
            {
                // Debug.Log($"[Tiger AI] Target Acquired → '{currentTargetObject.GetItemName()}'");
                hadTargetLastFrame = true;
            }

            Vector3 targetPosition = currentTargetObject.transform.position;
            
            // FIX: Flatten vectors to run 2D horizontal distance checks, bypassing pivot height offsets
            Vector3 flatTigerPos = new Vector3(transform.position.x, 0, transform.position.z);
            Vector3 flatTargetPos = new Vector3(targetPosition.x, 0, targetPosition.z);
            float distanceToTarget = Vector3.Distance(flatTigerPos, flatTargetPos);

            // LOG ADDED: This will print every frame while chasing so you can see why it's stuck pushing
            if (showDebugLogs)
            {
                // Debug.Log($"[Tiger Distance Check] Calculated Distance: {distanceToTarget:F2}m | Required Attack Radius: {attackRadius}m");
            }

            if (distanceToTarget <= attackRadius)
            {
                if (showDebugLogs)
                {
                    // Debug.Log($"[Tiger Logic Target] Distance condition MET ({distanceToTarget:F2} <= {attackRadius}). Entering Coroutine.");
                }
                StartCoroutine(ExecuteAttackSequence(currentTargetObject));
            }
            else
            {
                agent.speed = runSpeed;
                agent.SetDestination(targetPosition);
            }
        }
        else
        {
            if (hadTargetLastFrame && showDebugLogs)
            {
                // Debug.Log("[Tiger AI] Target lost or no rabbits left in the world. Returning to baseline idle loop.");
                hadTargetLastFrame = false;
            }

            agent.speed = walkSpeed;
        }

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

    IEnumerator ExecuteAttackSequence(InteractableObject target)
    {
        isAttacking = true;
        agent.isStopped = true;
        animator.SetFloat("Speed", 0f);

        string rabbitName = target.GetItemName();

        // 1. Fire hit animation sequence using original ActionTrigger parameter
        animator.SetInteger("ActionTrigger", 1);
        yield return new WaitForSeconds(1.0f); 

        if (target != null)
        {
            // FIX: Look everywhere inside the target hierarchy to find the health script
            RabbitHealth rabbitHealth = target.GetComponentInChildren<RabbitHealth>();
            
            if (rabbitHealth != null)
            {
                // Pass 'transform' (the tiger's transform) so the rabbit calculates the push direction
                rabbitHealth.TakeFatalHit(transform);
            }
            else
            {
                // Clean fallback cleanup if health component is completely missing from the prefab
                Destroy(target.gameObject);
                
                // Manual fallback to alert the UI system a rabbit was wiped out
                Rabbits.RabbitHealth.OnRabbitDestroyed?.Invoke();
            }
        }

        // 2. Play roar/sound animation sequence
        animator.SetInteger("ActionTrigger", 2);
        yield return new WaitForSeconds(1.5f);

        // 3. Reset tracking loop parameters cleanly back to baseline loop states
        animator.SetInteger("ActionTrigger", 0);
        currentTargetObject = null;
        hadTargetLastFrame = false; 
        agent.isStopped = false;
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