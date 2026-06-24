using UnityEngine;
using System.Collections;
using TMPro;
using Tiger;

public class SelectionManager : MonoBehaviour
{
    public GameObject interaction_info_ui;
    private TextMeshProUGUI interaction_text;

    [SerializeField] private CameraFocus focusScript;

    [Header("Inventory Setup")]
    public InventoryManager playerInventory;
    public Animator playerAnimator;

    [Header("Combat & Equipment Configuration")]
    public Transform activeHandPreviewSource; // Hand preview anchor point

    [Header("Vision & Whisker System")]
    public Transform playerEyeAnchor;
    [SerializeField] private float whiskerSpread = 0.15f; // Distance offset of side rays from center
    [SerializeField] private float whiskerLength = 40f;   // Maximum distance for checks

    [Header("Throw Charge Settings")]
    [SerializeField] private float minThrowDistance = 5f;
    [SerializeField] private float maxThrowDistance = 40f;
    [SerializeField] private float maxChargeTime = 1.5f;

    private PlayerMovement playerMovementScript;
    private bool isPickingUp = false;
    private VillainUIController currentFocusedTigerUI;

    private float enterPressStartTime;
    private bool isChargingThrow = false;

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

        bool hasActiveItemEquipped = playerInventory != null && playerInventory.activeEquippedItem != null;
        bool isCurrentlyFocusing = focusScript != null && focusScript.isFocusing;

        // 🎯 TARGETING MATRIX: 5-Ray Antenna Whiskers
        Ray centerRay;
        if (isCurrentlyFocusing)
        {
            centerRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        }
        else if (playerEyeAnchor != null)
        {
            centerRay = new Ray(playerEyeAnchor.position, playerEyeAnchor.forward);
        }
        else
        {
            centerRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        }

        Vector3 rightOffset = Camera.main.transform.right * whiskerSpread;
        Vector3 upOffset = Camera.main.transform.up * whiskerSpread;

        Ray topLeftWhisker = new Ray(centerRay.origin - rightOffset + upOffset, centerRay.direction);
        Ray topRightWhisker = new Ray(centerRay.origin + rightOffset + upOffset, centerRay.direction);
        Ray bottomLeftWhisker = new Ray(centerRay.origin - rightOffset - upOffset, centerRay.direction);
        Ray bottomRightWhisker = new Ray(centerRay.origin + rightOffset - upOffset, centerRay.direction);

        RaycastHit hit;
        bool hitFound = false;

        // Priority 1: Main Center Ray
        if (Physics.Raycast(centerRay, out hit, whiskerLength))
        {
            hitFound = true;
        }
        // Priority 2: Whisker Boundary Fallback
        else
        {
            Ray[] whiskers = { topLeftWhisker, topRightWhisker, bottomLeftWhisker, bottomRightWhisker };
            foreach (Ray whisker in whiskers)
            {
                if (Physics.Raycast(whisker, out RaycastHit whiskerHit, whiskerLength))
                {
                    if (whiskerHit.transform.GetComponentInParent<Villain_AI_Controller>() != null ||
                        whiskerHit.transform.GetComponentInParent<InteractableObject>() != null)
                    {
                        hit = whiskerHit;
                        hitFound = true;
                        break;
                    }
                }
            }
        }

        // ========================================================
        // CHARGE DETECTION LOOP
        // ========================================================
        if (hasActiveItemEquipped)
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                enterPressStartTime = Time.time;
                isChargingThrow = true;
            }

            if (isChargingThrow && Input.GetKey(KeyCode.Return))
            {
                float holdDuration = Time.time - enterPressStartTime;
                float chargeRatio = Mathf.Clamp01(holdDuration / maxChargeTime);
                float currentThrowDistance = Mathf.Lerp(minThrowDistance, maxThrowDistance, chargeRatio);

                if (interaction_text != null)
                {
                    interaction_text.text = $"{playerInventory.activeEquippedItem.itemName}\n<color=orange>Power: {chargeRatio * 100f:F0}%</color>\nRange: {currentThrowDistance:F1}m";
                    if (interaction_info_ui != null) interaction_info_ui.SetActive(true);
                }
            }

            if (isChargingThrow && Input.GetKeyUp(KeyCode.Return))
            {
                isChargingThrow = false;
                float totalHoldTime = Time.time - enterPressStartTime;
                float finalChargeRatio = Mathf.Clamp01(totalHoldTime / maxChargeTime);

                Vector3 targetPoint;
                if (hitFound)
                {
                    targetPoint = hit.point;
                }
                else
                {
                    float calculatedDistance = Mathf.Lerp(minThrowDistance, maxThrowDistance, finalChargeRatio);
                    targetPoint = centerRay.origin + (centerRay.direction * calculatedDistance);
                }

                StartCoroutine(ExecuteThrowSequence(targetPoint, finalChargeRatio));
                return;
            }
        }

        if (isChargingThrow) return;

        if (!isCurrentlyFocusing)
        {
            if (interaction_info_ui != null) interaction_info_ui.SetActive(false);
            ClearCurrentFocusedTiger();
            return;
        }

        if (hitFound)
        {
            var selectionTransform = hit.transform;

            VillainUIController tigerUI = selectionTransform.GetComponent<VillainUIController>() ??
                                          selectionTransform.GetComponentInParent<VillainUIController>();

            if (tigerUI != null)
            {
                if (currentFocusedTigerUI != null && currentFocusedTigerUI != tigerUI)
                {
                    currentFocusedTigerUI.SetHealthBarVisible(false);
                }
                currentFocusedTigerUI = tigerUI;
                currentFocusedTigerUI.SetHealthBarVisible(true);
            }
            else
            {
                ClearCurrentFocusedTiger();
            }

            var tigerTarget = selectionTransform.GetComponent<Villain_AI_Controller>() ??
                              selectionTransform.GetComponentInParent<Villain_AI_Controller>();

            if (tigerTarget != null && hasActiveItemEquipped)
            {
                ItemData equippedItem = playerInventory.activeEquippedItem;
                float distanceToTiger = hit.distance;

                if (interaction_text != null)
                {
                    interaction_text.text = $"{selectionTransform.name}\n<size=70%>({distanceToTiger:F2}m)</size>\n<color=yellow>[Hold ENTER to Charge Throw]</color>";
                }
                if (interaction_info_ui != null) interaction_info_ui.SetActive(true);
                return;
            }

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
            ClearCurrentFocusedTiger();
            if (interaction_info_ui != null) CustomActivationTracker(false);
        }
    }

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

    private IEnumerator ExecuteThrowSequence(Vector3 targetPosition, float chargePercent)
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

        if (playerInventory != null && playerInventory.activeEquippedItem != null)
        {
            ItemData activeData = playerInventory.activeEquippedItem;

            // 🛑 UNDERFLOW DEFENSE: Stops stock counts from dipping below zero
            int currentStock = playerInventory.GetItemCount(activeData.itemName);
            if (currentStock <= 0)
            {
                playerInventory.activeEquippedItem = null;
                isPickingUp = false;
                if (playerMovementScript != null) playerMovementScript.isLocked = false;
                yield break;
            }

            // Spawn directly from active hand preview anchor source frame point
            Vector3 spawnPosition = activeHandPreviewSource != null ? activeHandPreviewSource.position : playerMovementScript.transform.position + Vector3.up * 1.2f;

            if (activeData.itemPrefab != null)
            {
                GameObject flyingProjectile = Instantiate(activeData.itemPrefab, spawnPosition, Quaternion.identity);
                flyingProjectile.transform.localScale = activeData.itemPrefab.transform.localScale;

                StoneProjectile projectile = flyingProjectile.AddComponent<StoneProjectile>();
                projectile.LaunchAtPosition(spawnPosition, targetPosition, activeData, chargePercent);
            }

            var equipment = playerMovementScript.GetComponent<PlayerEquipment>() ?? playerMovementScript.GetComponentInChildren<PlayerEquipment>();
            if (equipment != null) equipment.ClearBagStorage();

            // Safe subtraction deduction commit
            playerInventory.AddToInventory(activeData.itemName, -1);
            int remainingQuantity = playerInventory.GetItemCount(activeData.itemName);

            if (remainingQuantity > 0)
            {
                if (equipment != null) equipment.DisplayItemInBag(activeData);
            }
            else
            {
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
                playerAnimator.SetFloat("Speed", 0f);
                playerAnimator.SetTrigger("PickUp");
            }

            yield return new WaitForSeconds(0.45f);

            Vector3 originalScale = target != null ? target.transform.localScale : Vector3.zero;
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