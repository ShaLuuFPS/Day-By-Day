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

    void Start()
    {
        currentHealth = maxHealth;

        // 自动获取自身的渲染器和原本材质颜色，用来做受击变色反馈
        enemyRenderer = GetComponent<Renderer>();
        if (enemyRenderer != null)
        {
            originalColor = enemyRenderer.material.color;
        }
    }

    // 【核心公开接口】：子弹打中时调用
    public void TakeDamage(float damageAmount)
    {
        currentHealth -= damageAmount;
        Debug.Log($"{gameObject.name} 受到 {damageAmount} 点伤害！剩余血量: {currentHealth}/{maxHealth}");

        // 1. 展示受击动作：让怪物的身体闪烁一下红色
        ShowHurtEffect();

        // 2. 检查死亡状态
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // 执行受击视觉反馈
    void ShowHurtEffect()
    {
        if (enemyRenderer == null) return;

        // 先把颜色变成受击红
        enemyRenderer.material.color = hurtColor;

        // 并在 0.1 秒后自动恢复原样（Invoke 是 Unity 自带的延时呼叫工具）
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

        // 3. 动作展示死亡：现在先直接毁灭。后期我们可以在这里触发“倒地动画”或者“爆出一滩血水/零件”
        Destroy(gameObject);
    }
}