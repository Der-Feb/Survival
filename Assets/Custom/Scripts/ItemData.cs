using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public Sprite itemIcon; 

    [Header("Equipment & Combat Settings")]
    public GameObject itemPrefab;       // The 3D model asset to spawn (Stone, Helicopter, etc.)
    public Vector3 equippedScale = Vector3.one; // Custom scale modifier (e.g., set to 0.1, 0.1, 0.1 to shrink huge objects)
}