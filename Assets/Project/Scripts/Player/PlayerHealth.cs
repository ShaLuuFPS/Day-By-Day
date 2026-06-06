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
    public GameObject gameOverPanel;

    [Header("重置解耦：管理器连线")]
    public GameObject spawnerManager;

    private Vector3 playerInitialPosition; // 记录玩家初始位置

    // 供其他脚本（PlayerMovement / PlayerShooting）读取：主角是否已经阵亡
    public bool IsDead { get; private set; } = false;

    void Start()
    {
        // 游戏刚开始时，记录出生点
        playerInitialPosition = transform.position;

        // 游戏刚启动时的初始化，现在也可以直接顺从契约调用了
        ResetData();
    }

    // ✨ 核心重构：彻底取缔 InitGame()，让它变成标准的契约实现！
    public void ResetData()
    {
        IsDead = false;
        currentHealth = maxHealth;
        UpdateHealthBar();

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        // 核心复位：把 Unity 的游戏时间轴恢复正常
        Time.timeScale = 1f;

        Debug.Log("【数据重置】玩家生命值已回满，游戏时间轴恢复！");
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

    void UpdateHealthBar()
    {
        if (healthBarFill == null) return;
        healthBarFill.fillAmount = currentHealth / maxHealth;
    }

    void PlayerDie()
    {
        IsDead = true;
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        // 死亡时时间静止
        Time.timeScale = 0f;
        Debug.Log("玩家已阵亡！游戏定格。");
    }

    // ⭐⭐⭐ 核心重置函数：绑定给“重新开始”按钮
    public void RestartGame()
    {
        Debug.Log("【开始一键清理重置现场...】");

        // 1. 物理清场：毁灭 SpawnerManager 下的所有僵尸 + 清理掉落物
        if (spawnerManager != null)
        {
            foreach (Transform child in spawnerManager.transform)
            {
                Destroy(child.gameObject);
            }
        }

        // 清理所有掉落物（标记了 LootDrop 组件）
        LootDrop[] drops = FindObjectsByType<LootDrop>(FindObjectsInactive.Include);
        foreach (LootDrop drop in drops)
            Destroy(drop.gameObject);

        // 2. 玩家物理复位：把玩家坐标拉回初始点
        transform.position = playerInitialPosition;

        // 3. 刚体速度物理清空（防止带着死亡前的速度惯性滑行）
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // 4. ✨【一键降维广播】
        // 找出场景中所有的 MonoBehaviour 脚本（包括死后被禁用的）
        MonoBehaviour[] allScripts = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include);

        // 遍历每一个脚本，谁签了契约谁就自理
        foreach (MonoBehaviour mono in allScripts)
        {
            if (mono is IResettable resettableTarget)
            {
                // 当循环扫描到自己（PlayerHealth）时，它也会自动调用上面的 ResetData()！
                resettableTarget.ResetData();
            }
        }

        Debug.Log("【重置完毕！】新一轮战斗开始。");
    }
}