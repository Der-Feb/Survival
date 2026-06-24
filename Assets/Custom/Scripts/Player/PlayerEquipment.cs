using UnityEngine;

public class PlayerEquipment : MonoBehaviour
{
    [Header("Storage Configuration")]
    public Transform bagContainer; // Drop your 'Bag' GameObject transform here

    private GameObject activeSpawnedPrefab;

    /// <summary>
    /// Spawns the specified item's 3D mesh directly inside the player's visual bag slot.
    /// </summary>
    public void DisplayItemInBag(ItemData item)
    {
        // 1. Instantly clear out whatever asset was previously showing inside the bag
        ClearBagStorage();

        if (item == null || item.itemPrefab == null || bagContainer == null) return;

        // 2. Instantiate the item's custom model mesh inside the bag anchor transform
        activeSpawnedPrefab = Instantiate(item.itemPrefab, bagContainer);

        // 3. Reset spatial coordinates to guarantee alignment
        activeSpawnedPrefab.transform.localPosition = Vector3.zero;
        activeSpawnedPrefab.transform.localRotation = Quaternion.identity;

        // 4. Force apply the custom scale modifier from the ScriptableObject asset records
        activeSpawnedPrefab.transform.localScale = item.equippedScale;

        Debug.Log($"[Bag Engine] Successfully instantiated visual model: {item.itemName} at scale {item.equippedScale}");
    }

    /// <summary>
    /// Deletes the currently visible item instance inside the bag.
    /// </summary>
    public void ClearBagStorage()
    {
        if (activeSpawnedPrefab != null)
        {
            Destroy(activeSpawnedPrefab);
            activeSpawnedPrefab = null;
        }
    }
}