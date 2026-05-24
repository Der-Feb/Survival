using UnityEngine;

public class StoneProjectile : MonoBehaviour
{
    private Vector3 targetImpactPoint;
    private float flightSpeed = 22f;
    private float hitThreshold = 0.6f;
    private bool isInitialized = false;
    private ItemData thrownItemData;

    /// <summary>
    /// Launches the projectile toward a fixed point in space.
    /// </summary>
    public void LaunchAtPosition(Vector3 targetPosition, ItemData data)
    {
        // Aim roughly 1 meter above the ground position to hit the body
        targetImpactPoint = targetPosition + Vector3.up * 1.0f;
        thrownItemData = data;
        
        transform.SetParent(null); 
        isInitialized = true;
    }

    private void Update()
    {
        if (!isInitialized) return;

        // Move towards the snapshot location
        transform.position = Vector3.MoveTowards(transform.position, targetImpactPoint, flightSpeed * Time.deltaTime);

        // Spin visually in flight
        transform.Rotate(Vector3.right * 360f * Time.deltaTime, Space.Self);

        // Check if it reached the target spot
        if (Vector3.Distance(transform.position, targetImpactPoint) <= hitThreshold)
        {
            RegisterImpact();
        }
    }

    private void RegisterImpact()
    {
        if (thrownItemData != null)
        {
            Debug.Log($"[Combat] {thrownItemData.itemName} hit the targeted location!");
        }

        // Check if a tiger is actually standing inside this splash zone area upon impact
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 1.5f);
        foreach (var col in hitColliders)
        {
            if (col.GetComponent<Villain_AI_Controller>() != null)
            {
                Debug.Log("Success: The tiger is hit!");
                break;
            }
        }

        Destroy(gameObject);
    }
}