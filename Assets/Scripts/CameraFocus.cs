using UnityEngine;

public class CameraFocus : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public GameObject focusImageUI;
    
    [Header("FOV Settings")]
    public float normalFOV = 60f;
    public float focusedFOV = 30f;
    public float zoomSpeed = 10f;

    [Header("Positioning")]
    // We get these from the MouseMovement script automatically
    private MouseMovement moveScript;
    private float originalY;
    private float originalZ;

    void Start()
    {
        // Find the scripts and camera if they aren't assigned
        if (playerCamera == null) playerCamera = GetComponent<Camera>();
        moveScript = GetComponent<MouseMovement>();

        // Store the original offsets you set in the inspector
        if (moveScript != null)
        {
            originalY = moveScript.yOffset;
            originalZ = moveScript.zOffset;
        }

        // Hide UI by default
        if (focusImageUI != null) focusImageUI.SetActive(false);
    }

    void Update()
    {
        bool isKeyPisPressingFocus = Input.GetKey(KeyCode.RightShift);
        if (isKeyPisPressingFocus)
        {
            ApplyFocus(true);
        }
        else
        {
            ApplyFocus(false);
        }
    }

    void ApplyFocus(bool isFocusing)
    {
        // 1. Handle UI Toggle
        if (focusImageUI != null && focusImageUI.activeSelf != isFocusing)
        {
            focusImageUI.SetActive(isFocusing);
        }

        // 2. Handle FOV (Zoom)
        float targetFOV = isFocusing ? focusedFOV : normalFOV;
        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, zoomSpeed * Time.deltaTime);

        // 3. Handle Offsets (TPS to FPS transition)
        if (moveScript != null)
        {
            // If focusing, set offsets to 0. If not, return to original values.
            moveScript.yOffset = isFocusing ? 0f : originalY;
            moveScript.zOffset = isFocusing ? 0f : originalZ;
        }
    }
}