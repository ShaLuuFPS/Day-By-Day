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
        Vector2 randomCirclePoint = UnityEngine.Random.insideUnitCircle.normalized;
        float randomDistance = UnityEngine.Random.Range(minRadius, maxRadius);
        Vector3 spawnOffset = new Vector3(randomCirclePoint.x, 0f, randomCirclePoint.y) * randomDistance;
        Vector3 spawnPosition = playerTransform.position + spawnOffset;
        spawnPosition.y = spawnYHeight;

        // 按权重随机抽取一条生成条目
        ZombieSpawnEntry selectedEntry = PickEntryByWeight();
        ZombieData selectedData = selectedEntry?.zombieData;

        if (selectedEntry == null)
            Debug.LogWarning("[Spawner] spawnEntries 为空，所有僵尸将使用默认配置！请在 Inspector 添加至少一条生成条目。");

        // 优先使用条目专属预制体，回退到 Spawner 默认预制体
        GameObject prefabToUse = (selectedEntry != null && selectedEntry.overridePrefab != null)
            ? selectedEntry.overridePrefab
            : zombiePrefab;

        if (prefabToUse == null) return;

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
