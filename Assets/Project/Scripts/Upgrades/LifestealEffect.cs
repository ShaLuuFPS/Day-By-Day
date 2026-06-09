using UnityEngine;

/// <summary>
/// 吸血 —— 造成伤害的 3% 回复自身血量
/// </summary>
public class LifestealEffect : IUpgradeEffect
{
    private const float LifestealPercent = 0.03f;
    private PlayerHealth playerHealth;

    public void OnApply()
    {
        UpgradeManager.OnPlayerDamageDealt += OnPlayerDamageDealt;
    }

    void OnPlayerDamageDealt(float damage)
    {
        if (playerHealth == null)
        {
            playerHealth = Object.FindAnyObjectByType<PlayerHealth>();
            if (playerHealth == null) return;
        }

        float healAmount = damage * LifestealPercent;
        playerHealth.Heal(healAmount);
    }

    public void OnEnemyKilled(EnemyHealth enemy) { }
    public void OnBulletHit(Bullet bullet, EnemyHealth enemy) { }
}
