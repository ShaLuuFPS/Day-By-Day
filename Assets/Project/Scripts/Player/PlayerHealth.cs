using UnityEngine;
using UnityEngine.UI;

// ✨ 核心重构：让 PlayerHealth 挂上 IResettable 契约！
public class PlayerHealth : MonoBehaviour, IResettable
{
    [Header("生命值设置")]
    public float maxHealth = 100f;
    private float currentHealth;

    [Header("UI 连线")]
    public Image healthBarFill;

    // 供其他脚本（PlayerMovement / PlayerShooting）读取：主角是否已经阵亡
    public bool IsDead { get; private set; } = false;

    void Start()
    {
        // 游戏刚启动时初始化血量
        ResetData();
    }

    // ✨ 核心重构：彻底取缔 InitGame()，让它变成标准的契约实现！
    public void ResetData()
    {
        IsDead = false;
        currentHealth = maxHealth;
        UpdateHealthBar();

        Debug.Log("【数据重置】玩家生命值已回满！");
    }

    public void TakeDamage(float damageAmount)
    {
        if (currentHealth <= 0) return;

        currentHealth -= damageAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        // 玩家受伤 → 蓝色伤害数字
        DamageNumberManager.Spawn(transform, transform.position, damageAmount, new Color(0.3f, 0.6f, 1f));

        Debug.Log($"玩家受到伤害！剩余血量: {currentHealth}/{maxHealth}");

        UpdateHealthBar();

        if (currentHealth <= 0)
        {
            PlayerDie();
        }
    }

    /// <summary>回复指定血量（吸血升级等使用）</summary>
    public void Heal(float amount)
    {
        if (currentHealth <= 0 || IsDead) return;
        currentHealth = Mathf.Clamp(currentHealth + amount, 0f, maxHealth);
        UpdateHealthBar();
    }

    void UpdateHealthBar()
    {
        if (healthBarFill == null) return;
        healthBarFill.fillAmount = currentHealth / maxHealth;
    }

    void PlayerDie()
    {
        IsDead = true;

        // 使用 GameStateManager 的事件系统通知死亡
        if (GameStateManager.OnPlayerDied != null)
        {
            GameStateManager.OnPlayerDied.Invoke();
        }

        Debug.Log("玩家已阵亡！游戏定格。");
    }
}