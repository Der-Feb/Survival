using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class SelectionManager : MonoBehaviour
{
    public GameObject interaction_info_ui;
    private TextMeshProUGUI interaction_text;

    [SerializeField]
    private CameraFocus focusScript;

    [Header("Inventory Setup")]
    public InventoryManager playerInventory;
    public Animator playerAnimator;

    private bool isPickingUp = false;

    private void Start()
    {
        if (interaction_info_ui != null)
        {
            interaction_text = interaction_info_ui.GetComponent<TextMeshProUGUI>();
        }
    }

    private void Update()
    {
        if (isPickingUp) return;

        if (focusScript != null && !focusScript.isFocusing)
        {
            if (interaction_info_ui != null) interaction_info_ui.SetActive(false);
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            var selectionTransform = hit.transform;
            InteractableObject interactable = selectionTransform.GetComponent<InteractableObject>()
                ?? selectionTransform.GetComponentInParent<InteractableObject>();

            if (interactable == null)
            {
                if (interaction_info_ui != null) interaction_info_ui.SetActive(false);
                return;
            }

            float distanceHit = hit.distance;
            string itemName = interactable.GetItemName();
            
            if (interaction_text != null)
            {
                if (interactable.storable)
                {
                    interaction_text.text = $"{itemName}\n<size=70%>({distanceHit:F2}m)</size>\n<color=yellow>[Right Click To store]</color>";

                    if (Input.GetMouseButtonDown(1))
                    {
                        StartCoroutine(ExecutePickupSequence(interactable));
                        return;
                    }
                }
                else
                {
                    interaction_text.text = $"{itemName}\n<size=70%>({distanceHit:F2}m)</size>";
                }
            }

            if (interaction_info_ui != null) interaction_info_ui.SetActive(true);
        }
        else
        {
            if (interaction_info_ui != null) interaction_info_ui.SetActive(false);
        }
    }

    private IEnumerator ExecutePickupSequence(InteractableObject target)
    {
        isPickingUp = true;
        if (interaction_info_ui != null) interaction_info_ui.SetActive(false);

        // Fallback checks to locate the player components if references aren't assigned manually
        if (playerInventory == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                playerInventory = playerObj.GetComponent<InventoryManager>();
            }
        }

        Collider col = target.GetComponentInChildren<Collider>();
        if (col != null) col.enabled = false;

        if (playerAnimator != null)
        {
            playerAnimator.SetTrigger("PickUp");
        }

        Vector3 originalScale = target.transform.localScale;
        float elapsed = 0f;
        float duration = 0.4f; 

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (target != null)
            {
                target.transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, elapsed / duration);
            }
            yield return null;
        }

        if (playerInventory != null)
        {
            playerInventory.AddToInventory(target.ItemName, 1);
        }

        Destroy(target.gameObject);
        isPickingUp = false;
    }
}