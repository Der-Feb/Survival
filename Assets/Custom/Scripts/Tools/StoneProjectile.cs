using UnityEngine;
using Tiger;

[RequireComponent(typeof(Rigidbody))]
public class StoneProjectile : MonoBehaviour
{
    private Rigidbody rb;
    private ItemData thrownItemData;
    private float impactDamage = 25f;
    private bool hasImpacted = false;

    [Header("Earth Delimiter Setup")]
    [SerializeField] private float absoluteEarthFloorDelimiter = -20f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    /// <summary>
    /// Launches the stone using realistic physics forces.
    /// </summary>
    public void LaunchAtPosition(Vector3 spawnOrigin, Vector3 targetPosition, ItemData data, float chargePercent)
    {
        thrownItemData = data;
        transform.position = spawnOrigin;
        transform.SetParent(null);

        // Ensure physics settings are clean
        rb.isKinematic = false;
        rb.useGravity = true;

        if (data != null)
        {
            impactDamage = data.throwDamage * Mathf.Lerp(0.5f, 2.0f, chargePercent);
        }

        // Dynamically adjust arc height based on the throw charge
        float preferredArcHeight = Mathf.Lerp(6f, 2f, chargePercent);

        // Calculate the physical initial velocity required to hit our target via a parabolic arc
        Vector3 launchVelocity = CalculateParabolicVelocity(spawnOrigin, targetPosition, preferredArcHeight);

        // Inject the force instantly into Unity's physics system
        rb.AddForce(launchVelocity, ForceMode.VelocityChange);

        // Give it an erratic spin for visual flavor
        rb.angularVelocity = new Vector3(Random.Range(-10f, 10f), Random.Range(-10f, 10f), Random.Range(-10f, 10f));
    }

    private void Update()
    {
        // 🧱 EARTH BASING VOID DELIMITER RULE
        if (!hasImpacted && transform.position.y < absoluteEarthFloorDelimiter)
        {
            RegisterImpact();
        }
    }

    // Detect collision with standard Unity physics matrices
    private void OnCollisionEnter(Collision collision)
    {
        if (hasImpacted) return;

        // Ignore collisions with the player who threw it
        if (collision.gameObject.CompareTag("Player")) return;

        RegisterImpact();
    }

    private void RegisterImpact()
    {
        hasImpacted = true;
        rb.isKinematic = true; // Turn off physics updates so it stops rolling forever

        Debug.Log($"[Combat Log] Physics projectile impacted at coordinates: {transform.position}");

        // Damage detection sweep
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
        transform.rotation = Quaternion.identity;

        // Reactivate script interactors so the player can pick it up again
        InteractableObject interactable = GetComponent<InteractableObject>() ?? GetComponentInChildren<InteractableObject>();
        if (interactable != null) interactable.enabled = true;

        Destroy(this); // Remove this combat tracker component, leaving the object in the world
    }

    /// <summary>
    /// Uses basic kinematics to find the velocity vector needed to reach a target point given an arc height.
    /// </summary>
    private Vector3 CalculateParabolicVelocity(Vector3 start, Vector3 end, float arcHeight)
    {
        float displacementY = end.y - start.y;
        Vector3 displacementXZ = new Vector3(end.x - start.x, 0, end.z - start.z);

        // Kinematics math: v_y = sqrt(2 * g * h)
        float gravity = Mathf.Abs(Physics.gravity.y);
        float velocityY = Mathf.Sqrt(2 * gravity * arcHeight);

        // Time to reach peak: t_peak = v_y / g
        float timeToPeak = velocityY / gravity;
        // Time from peak to destination: t_descend = sqrt(2 * (h - dy) / g)
        float timeToDescend = Mathf.Sqrt(2 * Mathf.Max(0.01f, arcHeight - displacementY) / gravity);

        float totalFlightTime = timeToPeak + timeToDescend;

        // Velocity = distance / time
        Vector3 velocityXZ = displacementXZ / totalFlightTime;

        return new Vector3(velocityXZ.x, velocityY, velocityXZ.z);
    }
}