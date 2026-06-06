using UnityEngine;

/// <summary>
/// 近战连击链中的一段。数组中索引 0 = 第 1 刀, 1 = 第 2 刀...
/// </summary>
[System.Serializable]
public class ComboStage
{
    [Header("伤害")]
    public float damage = 20f;

    [Header("检测范围")]
    [Tooltip("攻击距离（米）")]
    public float range = 3f;
    [Tooltip("扇形角度（度），180 = 前方半圆")]
    public float fanAngle = 90f;

    [Header("效果")]
    [Tooltip("击退力度")]
    public float knockback = 5f;

    [Header("时间")]
    [Tooltip("检测窗口持续时长（秒）")]
    public float hitboxActiveTime = 0.2f;

    [Header("倍率（可选）")]
    public float damageMultiplier = 1f;
    public float rangeMultiplier = 1f;
}

[CreateAssetMenu(menuName = "DayByDay/MeleeData")]
public class MeleeData : WeaponData
{
    [Header("连击链")]
    [Tooltip("挥砍序列：索引 0=第1刀, 1=第2刀, 2=第3刀...")]
    public ComboStage[] comboChain = new ComboStage[]
    {
        new ComboStage { damage = 15f, range = 3f, fanAngle = 90f, knockback = 3f, hitboxActiveTime = 0.2f },
        new ComboStage { damage = 20f, range = 3f, fanAngle = 120f, knockback = 5f, hitboxActiveTime = 0.25f },
        new ComboStage { damage = 35f, range = 3.6f, fanAngle = 180f, knockback = 8f, hitboxActiveTime = 0.3f, damageMultiplier = 1.5f, rangeMultiplier = 1.2f }
    };

    [Header("连击节奏")]
    [Tooltip("两刀之间最长时间间隔（秒），超时重置到第 1 段")]
    public float comboCooldown = 1.0f;

    [Header("攻击速度")]
    [Tooltip("两次挥砍最小间隔（秒），控制攻速上限")]
    public float attackCooldown = 0.4f;
}
