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

    // Accept charge percent parameter (Value from 0.0 to 1.0)
    public void LaunchAtPosition(Vector3 targetPosition, ItemData data, float chargePercent)
    {
        targetImpactPoint = targetPosition + Vector3.up * 0.5f;
        thrownItemData = data;

        if (data != null)
        {
            // Base properties are modified by how long the button was compressed
            // A full charge throw travels up to 1.5x base speed and deals up to 2x damage
            float speedMultiplier = Mathf.Lerp(0.6f, 1.5f, chargePercent);
            float damageMultiplier = Mathf.Lerp(0.5f, 2.0f, chargePercent);

            flightSpeed = data.throwSpeed * speedMultiplier;
            impactDamage = data.throwDamage * damageMultiplier;
        }

        transform.SetParent(null);
        isInitialized = true;
    }

    private void Update()
    {
        if (!isInitialized) return;

        transform.position = Vector3.MoveTowards(transform.position, targetImpactPoint, flightSpeed * Time.deltaTime);
        transform.Rotate(Vector3.right * 450f * Time.deltaTime, Space.Self);

        if (Vector3.Distance(transform.position, targetImpactPoint) <= hitThreshold)
        {
            RegisterImpact();
        }
    }

    private void RegisterImpact()
    {
        isInitialized = false;
        Debug.Log($"[Combat Log] Charged projectile hit target area. Yielded Damage Value: {impactDamage}");

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