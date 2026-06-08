/// <summary>
/// 升级效果接口 —— 每个升级类型实现此接口，由 UpgradeManager 统一调度
/// </summary>
public interface IUpgradeEffect
{
    /// <summary>升级被选择后立即调用（只调一次）</summary>
    void OnApply();

    /// <summary>任意敌人死亡时调用</summary>
    void OnEnemyKilled(EnemyHealth enemy);

    /// <summary>子弹命中敌人时调用</summary>
    void OnBulletHit(Bullet bullet, EnemyHealth enemy);
}
