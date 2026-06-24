using UnityEngine;
using TMPro;

public class InventoryUI : MonoBehaviour
{
    [Header("References")]
    public InventoryManager playerInventory;
    public TextMeshProUGUI uiDisplayListText;

    // This runs automatically every single time the Inventory Panel is toggled to active
    private void OnEnable()
    {
        UpdateInventoryDisplay();
    }

    public void UpdateInventoryDisplay()
    {
        if (uiDisplayListText == null) return;

        // Auto-locate the player inventory if it isn't assigned manually
        if (playerInventory == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                playerInventory = playerObj.GetComponent<InventoryManager>();
            }
        }

        if (playerInventory == null)
        {
            uiDisplayListText.text = "Error: Inventory system not found.";
            return;
        }

        // Get the items currently registered
        var storedKeys = playerInventory.GetInventoryKeys();
        string updatedManifestText = "";
        bool isEmpty = true;

        foreach (string itemName in storedKeys)
        {
            int count = playerInventory.GetItemCount(itemName);
            if (count > 0)
            {
                // Format each item nicely on a new line
                updatedManifestText += $"• {itemName}  <color=yellow>x{count}</color>\n";
                isEmpty = false;
            }
        }

        if (isEmpty)
        {
            updatedManifestText += "The inventory is empty";
        }

        // Update the screen display text component
        uiDisplayListText.text = updatedManifestText;
    }
}