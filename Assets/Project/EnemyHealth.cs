using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("生命值设置")]
    public float maxHealth = 30f;   // 最大血量
    private float currentHealth;    // 当前血量

    void Start()
    {
        // 游戏一开始，把血量补满
        currentHealth = maxHealth;
    }

    // 【核心公开方法】：允许外部（比如子弹）调用，用来对这个敌人造成伤害
    public void TakeDamage(float damageAmount)
    {
        // 1. 扣除对应的血量
        currentHealth -= damageAmount;
        Debug.Log($"{gameObject.name} 受到 {damageAmount} 点伤害！剩余血量: {currentHealth}/{maxHealth}");

        // 2. 每次受伤害，都检查一下自己是不是死掉了
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // 处理死亡状态的私有函数
    void Die()
    {
        Debug.Log($"{gameObject.name} 已死亡！");

        // 3. 死亡反馈：这里先用最直接的方法，让敌人从世界上消失
        Destroy(gameObject);
    }
}