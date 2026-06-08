using UnityEngine;

/// <summary>
/// 击杀回耐 —— 击杀敌人时恢复 20% 最大体力
/// </summary>
public class KillRestoreStaminaEffect : IUpgradeEffect
{
    private const float RestorePercent = 0.2f;
    private PlayerStamina playerStamina;

    public KillRestoreStaminaEffect(PlayerStamina stamina)
    {
        playerStamina = stamina;
    }

    public void OnApply()
    {
        // 此升级无被动开关——直接通过 OnEnemyKilled 触发
    }

    public void OnEnemyKilled(EnemyHealth enemy)
    {
        if (playerStamina == null)
        {
            playerStamina = Object.FindAnyObjectByType<PlayerStamina>();
            if (playerStamina == null) return;
        }

        float amount = playerStamina.MaxStamina * RestorePercent;
        playerStamina.Restore(amount);

        Debug.Log($"[击杀回耐] +{amount:F0} 体力 ({(RestorePercent * 100):F0}% 最大体力)");
    }

    public void OnBulletHit(Bullet bullet, EnemyHealth enemy) { }
}
