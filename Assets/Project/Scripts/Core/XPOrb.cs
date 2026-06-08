using UnityEngine;

/// <summary>
/// 经验球 —— 击杀敌人掉落，大小随 XP 缩放，靠近玩家时自动飞向吸收。
/// 贴地用 DropHelper，闪烁+自动销毁用 TimedDestroyer（不重复造轮子）。
/// </summary>
public class XPOrb : MonoBehaviour, IResettable
{
    [Tooltip("该经验球提供的 XP 量（由 EnemyHealth 设置）")]
    public float xpAmount = 10f;

    [Header("缩放")]
    public float maxXPForMaxScale = 100f;
    public float maxScale = 0.6f;
    public float minScale = 0.3f;

    [Header("吸引")]
    public float attractSpeed = 8f;

    [Header("浮动动画")]
    public float floatAmplitude = 0.3f;
    public float floatFrequency = 2f;

    /// <summary>
    /// 全局拾取范围 —— 升级系统可通过此字段动态修改
    /// </summary>
    public static float GlobalPickupRadius = 3f;

    private Transform playerTransform;
    private PlayerXP playerXP;
    private float floatTimer;
    private float groundY;
    private float currentScale;
    private float halfScale;
    private bool isAttracted;

    void Start()
    {
        // 1. 缩放
        float t = Mathf.Clamp01(xpAmount / maxXPForMaxScale);
        currentScale = Mathf.Lerp(minScale, maxScale, t);
        halfScale = currentScale * 0.5f;
        transform.localScale = Vector3.one * currentScale;

        // 2. 贴地（统一工具）
        DropHelper.PlaceOnGround(transform);
        groundY = DropHelper.GetGroundY(transform.position);

        // 3. 复用 TimedDestroyer 处理闪烁+自动销毁
        TimedDestroyer td = GetComponent<TimedDestroyer>();
        if (td == null)
            td = gameObject.AddComponent<TimedDestroyer>();
        td.totalLifeTime = 30f;
        td.blinkDuration = 5f;
        td.blinkInterval = 0.25f;
        td.ResetTimer(); // 确保用新参数重启（可能已有旧协程在跑）

        FindPlayer();
    }

    void FindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            playerXP = player.GetComponent<PlayerXP>();
        }
    }

    public void ResetData()
    {
        Destroy(gameObject);
    }

    void Update()
    {
        if (playerTransform == null)
        {
            FindPlayer();
            if (playerTransform == null) return;
        }

        floatTimer += Time.deltaTime;
        Vector3 playerPos = playerTransform.position;
        float distance = Vector3.Distance(
            new Vector3(transform.position.x, 0, transform.position.z),
            new Vector3(playerPos.x, 0, playerPos.z));

        if (!isAttracted && distance <= GlobalPickupRadius)
            isAttracted = true;

        if (!isAttracted)
        {
            // 悬浮动画：底部不穿地
            float offset = (Mathf.Sin(floatTimer * floatFrequency) * 0.5f + 0.5f) * floatAmplitude;
            Vector3 p = transform.position;
            p.y = groundY + halfScale + offset;
            transform.position = p;
            return;
        }

        // 飞向玩家
        Vector3 target = new Vector3(playerPos.x, groundY + halfScale, playerPos.z);
        float step = attractSpeed * Time.deltaTime;
        step += step * 0.5f * (1f - distance / GlobalPickupRadius);
        transform.position = Vector3.MoveTowards(transform.position, target, step);

        if (distance < 0.5f)
        {
            if (playerXP != null)
                playerXP.AddXP(xpAmount);
            Destroy(gameObject);
        }
    }
}
