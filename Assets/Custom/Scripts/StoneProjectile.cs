using UnityEngine;

public class StoneProjectile : MonoBehaviour
{
    private Vector3 targetImpactPoint;
    private float flightSpeed = 22f; // This will now get overridden dynamically
    private float hitThreshold = 1.5f;
    private bool isInitialized = false;
    private ItemData thrownItemData;

    public void LaunchAtPosition(Vector3 targetPosition, ItemData data)
    {
        targetImpactPoint = targetPosition + Vector3.up * 1.0f;
        thrownItemData = data;
        
        // DYNAMIC SPEED SYNC: Pull the speed variable value directly from your asset configurations!
        if (data != null)
        {
            flightSpeed = data.throwSpeed;
        }
        
        transform.SetParent(null); 
        isInitialized = true;
    }

    private void Update()
    {
        if (!isInitialized) return;

        // Fly towards destination using the customized item speed
        transform.position = Vector3.MoveTowards(transform.position, targetImpactPoint, flightSpeed * Time.deltaTime);
        transform.Rotate(Vector3.right * 360f * Time.deltaTime, Space.Self);

        if (Vector3.Distance(transform.position, targetImpactPoint) <= hitThreshold)
        {
            RegisterImpact();
        }
    }

    private void RegisterImpact()
    {
        Debug.Log($"[Projectile Log] {thrownItemData.itemName} landed at: {transform.position} traveling at speed: {flightSpeed}");

        // Check if the Tiger was inside the blast zone
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 2.5f);
        foreach (var col in hitColliders)
        {
            if (col.GetComponent<Villain_AI_Controller>() != null || col.GetComponentInParent<Villain_AI_Controller>() != null)
            {
                Debug.Log("Success: The tiger is hit!");
                break;
            }
        }

        MakeItemPickableAgain();
    }

    private void MakeItemPickableAgain()
    {
        isInitialized = false; 

        // Drop the stone cleanly to the floor level
        RaycastHit groundHit;
        if (Physics.Raycast(transform.position, Vector3.down, out groundHit, 15f))
        {
            transform.position = groundHit.point; 
        }

        transform.rotation = Quaternion.identity;

        Collider col = GetComponent<Collider>() ?? GetComponentInChildren<Collider>();
        if (col != null) col.enabled = true;

        InteractableObject interactable = GetComponent<InteractableObject>() ?? GetComponentInChildren<InteractableObject>();
        if (interactable != null)
        {
            interactable.enabled = true;
        }

        Destroy(this); 
    }
}