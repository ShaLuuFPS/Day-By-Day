using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 升级效果中央调度器 —— 监听升级选择，创建对应 Effect，在子弹命中/敌人死亡时转发通知
/// </summary>
public class UpgradeManager : MonoBehaviour, IResettable
{
    // ── 静态属性：Bullet/EnemyHealth 等系统通过此读取当前激活的升级效果 ──

    /// <summary>穿透弹：新子弹初始化时读取此值</summary>
    public static int PierceCount { get; set; } = 0;

    /// <summary>分裂弹：命中时是否触发分裂</summary>
    public static bool SplitEnabled { get; set; } = false;

    /// <summary>尸体爆炸：敌人死亡时是否触发爆炸</summary>
    public static bool CorpseExplosionEnabled { get; set; } = false;

    /// <summary>减速弹：命中时是否附加减速</summary>
    public static bool SlowEnabled { get; set; } = false;

    /// <summary>电磁弹：命中时是否连锁伤害</summary>
    public static bool EMPEnabled { get; set; } = false;

    // ── 实例 ──

    private List<IUpgradeEffect> activeEffects = new List<IUpgradeEffect>();

    void OnEnable()
    {
        LevelUpManager.OnUpgradeChosen += OnUpgradeChosen;
        Bullet.OnBulletHitEnemy += OnBulletHitEnemy;
        EnemyHealth.OnAnyEnemyDied += OnAnyEnemyDied;
    }

    void OnDisable()
    {
        LevelUpManager.OnUpgradeChosen -= OnUpgradeChosen;
        Bullet.OnBulletHitEnemy -= OnBulletHitEnemy;
        EnemyHealth.OnAnyEnemyDied -= OnAnyEnemyDied;
    }

    void OnUpgradeChosen(UpgradeData data)
    {
        IUpgradeEffect effect = CreateEffect(data.upgradeType);
        if (effect == null)
        {
            Debug.LogWarning($"[UpgradeManager] 未知升级类型: {data.upgradeType}");
            return;
        }

        effect.OnApply();
        activeEffects.Add(effect);
        Debug.Log($"[UpgradeManager] 已激活升级: {data.upgradeName} ({data.upgradeType})");
    }

    IUpgradeEffect CreateEffect(UpgradeType type)
    {
        switch (type)
        {
            case UpgradeType.PiercingRounds:      return new PiercingRoundsEffect();
            case UpgradeType.SplitProjectile:     return new SplitProjectileEffect();
            case UpgradeType.CorpseExplosion:     return new CorpseExplosionEffect();
            case UpgradeType.SlowBullet:          return new SlowBulletEffect();
            case UpgradeType.EMPChain:            return new EMPChainEffect();
            case UpgradeType.KillRestoreStamina:  return new KillRestoreStaminaEffect(GetComponent<PlayerStamina>());
            default: return null;
        }
    }

    // ── 事件转发 ──

    void OnBulletHitEnemy(Bullet bullet, EnemyHealth enemy)
    {
        foreach (var e in activeEffects)
            e.OnBulletHit(bullet, enemy);
    }

    void OnAnyEnemyDied(EnemyHealth enemy)
    {
        foreach (var e in activeEffects)
            e.OnEnemyKilled(enemy);
    }

    public void ResetData()
    {
        activeEffects.Clear();

        // 重置所有静态属性
        PierceCount = 0;
        SplitEnabled = false;
        CorpseExplosionEnabled = false;
        SlowEnabled = false;
        EMPEnabled = false;

        Debug.Log("[UpgradeManager] 🔄 升级系统已重置");
    }
}
