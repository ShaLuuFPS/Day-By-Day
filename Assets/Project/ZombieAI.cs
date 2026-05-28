using UnityEngine;

public class ZombieAI : MonoBehaviour
{
    [Header("移动设置")]
    public float moveSpeed = 2.5f;       // 僵尸的移动速度
    public float stoppingDistance = 2.0f; // 距离玩家的停止距离（现在调2~3米就很完美）

    private Transform playerTransform;   // 玩家的坐标组件
    private Rigidbody rb;                // 刚体组件

    [Header("攻击设置")]
    public float attackDamage = 10f; // 僵尸每次攻击扣多少血
    public float attackRate = 1.0f;  // 攻击频率（秒/次，比如 1 秒咬一次）
    private float nextAttackTime = 0f; // 下一次可以攻击的时间




    void Start()
    {
        // 自动获取自身的刚体组件
        rb = GetComponent<Rigidbody>();

        // 自动通过标签找到玩家
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }

    void FixedUpdate() // 涉及物理移动时，用 FixedUpdate 代替 Update 是标准的工业规范，最稳定
    {
        if (playerTransform == null || rb == null) return;

        // 1. 计算与玩家的距离和方向
        Vector3 directionToPlayer = playerTransform.position - transform.position;
        directionToPlayer.y = 0f; // 抹平高度差
        float currentDistance = directionToPlayer.magnitude;

        // 2. 始终死死盯住玩家
        if (directionToPlayer != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
            rb.MoveRotation(Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * 10f));
        }

        // 3. 【纯粹无污染的绝对判定】
        if (currentDistance <= stoppingDistance)
        {
            // 如果在停止距离内，直接把刚体的物理速度完全抽干复位，原地立定！
            rb.linearVelocity = Vector3.zero; // Unity 6 新版中 velocity 写作 linearVelocity
        }
        else
        {
            // 如果还没到，正常给它一个朝向玩家的世界物理移动速度
            Vector3 targetVelocity = directionToPlayer.normalized * moveSpeed;
            targetVelocity.y = rb.linearVelocity.y; // 保持原本的垂直速度（防止影响重力）
            
            rb.linearVelocity = targetVelocity;
        }
    }

    // 当僵尸持续撞击（贴着）有碰撞体的主角时，这个函数会自动高频触发
    void OnCollisionStay(Collision collision)
    {
        // 检查撞到的是不是玩家
        if (collision.gameObject.CompareTag("Player"))
        {
            // 检查攻击冷却时间是否到了
            if (Time.time >= nextAttackTime)
            {
                // 尝试获取玩家身上的 PlayerHealth 脚本
                PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    // 狠狠给玩家来一爪子！
                    playerHealth.TakeDamage(attackDamage);

                    // 刷新下次攻击的冷却时间戳
                    nextAttackTime = Time.time + attackRate;
                }
            }
        }
    }
}