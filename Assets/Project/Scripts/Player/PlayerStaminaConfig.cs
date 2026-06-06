using UnityEngine;

[CreateAssetMenu(menuName = "DayByDay/StaminaConfig")]
public class PlayerStaminaConfig : ScriptableObject
{
    [Header("体力上限")]
    public float maxStamina = 100f;

    [Header("Dash 消耗")]
    public float dashCost = 25f;

    [Header("Dash 位移")]
    [Tooltip("Dash 期间的移动速度（单位/秒）")]
    public float dashSpeed = 18f;

    [Tooltip("Dash 位移持续时间（秒）")]
    public float dashDuration = 0.15f;

    [Header("冷却与回复")]
    [Tooltip("两次 Dash 之间的最小间隔（秒）")]
    public float dashCooldown = 1.0f;

    [Tooltip("最后一次 Dash 后多久开始回复体力（秒）")]
    public float recoveryDelay = 1.5f;

    [Tooltip("体力每秒回复量")]
    public float recoveryRate = 30f;
}
