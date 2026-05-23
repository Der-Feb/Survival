using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventorySlot : MonoBehaviour
{
    public Image iconImage;
    public TextMeshProUGUI countText;
    
    [Header("New Button Toggle References")]
    public Button toggleButton;
    public TextMeshProUGUI toggleButtonText;

    private ItemData currentItem;
    private System.Action<ItemData> onToggleCallback;

    public void Setup(ItemData item, int count, bool isActive, System.Action<ItemData> onToggleClick)
    {
        currentItem = item;
        onToggleCallback = onToggleClick;
        
        iconImage.sprite = item.itemIcon;
        countText.text = $"{item.itemName} <color=yellow>x{count}</color>";

        // Set the button text based on whether this specific item is currently equipped
        if (isActive)
        {
            toggleButtonText.text = "Deactivate";
            toggleButtonText.color = Color.red;
        }
        else
        {
            toggleButtonText.text = "Activate";
            toggleButtonText.color = Color.green;
        }

        // Set up the button click event listener safely
        toggleButton.onClick.RemoveAllListeners();
        toggleButton.onClick.AddListener(() => onToggleCallback?.Invoke(currentItem));
    }
}