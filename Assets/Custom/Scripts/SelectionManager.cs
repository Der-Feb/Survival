using UnityEngine;
using System.Collections;
using TMPro;

public class SelectionManager : MonoBehaviour
{
    public GameObject interaction_info_ui;
    private TextMeshProUGUI interaction_text;

    [SerializeField] private CameraFocus focusScript;

    [Header("Inventory Setup")]
    public InventoryManager playerInventory;
    public Animator playerAnimator;

    [Header("Combat & Bag Configuration")]
    public Transform bagContainerSource; // Drag your 'Bag' GameObject hierarchy link here!

    private PlayerMovement playerMovementScript;
    private bool isPickingUp = false;

    private void Start()
    {
        if (interaction_info_ui != null)
        {
            interaction_text = interaction_info_ui.GetComponent<TextMeshProUGUI>();
        }

        // Direct Type Search: Finds the active script in the scene
        playerMovementScript = FindAnyObjectByType<PlayerMovement>();

        if (playerMovementScript != null)
        {
            // SAFEGUARD: Keep manually assigned inspector animators intact
            if (playerAnimator == null)
            {
                playerAnimator = playerMovementScript.GetComponentInChildren<Animator>();
            }
            else
            {
                // Sync manually assigned animator to the movement script
                playerMovementScript.animator = playerAnimator;
            }

            // Cache the inventory script from the player automatically if left blank
            if (playerInventory == null)
            {
                playerInventory = playerMovementScript.GetComponent<InventoryManager>() 
                                  ?? playerMovementScript.GetComponentInChildren<InventoryManager>();
            }
        }
    }

    private void Update()
    {
        // Keep the update loop locked out if an action sequence is already playing
        if (isPickingUp) return;

        if (focusScript != null && !focusScript.isFocusing)
        {
            if (interaction_info_ui != null) interaction_info_ui.SetActive(false);
            return;
        }

        // DYNAMIC COMBAT CHECK: Verify if there is ANY active item equipped in the active slot right now
        bool hasActiveItemEquipped = playerInventory != null && playerInventory.activeEquippedItem != null;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            var selectionTransform = hit.transform;

            // 1. COMBAT TARGET EVALUATION: Look for the Tiger's exact AI Controller script
            var tigerTarget = selectionTransform.GetComponent<Villain_AI_Controller>() ?? 
                              selectionTransform.GetComponentInParent<Villain_AI_Controller>();

            // If we are looking at a valid combat target and holding any active item
            if (tigerTarget != null && hasActiveItemEquipped)
            {
                ItemData equippedItem = playerInventory.activeEquippedItem;
                float distanceToTiger = hit.distance;

                if (interaction_text != null)
                {
                    interaction_text.text = $"{selectionTransform.name}\n<size=70%>({distanceToTiger:F2}m)</size>\n<color=orange>[Press ENTER to Throw {equippedItem.itemName}]</color>";
                }
                if (interaction_info_ui != null) interaction_info_ui.SetActive(true);

                // Listen for the Enter / Return key action
                if (Input.GetKeyDown(KeyCode.Return))
                {
                    // CRITICAL: Take a structural Vector3 position snapshot right now so it doesn't track if the tiger moves
                    Vector3 tigerPositionSnapshot = tigerTarget.transform.position;
                    StartCoroutine(ExecuteThrowSequence(tigerPositionSnapshot));
                }
                return; // Break out early to bypass standard item pickup code loops
            }

            // 2. RESOURCE PICKUP EVALUATION: (Your original asset gathering logic loop)
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

            if (interaction_info_ui != null) interaction_info_ui.SetActive(true);
        }
        else
        {
            if (interaction_info_ui != null) interaction_info_ui.SetActive(false);
        }
    }

    private IEnumerator ExecuteThrowSequence(Vector3 targetPosition)
    {
        isPickingUp = true; // Lock internal processing updates
        if (interaction_info_ui != null) interaction_info_ui.SetActive(false);
        if (playerMovementScript != null) playerMovementScript.isLocked = true;

        // Smoothly rotate character to face the location snapshot point before throwing
        if (playerMovementScript != null)
        {
            Vector3 faceDirection = (targetPosition - playerMovementScript.transform.position);
            faceDirection.y = 0; // Lock vertical tipping
            if (faceDirection.sqrMagnitude > 0.01f)
            {
                playerMovementScript.transform.rotation = Quaternion.LookRotation(faceDirection);
            }
        }

        // Play the animator parameter trigger we configured together
        if (playerAnimator != null)
        {
            playerAnimator.SetTrigger("Throw");
        }

        // Wait for the arm animation frames to sync visually before detaching the object
        yield return new WaitForSeconds(0.35f); 

        // Spawn out of the container bag space dynamically
        if (bagContainerSource != null && bagContainerSource.childCount > 0 && playerInventory != null)
        {
            // Pick up the first visual model child instantiated on the player's back
            Transform displayedItem = bagContainerSource.GetChild(0);
            ItemData activeData = playerInventory.activeEquippedItem;

            // Add the projectile physics logic onto the item dynamically and launch it
            StoneProjectile projectile = displayedItem.gameObject.AddComponent<StoneProjectile>();
            projectile.LaunchAtPosition(targetPosition, activeData);

            // Cleanly wipe the active item's visual reference out of the Bag container space
            var equipment = playerMovementScript.GetComponent<PlayerEquipment>() ?? playerMovementScript.GetComponentInChildren<PlayerEquipment>();
            if (equipment != null) equipment.ClearBagStorage();

            // 1. QUANTITY MANAGEMENT: Reduce item database tracking entries by -1
            playerInventory.AddToInventory(activeData.itemName, -1);
            
            // 2. AUTO-RELOAD SYSTEM: Query how many of this item are still left in the database
            int remainingQuantity = playerInventory.GetItemCount(activeData.itemName);

            if (remainingQuantity > 0)
            {
                // Auto-reload: Place the next matching item model prefab directly back onto the player's back
                Debug.Log($"[Combat Auto-Reload] Remaining {activeData.itemName} count: {remainingQuantity}. Reloading bag slot container.");
                if (equipment != null)
                {
                    equipment.DisplayItemInBag(activeData);
                }
            }
            else
            {
                // Empty ammo: Wipe active equipment state memory clear completely
                Debug.Log($"[Combat System] Out of {activeData.itemName}s. Unarming active player item reference state slots.");
                playerInventory.activeEquippedItem = null;
            }
        }

        yield return new WaitForSeconds(0.25f); // Finish recovery frames loop safely

        if (playerMovementScript != null) playerMovementScript.isLocked = false;
        isPickingUp = false;
    }
    private IEnumerator ExecutePickupSequence(InteractableObject target)
    {
        if (target == null || playerMovementScript == null)
        {
            yield break;
        }

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
                else
                {
                    arrived = true;
                }

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