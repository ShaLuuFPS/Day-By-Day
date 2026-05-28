using UnityEngine;
using UnityEngine.UI; // 必须引入 UI 命名空间

public class PlayerHealth : MonoBehaviour
{
    [Header("生命值设置")]
    public float maxHealth = 100f;   // 主角最大血量
    private float currentHealth;    // 主角当前血量

    [Header("UI 连线")]
    public Image healthBarFill;     // 拖入你刚刚创建的 Fill 图片

    void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthBar(); // 游戏开始初始化血条
    }

    // 【公开受伤害接口】：当被僵尸咬到时，由僵尸调用这个函数
    public void TakeDamage(float damageAmount)
    {
        currentHealth -= damageAmount;
        // 限制血量不能低于 0
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        Debug.Log($"玩家受到伤害！剩余血量: {currentHealth}/{maxHealth}");

        // 同步刷新屏幕上的血条
        UpdateHealthBar();

        if (currentHealth <= 0)
        {
            PlayerDie();
        }
    }

    // 刷新血条 UI
    void UpdateHealthBar()
    {
        if (healthBarFill == null) return;

        // 计算血量百分比（Filled 模式的 fillAmount 刚好接收 0 到 1 之间的数字）
        healthBarFill.fillAmount = currentHealth / maxHealth;
    }

    void PlayerDie()
    {
        Debug.Log("游戏结束，玩家已死亡！");
        // 这里后期可以触发“显示游戏失败面板”或“重新加载场景”
    }
}