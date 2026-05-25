using UnityEngine;
using UnityEngine.UI;

namespace Tiger
{
    public class VillainUIController : MonoBehaviour
    {
        [Header("UI Canvas Reference")]
        public GameObject healthBarCanvas;
        
        [Header("Slider Component")]
        public Slider healthSlider;

        private void LateUpdate()
        {
            // BILLBOARD EFFECT: If the canvas is active, force it to face the main camera
            if (healthBarCanvas != null && healthBarCanvas.activeSelf && Camera.main != null)
            {
                healthBarCanvas.transform.LookAt(
                    healthBarCanvas.transform.position + Camera.main.transform.rotation * Vector3.forward, 
                    Camera.main.transform.rotation * Vector3.up
                );
            }
        }

        /// <summary>
        /// Turns the health bar display on or off when focused.
        /// </summary>
        public void SetHealthBarVisible(bool isVisible)
        {
            if (healthBarCanvas != null)
            {
                healthBarCanvas.SetActive(isVisible);
            }
        }
    }   
}
