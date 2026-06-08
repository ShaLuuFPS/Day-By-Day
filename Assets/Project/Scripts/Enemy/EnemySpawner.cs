using UnityEngine;

/// <summary>
/// 僵尸生成条目：配置 + 权重 + 可选专属预制体
/// </summary>
[System.Serializable]
public class ZombieSpawnEntry
{
    [Tooltip("僵尸配置资产")]
    public ZombieData zombieData;
    [Tooltip("生成权重（数值越大出现概率越高）")]
    public float weight = 1f;
    [Tooltip("可选：该类型专属预制体（为空则使用 Spawner 的默认预制体）")]
    public GameObject overridePrefab;
}

public class EnemySpawner : MonoBehaviour, IResettable
{
    [Header("僵尸预制体（通用）")]
    public GameObject zombiePrefab;
    public Transform playerTransform;

    [Header("生成控制")]
    public bool autoSpawn = true;
    public float spawnInterval = 3.0f;
    public int maxZombies = 15;

    [Header("范围控制")]
    public float minRadius = 15f;
    public float maxRadius = 30f;
    public float spawnYHeight = 3f;

    [Header("多僵尸类型（至少添加一个条目）")]
    public ZombieSpawnEntry[] spawnEntries;

    private float timer = 0f;

    void Start()
    {
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }
    }

    void Update()
    {
        if (!autoSpawn) return;
        if (playerTransform == null || zombiePrefab == null) return;

        int currentZombieCount = Object.FindObjectsByType<ZombieAI>().Length;

        if (currentZombieCount < maxZombies)
        {
            timer += Time.deltaTime;
            if (timer >= spawnInterval)
            {
                SpawnOneEnemy();
                timer = 0f;
            }
        }
    }

    public void ResetData()
    {
        timer = 0f;
    }

    public void SpawnOneEnemy()
    {
        if (playerTransform == null) return;

        // 按权重随机抽取一条生成条目（只抽一次）
        ZombieSpawnEntry selectedEntry = PickEntryByWeight();
        ZombieData selectedData = selectedEntry?.zombieData;

        GameObject prefabToUse = (selectedEntry != null && selectedEntry.overridePrefab != null)
            ? selectedEntry.overridePrefab
            : zombiePrefab;

        if (prefabToUse == null) return;

        // 重试找有效地面（避免虚空生成）
        const int maxRetries = 10;
        Vector3 spawnPosition = Vector3.zero;
        bool validGround = false;

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            Vector2 rp = UnityEngine.Random.insideUnitCircle.normalized;
            float rd = UnityEngine.Random.Range(minRadius, maxRadius);
            Vector3 candidate = playerTransform.position + new Vector3(rp.x, 0f, rp.y) * rd;

            Vector3 rayOrigin = new Vector3(candidate.x, 100f, candidate.z);
            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 200f) && hit.point.y > -10f)
            {
                spawnPosition = hit.point + Vector3.up * 0.1f;
                validGround = true;
                break;
            }
        }

        if (!validGround)
        {
            Debug.LogWarning("[Spawner] " + maxRetries + " 次重试未找到有效地面，放弃生成");
            return;
        }

        GameObject newZombie = Instantiate(prefabToUse, spawnPosition, Quaternion.identity);
        newZombie.name = selectedData != null ? selectedData.zombieName : "Spawned_Zombie";
        newZombie.transform.SetParent(this.transform);

        // 注入 ZombieData 配置
        if (selectedData != null)
        {
            Debug.Log($"[Spawner] 生成 {selectedData.zombieName}（权重={selectedEntry.weight}）");

            ZombieAI ai = newZombie.GetComponent<ZombieAI>();
            if (ai != null)
            {
                ai.zombieData = selectedData;
                ai.ApplyConfig();
            }

            EnemyHealth health = newZombie.GetComponent<EnemyHealth>();
            if (health != null)
            {
                health.zombieData = selectedData;
                health.ApplyConfig();
            }
        }
    }

    /// <summary>
    /// 按权重随机抽取一个生成条目
    /// </summary>
    ZombieSpawnEntry PickEntryByWeight()
    {
        if (spawnEntries == null || spawnEntries.Length == 0) return null;

        float totalWeight = 0f;
        foreach (var entry in spawnEntries)
            totalWeight += entry.weight;

        if (totalWeight <= 0f) return spawnEntries[0];

        float roll = UnityEngine.Random.Range(0f, totalWeight);
        float cumulative = 0f;
        foreach (var entry in spawnEntries)
        {
            cumulative += entry.weight;
            if (roll <= cumulative)
                return entry;
        }

        return spawnEntries[0];
    }

    void OnDrawGizmosSelected()
    {
        if (playerTransform == null) return;
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(playerTransform.position, minRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(playerTransform.position, maxRadius);
    }
}
