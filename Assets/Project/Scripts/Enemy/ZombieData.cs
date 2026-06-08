using UnityEngine;

[CreateAssetMenu(fileName = "NewZombieData", menuName = "FPS/Zombie Data")]
public class ZombieData : ScriptableObject
{
    [Header("僵尸身份")]
    public string zombieName = "普通僵尸";

    [Header("移动属性")]
    public float moveSpeed = 3.5f;
    public float stoppingDistance = 2.0f;

    [Header("冲刺加速")]
    [Tooltip("进入冲刺范围后的移动速度（0 = 不启用冲刺）")]
    public float chargeSpeed = 0f;
    [Tooltip("冲刺触发距离（玩家进入此范围后加速）")]
    public float chargeRange = 0f;

    [Header("攻击属性")]
    public float attackRange = 2.5f;
    public float attackDamage = 10f;
    public float attackRate = 1.0f;

    [Header("生命值")]
    public float maxHealth = 30f;

    [Header("视觉外显")]
    public Color baseColor = Color.green;
    public Color hurtColor = Color.red;
    public float hurtFlashDuration = 0.1f;

    [Header("闪烁特效")]
    [Tooltip("是否启用闪烁")]
    public bool useFlickerEffect = false;
    [Tooltip("闪烁时的颜色")]
    public Color flickerColor = Color.white;
    [Tooltip("单次颜色持续时间（秒），越小闪得越快")]
    public float flickerInterval = 0.15f;
    [Tooltip("预警半径：玩家进入此范围才开始闪烁（0 = 始终闪烁）")]
    public float warningRadius = 0f;

    [Header("自爆模式")]
    [Tooltip("开启后到达攻击范围直接爆炸，不做近战攻击")]
    public bool suicideBomber = false;

    [Header("死亡爆炸")]
    [Tooltip("死亡时是否产生范围爆炸")]
    public bool explodeOnDeath = false;
    [Tooltip("爆炸伤害半径（米），决定伤害判定范围")]
    public float explosionRadius = 5f;
    [Tooltip("爆炸伤害（范围内固定伤害，无距离衰减）")]
    public float explosionDamage = 50f;
    [Tooltip("爆炸视觉特效预制体（带半透明材质），为空则用代码球体")]
    public GameObject explosionEffectPrefab;

    [Header("攻击预警")]
    [Tooltip("攻击前是否在地面显示范围指示器")]
    public bool showAttackWarning = false;
    [Tooltip("预警显示多久后攻击命中（秒）")]
    public float attackWarningDuration = 0.6f;
    [Tooltip("预警视觉预制体（留空则代码生成 Cylinder 圆盘兜底）")]
    public GameObject attackWarningPrefab;

    [Header("掉落物")]
    public GameObject dropPrefab;
    [Range(0f, 1f)]
    public float dropProbability = 0.2f;

    [Header("经验值")]
    [Tooltip("击杀后掉落经验值")]
    public float xpReward = 10f;

    [Header("边界")]
    [Tooltip("Y 低于此值自动视为坠落死亡（触发击杀/经验/波次追踪）")]
    public float killBelowY = -50f;
}
