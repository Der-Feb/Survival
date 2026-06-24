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
        // Debug.Log("[Rabbit UI Counter] Awake fired. Checking TextMeshPro reference...");
        ForceWakeUpUI();
    }

    void Start()
    {
        // Debug.Log("[Rabbit UI Counter] Start fired. Running baseline check...");
        ForceWakeUpUI();
    }

    void Update()
    {
        // Continuous hammer loop to make absolutely sure nothing else turns it off
        ForceWakeUpUI();
    }

    private void ForceWakeUpUI()
    {
        // Condition check: Do we have a TextMeshPro reference assigned?
        if (aliveCounterText != null)
        {
            // If it is not active, force it to activate right now!
            if (!aliveCounterText.gameObject.activeSelf)
            {
                // Debug.Log($"[Rabbit UI Counter] SUCCESS! Found reference. Force activating GameObject: '{aliveCounterText.gameObject.name}'");
                aliveCounterText.gameObject.SetActive(true);
            }

            // Run the count query just to fill the text container
            GameObject[] activeRabbits = GameObject.FindGameObjectsWithTag(rabbitTag);
            aliveCounterText.text = $"{activeRabbits.Length} rabbits";
        }
        else
        {
            // CRITICAL LOG: If you see this in your console, the slot in the inspector is completely empty!
            // Debug.LogError("[Rabbit UI Counter] CRITICAL DEADLOCK: 'aliveCounterText' field is NULL! The script has nothing to activate.");
        }
    }
}
