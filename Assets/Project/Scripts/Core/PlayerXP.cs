using UnityEngine;

/// <summary>
/// 玩家经验值组件 —— 跟踪经验、等级、升级阈值
/// </summary>
public class PlayerXP : MonoBehaviour, IResettable
{
    [Header("配置")]
    [Tooltip("升到 Lv2 所需 XP")]
    public float baseXP = 50f;
    [Tooltip("每级额外增加的经验量")]
    public float xpIncrement = 50f;

    [Header("状态（只读）")]
    [SerializeField] private float currentXP = 0f;
    [SerializeField] private float xpToNextLevel = 50f;
    [SerializeField] private int currentLevel = 1;

    /// <summary>经验值变化时触发（当前XP，升级所需XP）</summary>
    public static event System.Action<float, float> OnXPGained;
    /// <summary>升级时触发（新等级）</summary>
    public static event System.Action<int> OnLevelUp;

    public float CurrentXP => currentXP;
    public float XPToNextLevel => xpToNextLevel;
    public int CurrentLevel => currentLevel;

    void Start()
    {
        xpToNextLevel = baseXP;
    }

    /// <summary>
    /// 增加经验值，自动检测升级
    /// </summary>
    public void AddXP(float amount)
    {
        if (amount <= 0f) return;

        currentXP += amount;
        Debug.Log($"[PlayerXP] +{amount} XP ({currentXP:F0}/{xpToNextLevel:F0}), Lv{currentLevel}");

        OnXPGained?.Invoke(currentXP, xpToNextLevel);

        // 可能连续升多级
        while (currentXP >= xpToNextLevel)
        {
            currentXP -= xpToNextLevel;
            currentLevel++;
            xpToNextLevel = baseXP + (currentLevel - 1) * xpIncrement;

            Debug.Log($"[PlayerXP] 🎉 升级！Lv{currentLevel}，下一级需要 {xpToNextLevel:F0} XP");
            OnLevelUp?.Invoke(currentLevel);
        }
    }

    public void ResetData()
    {
        currentXP = 0f;
        currentLevel = 1;
        xpToNextLevel = baseXP;
        Debug.Log("[PlayerXP] 🔄 经验值已重置");
    }
}
