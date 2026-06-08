/// <summary>
/// 减速弹 —— 子弹命中敌人时附加减速效果
/// </summary>
public class SlowBulletEffect : IUpgradeEffect
{
    private const float SlowMultiplier = 0.5f;
    private const float SlowDuration = 0.5f;

    public void OnApply()
    {
        UpgradeManager.SlowEnabled = true;
    }

    public void OnEnemyKilled(EnemyHealth enemy) { }

    public void OnBulletHit(Bullet bullet, EnemyHealth enemy)
    {
        SlowEffect slow = enemy.GetComponent<SlowEffect>();
        if (slow == null)
        {
            slow = enemy.gameObject.AddComponent<SlowEffect>();
            slow.Initialize(SlowMultiplier, SlowDuration);
        }
        else
        {
            // 刷新持续时间（不叠加减速倍数）
            slow.RefreshDuration(SlowDuration);
        }
    }
}
