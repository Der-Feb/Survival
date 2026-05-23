using UnityEngine;
using System.Collections.Generic;

public class InteractableObject : MonoBehaviour
{
    public string ItemName;
    private string finalName;

    // NEW FIELD: Set to true in the inspector for pocketable items
    public bool storable = false;

    private static Dictionary<string, int> nameCounters = new Dictionary<string, int>();

    private void Awake()
    {
        if (!nameCounters.ContainsKey(ItemName))
        {
            nameCounters[ItemName] = 0;
        }

        nameCounters[ItemName]++;
        int index = nameCounters[ItemName];

        finalName = index > 1 ? $"{ItemName} {index}" : ItemName;
    }

    public string GetItemName() => finalName;
}