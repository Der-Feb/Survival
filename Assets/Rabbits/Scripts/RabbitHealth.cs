using UnityEngine;
using System.Collections;

namespace Rabbits
{
    public class RabbitHealth : MonoBehaviour
    {
        public static System.Action OnRabbitDestroyed;

        private Animator animator;
        private bool isVaporized = false;

        void Awake()
        {
            animator = GetComponentInChildren<Animator>();
        }

        public void TakeFatalHit(Transform attackerTransform)
        {
            if (isVaporized) return;
            isVaporized = true;

            StartCoroutine(ExecuteManualDeathSequence(attackerTransform));
        }

        private IEnumerator ExecuteManualDeathSequence(Transform attacker)
        {
            // FIX: Find the actual prefab container instead of the giant global folder container!
            // We search upward until we find the component that identifies the rabbit instance root.
            Transform prefabRoot = transform;
            InteractableObject interactable = GetComponentInParent<InteractableObject>();
            
            if (interactable != null)
            {
                prefabRoot = interactable.transform;
            }
            else if (transform.parent != null && transform.parent.name != "[Living]")
            {
                prefabRoot = transform.parent;
            }

            Debug.Log($"[Rabbit Death] Safe isolated tumble sequence on target container: {prefabRoot.name}");

            // 1. Safe Navigation Fix targeted ONLY at this rabbit instance
            UnityEngine.AI.NavMeshAgent rabbitAgent = prefabRoot.GetComponentInChildren<UnityEngine.AI.NavMeshAgent>();
            if (rabbitAgent != null && rabbitAgent.isOnNavMesh)
            {
                rabbitAgent.isStopped = true;
                rabbitAgent.updatePosition = false;     
                rabbitAgent.updateRotation = false;     
                rabbitAgent.velocity = Vector3.zero;
            }

            // 2. Shut down movement script on this single instance
            MonoBehaviour rabbitMovementScript = prefabRoot.GetComponentInChildren<Rabbit_AI_Movement>() as MonoBehaviour;
            if (rabbitMovementScript != null)
            {
                rabbitMovementScript.enabled = false; 
            }

            // 3. Disable local colliders so the tiger passes through cleanly
            Collider[] colliders = prefabRoot.GetComponentsInChildren<Collider>();
            foreach (Collider col in colliders)
            {
                col.enabled = false;
            }

            // 4. Play the animation clip
            if (animator != null)
            {
                animator.SetBool("isDead", true);
            }

            // 5. Calculate Motion Tumble Vectors
            Vector3 directionFromAttacker = (prefabRoot.position - attacker.position).normalized;
            directionFromAttacker.y = 0; 

            Vector3 crossProduct = Vector3.Cross(attacker.forward, directionFromAttacker);
            float rollDirection = (crossProduct.y >= 0) ? -90f : 90f; 

            Quaternion startRotation = prefabRoot.rotation;
            Quaternion targetRotation = Quaternion.LookRotation(directionFromAttacker) * Quaternion.Euler(0, 0, rollDirection);

            Vector3 startPosition = prefabRoot.position;
            Vector3 targetPosition = startPosition + (directionFromAttacker * 2.0f); 

            // 6. The smooth isolated lerp loop
            float duration = 0.5f; 
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float easeOut = 1f - Mathf.Pow(1f - t, 3);

                prefabRoot.rotation = Quaternion.Slerp(startRotation, targetRotation, easeOut);
                prefabRoot.position = Vector3.Lerp(startPosition, targetPosition, easeOut);

                yield return null;
            }

            yield return new WaitForSeconds(1.0f);

            // 7. Fire UI callback counter and remove ONLY this individual rabbit
            OnRabbitDestroyed?.Invoke();
            Destroy(prefabRoot.gameObject);
        }
    }
}