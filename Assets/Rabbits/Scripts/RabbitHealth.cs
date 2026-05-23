using UnityEngine;
using System;

namespace Rabbits
{
    public class RabbitHealth : MonoBehaviour
    {
        // Global event that tells the UI and Tigers a rabbit died
        public static event Action OnRabbitDestroyed;

        [Header("Disintegration Visuals")]
        [Tooltip("Assign a simple generic particle system prefab or rock/debris chunk mesh here")]
        public GameObject disintegrationEffectPrefab; 
        
        private bool isDead = false;

        public void TakeFatalHit()
        {
            if (isDead) return;
            isDead = true;

            // 1. Spawn disintegration/broken bits effect if assigned
            if (disintegrationEffectPrefab != null)
            {
                GameObject effect = Instantiate(disintegrationEffectPrefab, transform.position + Vector3.up * 0.2f, Quaternion.identity);
                Destroy(effect, 2f); // Automatically clean up particle fragments after 2 seconds
            }

            // 2. Broadcast the death event to update the UI Counter and redirect chasing Tigers
            OnRabbitDestroyed?.Invoke();

            // 3. Vaporize the rabbit object from the game world
            Destroy(gameObject);
        }
    }
    
}
