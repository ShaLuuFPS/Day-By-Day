using UnityEngine;

/// <summary>
/// 尸体爆炸 —— 仅玩家直接击杀的敌人触发爆炸，爆炸波及的敌人只死亡不连环爆
/// 特效预制体由 UpgradeManager.corpseExplosionPrefab 指定，可在 Inspector 自由替换颜色/模型
/// </summary>
public class CorpseExplosionEffect : IUpgradeEffect
{
    private const float ExplosionRadius = 3f;
    private const float ExplosionDamage = 20f;

    private readonly GameObject explosionPrefab;

    /// <summary>防递归：正在处理爆炸时忽略新死亡事件，防止连环爆炸</summary>
    private bool isExploding = false;

    public CorpseExplosionEffect(GameObject prefab)
    {
        explosionPrefab = prefab;
    }

    public void OnApply()
    {
        UpgradeManager.CorpseExplosionEnabled = true;
    }

    public void OnEnemyKilled(EnemyHealth enemy)
    {
        if (isExploding) return;
        isExploding = true;
        DoExplosion(enemy.transform.position, enemy);
        isExploding = false;
    }

    void DoExplosion(Vector3 pos, EnemyHealth source)
    {
        SpawnExplosionVFX(pos);

        Collider[] hits = Physics.OverlapSphere(pos, ExplosionRadius);
        foreach (var h in hits)
        {
            EnemyHealth eh = h.GetComponent<EnemyHealth>();
            if (eh != null && eh != source)
                eh.TakeDamage(ExplosionDamage);
        }

        Debug.Log($"[尸体爆炸] 中心={pos} 半径={ExplosionRadius} 伤害={ExplosionDamage}");
    }

    void SpawnExplosionVFX(Vector3 pos)
    {
        if (explosionPrefab == null)
        {
            Debug.LogWarning("[尸体爆炸] 未设置爆炸预制体，跳过特效");
            return;
        }

        GameObject vfx = Object.Instantiate(explosionPrefab, pos, Quaternion.identity);
        // 按爆炸半径缩放预制体（假设预制体基础尺寸为直径 1m 即半径 0.5m）
        float prefabRadius = 0.5f;
        vfx.transform.localScale = Vector3.one * (ExplosionRadius / prefabRadius);
        Object.Destroy(vfx, 0.5f);
    }

    public void OnBulletHit(Bullet bullet, EnemyHealth enemy) { }
}
