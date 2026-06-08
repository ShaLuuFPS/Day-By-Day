using UnityEngine;

/// <summary>
/// 分裂弹 —— 子弹命中敌人时在命中点分裂出 3 颗子弹，角度 ±30°
/// 通过克隆原子弹自身创建子子弹（避免依赖 Resources.Load）
/// </summary>
public class SplitProjectileEffect : IUpgradeEffect
{
    private const int SplitCount = 3;
    private const float SplitAngle = 30f;
    private const float SplitDamageMultiplier = 0.2f;

    public void OnApply()
    {
        UpgradeManager.SplitEnabled = true;
    }

    public void OnEnemyKilled(EnemyHealth enemy) { }

    public void OnBulletHit(Bullet bullet, EnemyHealth enemy)
    {
        // 分裂弹产生的子子弹不再分裂（防止无限递归）
        if (bullet.isSplitBullet) return;

        Vector3 hitPoint = bullet.transform.position;
        float splitDamage = bullet.Damage * SplitDamageMultiplier;

        for (int i = 0; i < SplitCount; i++)
        {
            float angle = Random.Range(-SplitAngle, SplitAngle);
            Quaternion rot = bullet.transform.rotation * Quaternion.Euler(0f, angle, 0f);

            // 克隆原子弹自身（不依赖 Resources）
            GameObject splitBulletObj = Object.Instantiate(bullet.gameObject, hitPoint, rot);

            Bullet sb = splitBulletObj.GetComponent<Bullet>();
            if (sb != null)
            {
                sb.isSplitBullet = true;
                sb.remainingPierce = 0; // 分裂弹不穿透
                sb.Initialize(splitDamage);
            }
        }
    }
}
