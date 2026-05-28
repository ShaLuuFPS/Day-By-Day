using UnityEngine;

public class ZombieAI : MonoBehaviour
{
    [Header("移动设置")]
    public float moveSpeed = 3.5f;       // 僵尸的移动速度
    public float stoppingDistance = 2.0f; // 距离玩家的停止距离

    [Header("优化：攻击设置")]
    public float attackRange = 2.5f;     // 攻击范围！必须大于或等于停止距离（比如2.5米就能打到2米外的玩家）
    public float attackDamage = 10f;     // 每次攻击扣多少血
    public float attackRate = 1.0f;      // 攻击冷却时间（1秒/次）

    private float nextAttackTime = 0f;   // 每个敌人独享的下一次攻击时间戳
    private Transform playerTransform;   // 玩家的坐标组件
    private Rigidbody rb;                // 刚体组件

    void Start()
    {
        rb = GetComponent<Rigidbody>();

    }

    void FixedUpdate()
    {
        if (rb == null) return;

        // 💡 【自主追踪自我修复】：如果身上没有玩家引用，或者引用丢失了，每帧自己去找！
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
            else
            {
                return; // 如果场景里真的没主角，再彻底拦截
            }
        }

        // 1. 计算与玩家的距离和方向
        Vector3 directionToPlayer = playerTransform.position - transform.position;

        directionToPlayer.y = 0f; // 抹平高度差
        float currentDistance = directionToPlayer.magnitude;

        // 2. 始终死死盯着玩家
        if (directionToPlayer != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
            rb.MoveRotation(Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * 10f));
        }

        // 3. 【判定 A：是否触发攻击】
        // 只要玩家进入了攻击范围，且 1 秒的冷却时间到了
        if (currentDistance <= attackRange && Time.time >= nextAttackTime)
        {
            ExecuteAttack();
        }

        // 4. 【判定 B：是否停止移动】
        if (currentDistance <= stoppingDistance)
        {
            // 到了停止距离，立马立定
            rb.linearVelocity = Vector3.zero;
        }
        else
        {
            // 没到距离，继续前进
            Vector3 targetVelocity = directionToPlayer.normalized * moveSpeed;
            targetVelocity.y = rb.linearVelocity.y;
            rb.linearVelocity = targetVelocity;
        }
    }

    // 执行攻击的私有函数
    void ExecuteAttack()
    {
        // 尝试获取玩家的血量组件
        PlayerHealth playerHealth = playerTransform.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            // 狠狠咬一口
            playerHealth.TakeDamage(attackDamage);

            // ⭐ 核心复位：刷新【当前这只僵尸】的专属冷却时间戳
            // Time.time 是游戏运行的总时间，加上 1 秒就是它下一次能打的时间
            nextAttackTime = Time.time + attackRate;

            Debug.Log($"{gameObject.name} 发动了攻击！下次攻击在 {nextAttackTime} 秒后");
        }
    }
}