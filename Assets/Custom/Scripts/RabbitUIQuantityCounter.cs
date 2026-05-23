using UnityEngine;
using TMPro;
using Rabbits; 

public class RabbitUIQuantityCounter : MonoBehaviour
{
    public string rabbitTag = "Rabbit";
    
    [Header("UI Text Component Hook")]
    public TMP_Text aliveCounterText; 

    private void Awake()
    {
        // Immediate setup check at launch frame
        UpdateAliveCounterDisplay();
    }

    private void OnEnable()
    {
        // Listen to the global death broadcast event to update counter metrics dynamically
        RabbitHealth.OnRabbitDestroyed += UpdateAliveCounterDisplay;
    }

    private void OnDisable()
    {
        // Disconnect listener loops to ensure zero memory leaks
        RabbitHealth.OnRabbitDestroyed -= UpdateAliveCounterDisplay;
    }

    void Start()
    {
        UpdateAliveCounterDisplay();
    }

    public void UpdateAliveCounterDisplay()
    {
        if (aliveCounterText == null)
        {
            Debug.LogError("[Rabbit UI Counter] CRITICAL: Text asset reference empty! Bind your component inside the inspector.");
            return;
        }

        // Scan scene setup for active instances
        GameObject[] activeRabbits = GameObject.FindGameObjectsWithTag(rabbitTag);
        
        // FIX: Removed early 'return' blocking code. Forces text object to wake up immediately.
        if (!aliveCounterText.gameObject.activeSelf)
        {
            Debug.Log($"[Rabbit UI Counter] Waking up UI text target: '{aliveCounterText.gameObject.name}'");
            aliveCounterText.gameObject.SetActive(true);
        }

        // Output matches your custom lowercase "X rabbits" formatting structure perfectly
        aliveCounterText.text = $"{activeRabbits.Length} rabbits";
    }
}