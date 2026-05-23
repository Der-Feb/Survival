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
            Debug.Log($"[Rabbit Death] Manual momentum sequence initiated for {gameObject.name}.");

            // 1. Kill navigation and physics completely so they don't override our manual movement
            UnityEngine.AI.NavMeshAgent rabbitAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (rabbitAgent != null)
            {
                rabbitAgent.isStopped = true;
                rabbitAgent.enabled = false;
            }

            Collider[] colliders = GetComponentsInChildren<Collider>();
            foreach (Collider col in colliders)
            {
                col.enabled = false;
            }

            // 2. Play the animation clip (it will just handle the mesh visuals, we handle the transform)
            if (animator != null)
            {
                animator.SetBool("isDead", true);
            }

            // 3. CALCULATE THE MOMENTUM DIRECTION
            // Figure out where the tiger is relative to us
            Vector3 directionFromAttacker = (transform.position - attacker.position).normalized;
            directionFromAttacker.y = 0; // Keep it on the flat ground plane

            // Determine if we should roll left or right based on the tiger's right paw swing momentum.
            // Using the cross product tells us if the attacker is facing left or right relative to us.
            Vector3 crossProduct = Vector3.Cross(attacker.forward, directionFromAttacker);
            
            // If crossProduct.y is positive, roll right. If negative, roll left.
            float rollDirection = (crossProduct.y >= 0) ? -90f : 90f;

            // Define starting and target states
            Quaternion startRotation = transform.rotation;
            // Apply the tumble twist onto its local Z axis relative to the blow direction
            Quaternion targetRotation = Quaternion.LookRotation(directionFromAttacker) * Quaternion.Euler(0, 0, rollDirection);

            Vector3 startPosition = transform.position;
            // Push the rabbit out outward by 1.8 meters along the swing line
            Vector3 targetPosition = startPosition + (directionFromAttacker * 1.8f); 

            // 4. LERP LOOP: Smoothly animate the rotation and location translation over time
            float duration = 0.6f; // How fast the tumble takes to slam down
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Smooth step curve makes it start fast and slow down on impact
                float easeOut = 1f - Mathf.Pow(1f - t, 3); 

                transform.rotation = Quaternion.Slerp(startRotation, targetRotation, easeOut);
                transform.position = Vector3.Lerp(startPosition, targetPosition, easeOut);

                yield return null;
            }

            // Keep it lying flat on the ground for the remainder of the timer
            yield return new WaitForSeconds(0.9f);

            // 5. Clean up
            OnRabbitDestroyed?.Invoke();
            Destroy(gameObject);
        }
    }
}