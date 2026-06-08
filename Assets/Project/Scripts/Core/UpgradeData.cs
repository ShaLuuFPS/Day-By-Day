using UnityEngine;

/// <summary>
/// 升级类型枚举 —— Phase 4 实现具体效果时使用
/// </summary>
public enum UpgradeType
{
    PiercingRounds,      // 穿透弹
    SplitProjectile,     // 分裂弹头
    CorpseExplosion,     // 尸体爆炸
    SlowBullet,          // 减速弹
    EMPChain,            // 电磁弹
    KillRestoreStamina   // 击杀回耐
}

/// <summary>
/// 升级数据 ScriptableObject —— 定义一次升级选项的名称、描述和类型
/// </summary>
[CreateAssetMenu(fileName = "NewUpgrade", menuName = "FPS/Upgrade Data")]
public class UpgradeData : ScriptableObject
{
    [Header("展示")]
    public string upgradeName = "升级名称";
    [TextArea(2, 4)]
    public string description = "升级描述";
    public Sprite icon;

    [Header("效果")]
    public UpgradeType upgradeType;
}
