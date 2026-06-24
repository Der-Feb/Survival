using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class InventoryManager : MonoBehaviour
{
    private Dictionary<string, int> itemsInInventory = new Dictionary<string, int>();

    [Header("UI References")]
    public GameObject slotPrefab;       
    public Transform slotContainer;     
    public TextMeshProUGUI emptyStateText; // Assign your "Empty" Text component here

    [Header("Item Database")]
    public List<ItemData> itemDatabase; 

    [Header("Active Item Tracking")]
    public ItemData activeEquippedItem; // Tracks what you clicked/activated

    private List<GameObject> activeSlots = new List<GameObject>();

    // Run right when the game starts to ensure the empty state displays correctly
    private void Start()
    {
        UpdateInventoryUI();
    }

    public void AddToInventory(string baseItemName, int amount = 1)
    {
        if (!itemsInInventory.ContainsKey(baseItemName))
        {
            itemsInInventory[baseItemName] = 0;
        }

        itemsInInventory[baseItemName] += amount;
        Debug.Log($"[Inventory System] Registered: +{amount} {baseItemName}.");

        UpdateInventoryUI();
    }

    public void UpdateInventoryUI()
    {
        // 1. Clear out old slots
        foreach (GameObject slot in activeSlots)
        {
            Destroy(slot);
        }
        activeSlots.Clear();

        // 2. Handle Empty State Visibility
        if (itemsInInventory.Count == 0)
        {
            if (emptyStateText != null) emptyStateText.gameObject.SetActive(true);
            return;
        }
        else
        {
            if (emptyStateText != null) emptyStateText.gameObject.SetActive(false);
        }

        // 3. Spawn visual slot prefabs
        foreach (KeyValuePair<string, int> entry in itemsInInventory)
        {
            ItemData matchingData = itemDatabase.Find(x => x.itemName.ToLower() == entry.Key.ToLower());

            if (matchingData != null)
            {
                GameObject newSlot = Instantiate(slotPrefab, slotContainer);
                activeSlots.Add(newSlot);

                InventorySlot slotScript = newSlot.GetComponent<InventorySlot>();
                if (slotScript != null)
                {
                    // Check if this item loop iteration matches our current active item selection
                    bool isItemActive = (activeEquippedItem == matchingData);

                    // Initialize slot passing its state and the click callback function
                    slotScript.Setup(matchingData, entry.Value, isItemActive, ToggleItemActivation);
                }
            }
        }
    }

    private void ToggleItemActivation(ItemData item)
    {
        PlayerEquipment equipment = GetComponent<PlayerEquipment>() ?? GetComponentInChildren<PlayerEquipment>();

        if (activeEquippedItem == item)
        {
            // Deactivating the item selection
            activeEquippedItem = null;
            Debug.Log($"[Inventory] Deactivated: {item.itemName}");

            if (equipment != null) equipment.ClearBagStorage();
        }
        else
        {
            // Activating the item selection
            activeEquippedItem = item;
            Debug.Log($"[Inventory] Activated: {activeEquippedItem.itemName}");

            if (equipment != null) equipment.DisplayItemInBag(item);
        }

        // Force refresh visual interface state parameters instantly
        UpdateInventoryUI();
    }

    // This runs whenever you click an item slot in your menu
    private void SelectItem(ItemData item)
    {
        activeEquippedItem = item;
        Debug.Log($"[Inventory] Active item selection changed to: {activeEquippedItem.itemName}");
        
        // Later, your building or tool system can read 'activeEquippedItem' from here to use it!
    }

    public int GetItemCount(string baseItemName)
    {
        if (itemsInInventory.ContainsKey(baseItemName))
        {
            return itemsInInventory[baseItemName];
        }
        return 0;
    }

    public Dictionary<string, int>.KeyCollection GetInventoryKeys()
    {
        return itemsInInventory.Keys;
    }
}
