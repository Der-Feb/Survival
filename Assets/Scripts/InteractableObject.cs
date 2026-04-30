using UnityEngine;
using System.Collections.Generic;

public class InteractableObject : MonoBehaviour
{
    public string ItemName;

    private string finalName;

    // Tracks how many of each item type have been created
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