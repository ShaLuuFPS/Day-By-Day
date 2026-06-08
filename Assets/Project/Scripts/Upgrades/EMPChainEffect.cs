using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 电磁弹 —— 子弹命中敌人时概率连锁附近敌人（闪电链）
/// 参数：30% 伤害、首次 10% 触发、链上敌人 50% 继续触发、最多 5 只
/// </summary>
public class EMPChainEffect : IUpgradeEffect
{
    private const float ChainRange = 5f;
    private const int MaxChains = 5;
    private const float ChainDamageMultiplier = 0.3f;
    private const float FirstTriggerChance = 0.1f;   // 首次连锁 10%
    private const float ContinueTriggerChance = 0.5f; // 链上继续 50%

    public void OnApply()
    {
        UpgradeManager.EMPEnabled = true;
    }

    public void OnEnemyKilled(EnemyHealth enemy) { }

    public void OnBulletHit(Bullet bullet, EnemyHealth enemy)
    {
        // 首次触发 10%
        if (Random.value > FirstTriggerChance) return;

        HashSet<EnemyHealth> chained = new HashSet<EnemyHealth> { enemy };
        ChainFrom(enemy.transform.position, bullet, chained, MaxChains);
    }

    void ChainFrom(Vector3 origin, Bullet sourceBullet, HashSet<EnemyHealth> chained, int remainingChains)
    {
        if (remainingChains <= 0) return;

        // 搜索范围内所有未链过的敌人
        Collider[] hits = Physics.OverlapSphere(origin, ChainRange);
        EnemyHealth bestTarget = null;
        float bestDist = float.MaxValue;

        foreach (var h in hits)
        {
            EnemyHealth eh = h.GetComponent<EnemyHealth>();
            if (eh == null || chained.Contains(eh)) continue;
            float d = Vector3.Distance(origin, h.transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                bestTarget = eh;
            }
        }

        if (bestTarget == null) return;

        // 概率触发：链上的敌人 50% 继续链
        if (remainingChains < MaxChains && Random.value > ContinueTriggerChance) return;

        float chainDamage = sourceBullet.Damage * ChainDamageMultiplier;
        bestTarget.TakeDamage(chainDamage);
        chained.Add(bestTarget);

        DamageNumberManager.Spawn(bestTarget.transform, bestTarget.transform.position, chainDamage, Color.cyan);

        int chainIndex = MaxChains - remainingChains + 1;
        Debug.Log($"[EMP] 闪电链 #{chainIndex}: {bestTarget.name} 受到 {chainDamage:F0} 伤害 (距离={bestDist:F1}m)");

        // 递归链下一个
        ChainFrom(bestTarget.transform.position, sourceBullet, chained, remainingChains - 1);
    }
}
