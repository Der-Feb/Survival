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

    [Header("Combat & Bag Configuration")]
    public Transform bagContainerSource;

    [Header("Vision Configuration")]
    public Transform playerEyeAnchor;

    [Header("Throw Charge Settings")]
    [SerializeField] private float minThrowDistance = 5f;
    [SerializeField] private float maxThrowDistance = 40f;
    [SerializeField] private float maxChargeTime = 1.5f; // Max strength reached at 1.5 seconds

    private PlayerMovement playerMovementScript;
    private bool isPickingUp = false;
    private VillainUIController currentFocusedTigerUI;

    // Charge Tracking Flags
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

        // 🎯 TARGETING MATRIX
        Ray ray;
        if (isCurrentlyFocusing)
        {
            ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        }
        else if (playerEyeAnchor != null)
        {
            ray = new Ray(playerEyeAnchor.position, playerEyeAnchor.forward);
        }
        else
        {
            ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        }

        // ========================================================
        // 🔋 CHARGE DETECTION LOOP (COMBAT TRACKING)
        // ========================================================
        if (hasActiveItemEquipped)
        {
            // 1. Initial Press Down: Start charging
            if (Input.GetKeyDown(KeyCode.Return))
            {
                enterPressStartTime = Time.time;
                isChargingThrow = true;
            }

            // 2. Continuous Holding: Update UI / Visual calculations
            if (isChargingThrow && Input.GetKey(KeyCode.Return))
            {
                float holdDuration = Time.time - enterPressStartTime;
                float chargeRatio = Mathf.Clamp01(holdDuration / maxChargeTime);
                float currentThrowDistance = Mathf.Lerp(minThrowDistance, maxThrowDistance, chargeRatio);

                if (interaction_text != null)
                {
                    interaction_text.text = $"{playerInventory.activeEquippedItem.itemName}\n<color=orange>Charging: {chargeRatio * 100f:F0}%</color>\nDistance: {currentThrowDistance:F1}m";
                    if (interaction_info_ui != null) interaction_info_ui.SetActive(true);
                }
            }

            // 3. Release Key: Execute calculated throw calculations
            if (isChargingThrow && Input.GetKeyUp(KeyCode.Return))
            {
                isChargingThrow = false;
                float totalHoldTime = Time.time - enterPressStartTime;
                float finalChargeRatio = Mathf.Clamp01(totalHoldTime / maxChargeTime);

                Vector3 targetPoint;

                // Handle combat target override if focusing directly on an enemy element
                RaycastHit targetHit;
                if (Physics.Raycast(ray, out targetHit, maxThrowDistance) &&
                    (targetHit.transform.GetComponent<Villain_AI_Controller>() != null || targetHit.transform.GetComponentInParent<Villain_AI_Controller>() != null))
                {
                    // Locked targeted tracking throw
                    targetPoint = targetHit.point;
                }
                else
                {
                    // Blind free-throw calculation scaled entirely on charge values
                    float calculatedDistance = Mathf.Lerp(minThrowDistance, maxThrowDistance, finalChargeRatio);
                    targetPoint = ray.origin + (ray.direction * calculatedDistance);

                    // Drop point down to floor collision terrain if ray hits intermediate obstacles
                    if (Physics.Raycast(ray, out RaycastHit blindHit, calculatedDistance))
                    {
                        targetPoint = blindHit.point;
                    }
                }

                StartCoroutine(ExecuteThrowSequence(targetPoint, finalChargeRatio));
                return;
            }
        }

        // Stop processing layout if running blind throws
        if (isChargingThrow) return;

        if (!isCurrentlyFocusing)
        {
            if (interaction_info_ui != null) interaction_info_ui.SetActive(false);
            ClearCurrentFocusedTiger();
            return;
        }

        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
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

    // Updated sequence signature accepting tracking weight parameters
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

        if (bagContainerSource != null && playerInventory != null && playerInventory.activeEquippedItem != null)
        {
            ItemData activeData = playerInventory.activeEquippedItem;

            if (activeData.itemPrefab != null)
            {
                GameObject flyingProjectile = Instantiate(activeData.itemPrefab, bagContainerSource.position, Quaternion.identity);
                flyingProjectile.transform.localScale = activeData.itemPrefab.transform.localScale;

                StoneProjectile projectile = flyingProjectile.AddComponent<StoneProjectile>();
                // Pass the charge variable down to setup calculations
                projectile.LaunchAtPosition(targetPosition, activeData, chargePercent);
            }

            var equipment = playerMovementScript.GetComponent<PlayerEquipment>() ?? playerMovementScript.GetComponentInChildren<PlayerEquipment>();
            if (equipment != null) equipment.ClearBagStorage();

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