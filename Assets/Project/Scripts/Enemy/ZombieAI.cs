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

    private float attackCooldownTimer = 0f;  //冷却计时器
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
        directionToPlayer.y = 0f;
        float currentDistance = directionToPlayer.magnitude;

        // 2. 始终死死盯着玩家
        if (directionToPlayer != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
            rb.MoveRotation(Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * 10f));
        }

        // ✨ 核心美化：冷却时间在每个物理帧里自己往下扣减，直到扣到 0 为止
        if (attackCooldownTimer > 0f)
        {
            attackCooldownTimer -= Time.fixedDeltaTime;
        }

        // 3. 【判定 A：是否触发攻击】
        // 距离够了，且冷却计时器已经归零（可以攻击了）
        if (currentDistance <= attackRange && attackCooldownTimer <= 0f)
        {
            ExecuteAttack();
        }

        // 4. 【判定 B：是否停止移动】
        if (currentDistance <= stoppingDistance)
        {
            rb.linearVelocity = Vector3.zero;
        }
        else
        {
            Vector3 targetVelocity = directionToPlayer.normalized * moveSpeed;
            targetVelocity.y = rb.linearVelocity.y;
            rb.linearVelocity = targetVelocity;
        }
    }

    // 执行攻击的私有函数
    void ExecuteAttack()
    {
        PlayerHealth playerHealth = playerTransform.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(attackDamage);

            // ✨ 核心美化：攻击完，直接把冷却计时器充满（设为 1.0 秒）
            attackCooldownTimer = attackRate;

            // 现在的 Log 极其纯净，永远只会在 0 到 1 之间复位，再也不会打印三十几秒那种大数字了！
            Debug.Log($"{gameObject.name} 发动了攻击！冷却重置为 {attackCooldownTimer} 秒");
        }
    }
}