using UnityEngine;
using System.Collections;
using TMPro;
using Tiger; // 1. NAMESPACE REFERENCE

public class SelectionManager : MonoBehaviour
{
    public GameObject interaction_info_ui;
    private TextMeshProUGUI interaction_text;

    [SerializeField] private CameraFocus focusScript;

    [Header("Inventory Setup")]
    public InventoryManager playerInventory;
    public Animator playerAnimator;

    [Header("Combat & Bag Configuration")]
    public Transform bagContainerSource;

    private PlayerMovement playerMovementScript;
    private bool isPickingUp = false;
    
    // 2. TRACKING VARIABLE FIELD
    private VillainUIController currentFocusedTigerUI;

    private void Start()
    {
        if (interaction_info_ui != null)
        {
            interaction_text = interaction_info_ui.GetComponent<TextMeshProUGUI>();
        }

        playerMovementScript = FindAnyObjectByType<PlayerMovement>();

        if (playerMovementScript != null)
        {
            if (playerAnimator == null)
            {
                playerAnimator = playerMovementScript.GetComponentInChildren<Animator>();
            }
            else
            {
                playerMovementScript.animator = playerAnimator;
            }

            if (playerInventory == null)
            {
                playerInventory = playerMovementScript.GetComponent<InventoryManager>() 
                                  ?? playerMovementScript.GetComponentInChildren<InventoryManager>();
            }
        }
    }

    private void Update()
    {
        if (isPickingUp) return;

        if (focusScript != null && !focusScript.isFocusing)
        {
            if (interaction_info_ui != null) interaction_info_ui.SetActive(false);
            ClearCurrentFocusedTiger(); // Safely clear UI if focus script breaks look window
            return;
        }

        bool hasActiveItemEquipped = playerInventory != null && playerInventory.activeEquippedItem != null;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            var selectionTransform = hit.transform;

            // 3. TARGET FOCUS AND DISPLAY DETECTION BLOCK
            VillainUIController tigerUI = selectionTransform.GetComponent<VillainUIController>() ?? 
                                          selectionTransform.GetComponentInParent<VillainUIController>();

            if (tigerUI != null)
            {
                // If switching focus to a brand new target, disable the old canvas overlay first
                if (currentFocusedTigerUI != null && currentFocusedTigerUI != tigerUI)
                {
                    currentFocusedTigerUI.SetHealthBarVisible(false);
                }

                currentFocusedTigerUI = tigerUI;
                currentFocusedTigerUI.SetHealthBarVisible(true);
            }
            else
            {
                // Cursor moved to a regular object layer (Ground, tree, rocks) -> Turn off bar
                ClearCurrentFocusedTiger();
            }

            // COMBAT TARGET EVALUATION: Keep your original input checking loop running seamlessly
            var tigerTarget = selectionTransform.GetComponent<Villain_AI_Controller>() ?? 
                              selectionTransform.GetComponentInParent<Villain_AI_Controller>();

            if (tigerTarget != null && hasActiveItemEquipped)
            {
                ItemData equippedItem = playerInventory.activeEquippedItem;
                float distanceToTiger = hit.distance;

                if (interaction_text != null)
                {
                    interaction_text.text = $"{selectionTransform.name}\n<size=70%>({distanceToTiger:F2}m)</size>\n<color=orange>[Press ENTER to Throw {equippedItem.itemName}]</color>";
                }
                if (interaction_info_ui != null) interaction_info_ui.SetActive(true);

                if (Input.GetKeyDown(KeyCode.Return))
                {
                    Vector3 tigerPositionSnapshot = tigerTarget.transform.position;
                    StartCoroutine(ExecuteThrowSequence(tigerPositionSnapshot));
                }
                return; 
            }

            // RESOURCE PICKUP EVALUATION
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
                if (interactable.storable && distanceHit <= 10.0f)
                {
                    interaction_text.text = $"{itemName}\n<size=70%>({distanceHit:F2}m)</size>\n<color=yellow>[Left Click To Store]</color>";

                    if (Input.GetMouseButtonDown(0))
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

            if (interaction_info_ui != null) CustomActivationTracker(true);
        }
        else
        {
            // Raycast hit nothing but empty sky coordinates -> Hide the bar safely
            ClearCurrentFocusedTiger();
            if (interaction_info_ui != null) CustomActivationTracker(false);
        }
    }

    // CLEANUP HELPER METHOD: Turns off active UI visibility states cleanly
    private void ClearCurrentFocusedTiger()
    {
        if (currentFocusedTigerUI != null)
        {
            currentFocusedTigerUI.SetHealthBarVisible(false);
            currentFocusedTigerUI = null;
        }
    }

    private void CustomActivationTracker(bool state)
    {
        interaction_info_ui.SetActive(state);
    }

    private IEnumerator ExecuteThrowSequence(Vector3 targetPosition)
    {
        isPickingUp = true; 
        if (interaction_info_ui != null) interaction_info_ui.SetActive(false);
        if (playerMovementScript != null) playerMovementScript.isLocked = true;

        if (playerMovementScript != null)
        {
            Vector3 faceDirection = (targetPosition - playerMovementScript.transform.position);
            faceDirection.y = 0; 
            if (faceDirection.sqrMagnitude > 0.01f)
            {
                playerMovementScript.transform.rotation = Quaternion.LookRotation(faceDirection);
            }
        }

        if (playerAnimator != null)
        {
            playerAnimator.SetTrigger("Throw");
        }

        yield return new WaitForSeconds(0.35f); 

        if (bagContainerSource != null && playerInventory != null && playerInventory.activeEquippedItem != null)
        {
            ItemData activeData = playerInventory.activeEquippedItem;

            if (activeData.itemPrefab != null)
            {
                GameObject flyingProjectile = Instantiate(activeData.itemPrefab, bagContainerSource.position, Quaternion.identity);
                flyingProjectile.transform.localScale = activeData.itemPrefab.transform.localScale;

                StoneProjectile projectile = flyingProjectile.AddComponent<StoneProjectile>();
                projectile.LaunchAtPosition(targetPosition, activeData);
            }

            var equipment = playerMovementScript.GetComponent<PlayerEquipment>() ?? playerMovementScript.GetComponentInChildren<PlayerEquipment>();
            if (equipment != null) equipment.ClearBagStorage();

            playerInventory.AddToInventory(activeData.itemName, -1);
            
            int remainingQuantity = playerInventory.GetItemCount(activeData.itemName);

            if (remainingQuantity > 0)
            {
                Debug.Log($"[Combat Auto-Reload] Remaining {activeData.itemName} count: {remainingQuantity}. Reloading bag slot container.");
                if (equipment != null)
                {
                    equipment.DisplayItemInBag(activeData);
                }
            }
            else
            {
                Debug.Log($"[Combat System] Out of {activeData.itemName}s. Unarming active player item reference state slots.");
                playerInventory.activeEquippedItem = null;
            }
        }

        yield return new WaitForSeconds(0.25f); 

        if (playerMovementScript != null) playerMovementScript.isLocked = false;
        isPickingUp = false;
    }

    private IEnumerator ExecutePickupSequence(InteractableObject target)
    {
        if (target == null || playerMovementScript == null) yield break;
        isPickingUp = true;
        if (interaction_info_ui != null) interaction_info_ui.SetActive(false);

        try
        {
            playerMovementScript.isLocked = true;
            Collider col = target.GetComponentInChildren<Collider>();
            if (col != null) col.enabled = false;

            float stopRadius = 3.0f;
            bool arrived = false;

            while (!arrived && target != null)
            {
                Vector3 playerPosFlat = new Vector3(playerMovementScript.transform.position.x, 0, playerMovementScript.transform.position.z);
                Vector3 targetPosFlat = new Vector3(target.transform.position.x, 0, target.transform.position.z);
                float flatDistance = Vector3.Distance(playerPosFlat, targetPosFlat);

                if (flatDistance > stopRadius)
                {
                    Vector3 moveDirection = (targetPosFlat - playerPosFlat).normalized;
                    Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                    playerMovementScript.transform.rotation = Quaternion.Slerp(playerMovementScript.transform.rotation, targetRotation, 10f * Time.deltaTime);

                    playerMovementScript.currentSpeed = playerMovementScript.baseSpeed;
                    playerMovementScript.controller.Move(moveDirection * playerMovementScript.currentSpeed * Time.deltaTime);
                    playerMovementScript.UpdateAnimation();
                }
                else arrived = true;

                yield return null;
            }

            playerMovementScript.currentSpeed = 0f;
            if (playerAnimator == null) playerAnimator = playerMovementScript.GetComponentInChildren<Animator>();

            if (playerAnimator != null)
            {
                playerAnimator.SetBool("isMoving", false);
                playerAnimator.SetBool("isStopped", true);
                playerAnimator.SetTrigger("PickUp");
            }

            yield return new WaitForSeconds(0.3f);

            Vector3 originalScale = target != null ? target.transform.localScale : Vector3.zero;
            float elapsed = 0f;
            float duration = 0.5f; 

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                if (target != null)
                {
                    target.transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, elapsed / duration);
                }
                yield return null;
            }

            if (playerInventory != null && target != null)
            {
                playerInventory.AddToInventory(target.ItemName, 1);
            }

            if (target != null) Destroy(target.gameObject);
        }
        finally
        {
            if (playerMovementScript != null) playerMovementScript.isLocked = false;
            isPickingUp = false;
        }
    }
}
