using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("生成配置")]
    public GameObject zombiePrefab;      // 你的僵尸预制体（ZombieTarget）
    public Transform playerTransform;    // 玩家的 Transform

    [Header("生成控制")]
    public float spawnInterval = 3.0f;   // 每隔几秒生成一只
    public int maxZombies = 15;          // 场上最多同时存在多少只僵尸

    [Header("范围控制（阶段二核心）")]
    public float minRadius = 15f;        // 最小距离（内圈，前期测试可以调小，比如 2）
    public float maxRadius = 30f;        // 最大距离（外圈）
    public float spawnYHeight = 3f;    // 生成时的 Y 轴高度（确保僵尸脚踩在地面上）

    private float timer = 0f;

    void Start()
    {
        // 如果没有手动手动拖入玩家，尝试自动抓取
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }
    }

    void Update()
    {
        if (playerTransform == null || zombiePrefab == null) return;

        // 统计当前场景里还活着多少只僵尸
        int currentZombieCount = Object.FindObjectsByType<ZombieAI>().Length;

        // 如果没达到上限，开始倒计时刷怪
        if (currentZombieCount < maxZombies)
        {
            timer += Time.deltaTime;
            if (timer >= spawnInterval)
            {
                SpawnZombie();
                timer = 0f; // 重置计时器
            }
        }
    }

    void SpawnZombie()
    {
        // 1. 在圆形范围内随机生成一个 2D 方向向量
        Vector2 randomCirclePoint = Random.insideUnitCircle.normalized;

        // 2. 在【内圈】和【外圈】之间随机切一刀，得出最终的随机距离
        float randomDistance = Random.Range(minRadius, maxRadius);

        // 3. 将 2D 圆形坐标转化为 3D 世界坐标
        Vector3 spawnOffset = new Vector3(randomCirclePoint.x, 0f, randomCirclePoint.y) * randomDistance;
        Vector3 spawnPosition = playerTransform.position + spawnOffset;

        // 强制修正高度，防止僵尸出生在地下或者天上
        spawnPosition.y = spawnYHeight;

        // 4. 精准实例化（创建）僵尸
        GameObject newZombie = Instantiate(zombiePrefab, spawnPosition, Quaternion.identity);

        // 给生成的僵尸改个名字，方便在层级面板看数量
        newZombie.name = "Spawned_Zombie";
    }

    // ⭐ 调试小福利：在场景视图里用线条画出内圈和外圈，方便肉眼看刷怪范围
    void OnDrawGizmosSelected()
    {
        if (playerTransform == null) return;

        // 蓝圈：内圈（禁刷区）
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(playerTransform.position, minRadius);

        // 红圈：外圈（最大生成区）
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(playerTransform.position, maxRadius);
    }
}