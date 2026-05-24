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

    private PlayerMovement playerMovementScript;
    private bool isPickingUp = false;

    private void Start()
    {
        if (interaction_info_ui != null)
        {
            interaction_text = interaction_info_ui.GetComponent<TextMeshProUGUI>();
        }

        // Direct Type Search: Finds the active script in the scene (No tags required!)
        playerMovementScript = FindAnyObjectByType<PlayerMovement>();

        if (playerMovementScript != null)
        {
            // SAFEGUARD: If you assigned the Soldier Animator in the inspector, KEEP IT!
            // Do not run an automatic search that could accidentally grab the Main Camera.
            if (playerAnimator == null)
            {
                playerAnimator = playerMovementScript.GetComponentInChildren<Animator>();
            }
            else
            {
                // Sync your manually assigned inspector animator to the movement script
                playerMovementScript.animator = playerAnimator;
            }

            // Cache the inventory script from the player automatically if left blank
            if (playerInventory == null)
            {
                playerInventory = playerMovementScript.GetComponent<InventoryManager>() 
                                  ?? playerMovementScript.GetComponentInChildren<InventoryManager>();
            }
        }

        // Final verification checks
        if (playerMovementScript == null) Debug.LogError("[SELECTION ERROR] PlayerMovement script was not found anywhere in the scene!");
        if (playerAnimator == null) Debug.LogError("[SELECTION ERROR] Animator component was not found on the player model structure!");
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
            // Lock keyboard/WASD inputs safely
            playerMovementScript.isLocked = true;

            // Turn off the item collider so the character doesn't bump or trip over it
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

                    // Instantly rotate character smoothly towards resource anchor target
                    Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                    playerMovementScript.transform.rotation = Quaternion.Slerp(playerMovementScript.transform.rotation, targetRotation, 10f * Time.deltaTime);

                    // Force target manual speed assignments
                    playerMovementScript.currentSpeed = playerMovementScript.baseSpeed;

                    // Physically drive the character controller forward
                    playerMovementScript.controller.Move(moveDirection * playerMovementScript.currentSpeed * Time.deltaTime);

                    // Update manual movement blend tree fields
                    playerMovementScript.UpdateAnimation();
                }
                else
                {
                    arrived = true;
                }

                yield return null;
            }

            // Halt physical physics parameters immediately on arrival
            playerMovementScript.currentSpeed = 0f;
            
            // Re-verify the animator reference hasn't shifted dynamically
            if (playerAnimator == null) playerAnimator = playerMovementScript.GetComponentInChildren<Animator>();

            if (playerAnimator != null)
            {
                playerAnimator.SetBool("isMoving", false);
                playerAnimator.SetBool("isStopped", true);
                
                // Triggers the exact pickup parameter string in your Animator layout
                playerAnimator.SetTrigger("PickUp");
            }

            // Hold code positioning context briefly for the hand-lift animation frames
            yield return new WaitForSeconds(0.3f);

            // Interpolation scale reduction effect
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

            // Save object data records to inventory script
            if (playerInventory != null && target != null)
            {
                playerInventory.AddToInventory(target.ItemName, 1);
            }

            if (target != null) Destroy(target.gameObject);
        }
        finally
        {
            // Fail-safe protection: ALWAYS restore player control parameters no matter what
            if (playerMovementScript != null) playerMovementScript.isLocked = false;
            isPickingUp = false;
        }
    }
}
