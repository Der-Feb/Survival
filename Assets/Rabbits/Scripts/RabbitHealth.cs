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
            // If the animator is on a child object, find it automatically
            animator = GetComponentInChildren<Animator>();
        }

        public void TakeFatalHit()
        {
            // FORCE LOG: This will tell us if the tiger successfully triggered this function
            // Debug.Log($"[CRITICAL CHECK] TakeFatalHit() called on {gameObject.name}!");

            if (isVaporized) return;
            isVaporized = true;

            StartCoroutine(ExecuteDeathSequence());
        }

        private IEnumerator ExecuteDeathSequence()
        {
            // Debug.Log($"[Rabbit Health] '{gameObject.name}' playing isDead animation.");

            if (animator != null)
            {
                animator.SetBool("isDead", true);
            }

            // Shut off ALL colliders on this object and its children so the tiger doesn't push it
            Collider[] colliders = GetComponentsInChildren<Collider>();
            foreach (Collider col in colliders)
            {
                col.enabled = false;
            }

            // Wait for the animation to play out
            yield return new WaitForSeconds(1.5f);

            // Tell the UI to update
            OnRabbitDestroyed?.Invoke();

            // Destroy the root object
            Destroy(gameObject);
        }
    }
}