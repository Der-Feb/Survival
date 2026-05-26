using UnityEngine;

namespace Tiger
{
    public class VillainHealth : MonoBehaviour
    {
        [Header("Health Variables")]
        public float maxHealth = 100f;
        private float currentHealth;

        [Header("UI Reference From Controller")]
        private VillainUIController uiController;

        private void Start()
        {
            currentHealth = maxHealth;

            // Find the UI component attached to this tiger asset
            uiController = GetComponent<VillainUIController>();

            if (uiController != null && uiController.healthSlider != null)
            {
                uiController.healthSlider.maxValue = maxHealth;
                uiController.healthSlider.value = currentHealth;
            }
        }

        /// <summary>
        /// Deals damage to the tiger and updates its visible green health slider.
        /// </summary>
        public void TakeDamage(float damageAmount)
        {
            currentHealth -= damageAmount;
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

            // Directly push the new health value to the slider bar
            if (uiController != null && uiController.healthSlider != null)
            {
                uiController.healthSlider.value = currentHealth;
            }

            Debug.Log($"[Combat Log] Tiger took {damageAmount} damage! Current Life: {currentHealth}/{maxHealth}");

            if (currentHealth <= 0f)
            {
                DefeatTiger();
            }
        }

        private void DefeatTiger()
        {
            Debug.Log("[Combat Log] Tiger health hit 0. Tiger eliminated.");
            Destroy(gameObject);
        }
    }
}