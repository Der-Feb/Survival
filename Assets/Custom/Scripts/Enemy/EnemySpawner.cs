using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Prefabs & Tracking")]
    public GameObject villainPrefab;
    public string villainTag = "Untagged";

    [Header("Spawn Configuration")]
    public Transform[] spawnPoints;
    public int maxActiveVillains = 3;
    public float spawnInterval = 5f;

    private float spawnTimer;

    void Start()
    {
        // Debug.Log("[Spawner] ---- SPAWNER START ----");
        // Debug.Log($"[Spawner] villainPrefab assigned: {villainPrefab != null} | Name: {(villainPrefab != null ? villainPrefab.name : "NULL")}");
        // Debug.Log($"[Spawner] villainTag set to: '{villainTag}'");
        // Debug.Log($"[Spawner] maxActiveVillains: {maxActiveVillains} | spawnInterval: {spawnInterval}s");
        // Debug.Log($"[Spawner] spawnPoints array length: {(spawnPoints != null ? spawnPoints.Length.ToString() : "NULL")}");

        if (villainPrefab == null)
            // Debug.LogError("[Spawner] CRITICAL: villainPrefab is NULL! Assign it in the Inspector.");

        if (spawnPoints == null || spawnPoints.Length == 0)
        {        
            // Debug.LogError("[Spawner] CRITICAL: No spawnPoints assigned!");
        }
        else
        {
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                if (spawnPoints[i] == null)
                {
                        
                    // Debug.LogError($"[Spawner] spawnPoints[{i}] is NULL! Fix the missing reference in the Inspector.");
                }
                else
                    {
                        
                    // Debug.Log($"[Spawner] spawnPoints[{i}]: '{spawnPoints[i].name}' at position {spawnPoints[i].position}");
                    }
            }
        }

        SpawnToCap();
    }

    void Update()
    {
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f;
            // Debug.Log("[Spawner] UPDATE: Interval reached. Running SpawnToCap().");
            SpawnToCap();
        }
    }

    void SpawnToCap()
    {
        // Debug.Log("[Spawner] ---- SpawnToCap() called ----");

        if (spawnPoints == null || spawnPoints.Length == 0 || villainPrefab == null)
        {
            // Debug.LogWarning("[Spawner] Spawn canceled: prefab or spawnPoints missing.");
            return;
        }

        int currentVillainCount = GameObject.FindGameObjectsWithTag(villainTag).Length;
        // Debug.Log($"[Spawner] Active villains with tag '{villainTag}': {currentVillainCount} / {maxActiveVillains}");

        if (currentVillainCount >= maxActiveVillains)
        {
            // Debug.Log("[Spawner] Cap reached. No spawn needed.");
            return;
        }

        // Find an unoccupied spawn point
        bool spawnedOne = false;
        foreach (Transform spawnPoint in spawnPoints)
        {
            if (spawnPoint == null)
            {
                // Debug.LogWarning("[Spawner] Skipping a null spawnPoint entry.");
                continue;
            }

            // Check if a villain is already at this point
            Collider[] nearby = Physics.OverlapSphere(spawnPoint.position, 2f);
            bool pointIsOccupied = false;
            foreach (Collider col in nearby)
            {
                if (col.CompareTag(villainTag))
                {
                    // Debug.Log($"[Spawner] spawnPoint '{spawnPoint.name}' is OCCUPIED by '{col.gameObject.name}'. Skipping.");
                    pointIsOccupied = true;
                    break;
                }
            }

            if (!pointIsOccupied)
            {
                // Debug.Log($"[Spawner] spawnPoint '{spawnPoint.name}' is FREE. Spawning villain here at {spawnPoint.position}");
                GameObject spawnedObj = Instantiate(villainPrefab, spawnPoint.position, spawnPoint.rotation);

                if (spawnedObj != null)
                {
                    
                    // Debug.Log($"[Spawner] SUCCESS: Spawned '{spawnedObj.name}' | Tag: '{spawnedObj.tag}' | Position: {spawnedObj.transform.position}");
                }
                else
                {
                    
                    // Debug.LogError("[Spawner] Instantiate returned NULL!");
                }

                spawnedOne = true;
                break; // Spawn one per interval only
            }
        }

        if (!spawnedOne)
        {
            
            // Debug.Log("[Spawner] All spawn points are occupied. No spawn this interval.");
        }
    }
}