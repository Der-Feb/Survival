using UnityEngine;
using Tiger; 

public class StoneProjectile : MonoBehaviour
{
    private Vector3 targetImpactPoint;
    private float flightSpeed = 22f; 
    private float hitThreshold = 1.5f;
    private bool isInitialized = false;
    private ItemData thrownItemData;
    private float impactDamage = 25f; 

    public void LaunchAtPosition(Vector3 targetPosition, ItemData data)
    {
        targetImpactPoint = targetPosition + Vector3.up * 1.0f;
        thrownItemData = data;
        
        if (data != null)
        {
            flightSpeed = data.throwSpeed;
            impactDamage = data.throwDamage; 
        }
        
        transform.SetParent(null); 
        isInitialized = true;
    }

    private void Update()
    {
        if (!isInitialized) return;

        transform.position = Vector3.MoveTowards(transform.position, targetImpactPoint, flightSpeed * Time.deltaTime);
        transform.Rotate(Vector3.right * 360f * Time.deltaTime, Space.Self);

        if (Vector3.Distance(transform.position, targetImpactPoint) <= hitThreshold)
        {
            RegisterImpact();
        }
    }

    private void RegisterImpact()
    {
        isInitialized = false;
        Debug.Log($"[Combat Log] Thrown projectile impacted at target coordinates: {transform.position}");

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 2.5f);
        foreach (var col in hitColliders)
        {
            VillainHealth tigerHealth = col.GetComponent<VillainHealth>() ?? col.GetComponentInParent<VillainHealth>();
            if (tigerHealth != null)
            {
                tigerHealth.TakeDamage(impactDamage);
                break; 
            }
        }

        ResetAsGroundLoot();
    }

    private void ResetAsGroundLoot()
    {
        RaycastHit groundHit;
        if (Physics.Raycast(transform.position, Vector3.down, out groundHit, 15f))
        {
            transform.position = groundHit.point; 
        }

        transform.rotation = Quaternion.identity;

        Collider col = GetComponent<Collider>() ?? GetComponentInChildren<Collider>();
        if (col != null) col.enabled = true;

        InteractableObject interactable = GetComponent<InteractableObject>() ?? GetComponentInChildren<InteractableObject>();
        if (interactable != null) interactable.enabled = true;

        Destroy(this); 
    }
}