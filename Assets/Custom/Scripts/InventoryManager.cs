using UnityEngine;
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour
{
    private Dictionary<string, int> itemsInInventory = new Dictionary<string, int>();

    public void AddToInventory(string baseItemName, int amount = 1)
    {
        if (!itemsInInventory.ContainsKey(baseItemName))
        {
            itemsInInventory[baseItemName] = 0;
        }

        itemsInInventory[baseItemName] += amount;
        Debug.Log($"[Inventory System] Registered: +{amount} {baseItemName}.");
    }

    public int GetItemCount(string baseItemName)
    {
        if (itemsInInventory.ContainsKey(baseItemName))
        {
            return itemsInInventory[baseItemName];
        }
        return 0;
    }

    // NEW HELPER: Exposes a read-only snapshot of the keys for safe UI/Debug logging
    public Dictionary<string, int>.KeyCollection GetInventoryKeys()
    {
        return itemsInInventory.Keys;
    }
}