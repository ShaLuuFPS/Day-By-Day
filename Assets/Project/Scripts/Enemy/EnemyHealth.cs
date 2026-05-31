using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("生命值设置")]
    public float maxHealth = 30f;   // 最大血量
    private float currentHealth;    // 当前血量

    [Header("受击视觉反馈")]
    public Color hurtColor = Color.red; // 受击时闪烁的颜色（默认红色）
    private Color originalColor;        // 僵尸原本的颜色
    private Renderer enemyRenderer;     // 僵尸的渲染器

    // 🌟 核心新增：掉落机制配置
    [Header("💰 战利品掉落配置")]
    [Tooltip("把我们在 Prefabs 文件夹里做好的补给箱拉到这里")]
    public GameObject ammoBoxPrefab;
    [Range(0f, 1f)]
    [Tooltip("掉落概率：0.3 代表 30% 概率掉落")]
    public float dropProbability = 0.3f;

    void Start()
    {
        currentHealth = maxHealth;
        enemyRenderer = GetComponent<Renderer>();
        if (enemyRenderer != null)
        {
            originalColor = enemyRenderer.material.color;
        }
    }

    public void TakeDamage(float damageAmount)
    {
        currentHealth -= damageAmount;
        Debug.Log($"{gameObject.name} 受到 {damageAmount} 点伤害！剩余血量: {currentHealth}/{maxHealth}");
        ShowHurtEffect();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void ShowHurtEffect()
    {
        if (enemyRenderer == null) return;
        enemyRenderer.material.color = hurtColor;
        Invoke("ResetColor", 0.1f);
    }

    void ResetColor()
    {
        if (enemyRenderer != null)
        {
            enemyRenderer.material.color = originalColor;
        }
    }

    // 执行死亡动作
    void Die()
    {
        Debug.Log($"{gameObject.name} 已触发死亡状态！");

        // 🌟 核心新增：概率生成补给箱
        if (ammoBoxPrefab != null)
        {
            // Unity 随机算命：Random.value 会生成 0.0 到 1.0 之间的浮点数
            if (Random.value <= dropProbability)
            {
                // 在敌人死亡前的精确位置（transform.position）朝上抬起 0.1 米，生出一个补给箱！
                Vector3 spawnPos = transform.position + Vector3.up * 0.1f;
                Instantiate(ammoBoxPrefab, spawnPos, Quaternion.identity);
                Debug.Log("🎉 运气大爆发！敌人掉落了一个备弹补给箱！");
            }
        }

        Destroy(gameObject);
    }
}