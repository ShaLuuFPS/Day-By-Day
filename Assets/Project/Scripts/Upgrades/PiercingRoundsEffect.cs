/// <summary>
/// 穿透弹 —— 子弹命中敌人后不消失，继续飞行穿透下一个敌人
/// </summary>
public class PiercingRoundsEffect : IUpgradeEffect
{
    public void OnApply()
    {
        UpgradeManager.PierceCount = 1;
    }

    public void OnEnemyKilled(EnemyHealth enemy) { }
    public void OnBulletHit(Bullet bullet, EnemyHealth enemy) { }
}
