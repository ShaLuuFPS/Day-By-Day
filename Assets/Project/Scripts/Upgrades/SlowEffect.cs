using UnityEngine;

/// <summary>
/// 减速效果组件 —— 附加到敌人上，临时降低移速，到期自毁
/// </summary>
public class SlowEffect : MonoBehaviour
{
    private float slowMultiplier;
    private float timer;
    private ZombieAI zombieAI;
    private bool applied = false;

    public void Initialize(float multiplier, float duration)
    {
        slowMultiplier = multiplier;
        timer = duration;
    }

    /// <summary>刷新持续时间（不改变减速倍数）</summary>
    public void RefreshDuration(float newDuration)
    {
        timer = newDuration;
    }

    void Start()
    {
        zombieAI = GetComponent<ZombieAI>();
        if (zombieAI != null)
        {
            float newSpeed = zombieAI.OriginalMoveSpeed * slowMultiplier;
            zombieAI.SetMoveSpeed(newSpeed);
            applied = true;

            Debug.Log($"[SlowEffect] {name} 减速: {zombieAI.OriginalMoveSpeed} → {newSpeed} (×{slowMultiplier})");
        }
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
            Remove();
    }

    void Remove()
    {
        // 恢复原始移速
        if (zombieAI != null && applied)
        {
            zombieAI.SetMoveSpeed(zombieAI.OriginalMoveSpeed);
            Debug.Log($"[SlowEffect] {name} 减速已恢复: {zombieAI.OriginalMoveSpeed}");
        }

        Destroy(this);
    }

    void OnDestroy()
    {
        // 防止因外部销毁导致移速未恢复
        if (zombieAI != null && applied)
            zombieAI.SetMoveSpeed(zombieAI.OriginalMoveSpeed);
    }
}
