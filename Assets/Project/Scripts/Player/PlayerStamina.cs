using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System;
using System.Collections;

public class PlayerStamina : MonoBehaviour, IResettable
{
    [Header("配置")]
    public PlayerStaminaConfig config;

    [Header("UI 连线")]
    public Image staminaBarFill;

    [Header("依赖")]
    public PlayerMovement playerMovement;

    // ── 运行时状态 ──
    private float currentStamina;
    private bool isOnCooldown = false;
    private Coroutine dashRoutine;
    private float lastDashTime = -999f;
    private float recoveryTimer = 0f;

    /// <summary>Dash 进行中：PlayerMovement 应在此期间跳过移动</summary>
    public bool isDashing { get; private set; } = false;

    /// <summary>当前体力（0~max）</summary>
    public float CurrentStamina => currentStamina;

    /// <summary>满体力值</summary>
    public float MaxStamina => config != null ? config.maxStamina : 100f;

    /// <summary>体力比例（0~1），UI 可直接用</summary>
    public float StaminaRatio => config != null ? currentStamina / config.maxStamina : 0f;

    public static event Action OnStaminaChanged;

    void Start()
    {
        if (config != null)
            currentStamina = config.maxStamina;
        else
            currentStamina = 100f;

        UpdateStaminaBar();
    }

    void Update()
    {
        if (config == null) return;

        // ── Shift 触发 Dash ──
        if (Keyboard.current != null && Keyboard.current.shiftKey.wasPressedThisFrame)
        {
            TryDash();
        }

        // ── 体力回复 ──
        HandleRecovery();
    }

    void TryDash()
    {
        // 检查：不在冷却、不在 Dash 中、有足够体力
        if (isOnCooldown) return;
        if (isDashing) return;
        if (currentStamina < config.dashCost) return;

        // 消费体力
        currentStamina -= config.dashCost;
        UpdateStaminaBar();
        OnStaminaChanged?.Invoke();

        // 计算 Dash 方向
        Vector3 dashDir = GetDashDirection();

        // 启动 Dash
        dashRoutine = StartCoroutine(DashRoutine(dashDir));

        // 进入冷却
        isOnCooldown = true;
        lastDashTime = Time.time;
        StartCoroutine(CooldownRoutine());

        // 重置回复计时器
        recoveryTimer = 0f;
    }

    Vector3 GetDashDirection()
    {
        // 优先 WASD 输入方向，无输入时用角色朝向
        Vector3 moveDir = Vector3.zero;

        if (playerMovement != null)
        {
            moveDir = playerMovement.moveDirection;
        }
        else
        {
            // fallback：自己读 WASD
            if (Keyboard.current == null) return transform.forward;

            float x = 0f, z = 0f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) z = 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) z = -1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) x = -1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x = 1f;
            moveDir = new Vector3(x, 0f, z).normalized;
        }

        if (moveDir.magnitude < 0.1f)
            return transform.forward;

        return moveDir.normalized;
    }

    IEnumerator DashRoutine(Vector3 direction)
    {
        isDashing = true;
        float elapsed = 0f;
        float duration = config.dashDuration;

        while (elapsed < duration)
        {
            transform.Translate(direction * config.dashSpeed * Time.deltaTime, Space.World);
            elapsed += Time.deltaTime;
            yield return null;
        }

        isDashing = false;
    }

    IEnumerator CooldownRoutine()
    {
        yield return new WaitForSeconds(config.dashCooldown);
        isOnCooldown = false;
    }

    void HandleRecovery()
    {
        if (isDashing) return;
        if (currentStamina >= config.maxStamina) return;

        // 距离最后一次 Dash 或消耗后，需要等待 recoveryDelay 才回复
        recoveryTimer += Time.deltaTime;
        if (recoveryTimer < config.recoveryDelay) return;

        currentStamina += config.recoveryRate * Time.deltaTime;
        if (currentStamina > config.maxStamina)
            currentStamina = config.maxStamina;

        UpdateStaminaBar();
    }

    /// <summary>
    /// 外部回复体力（升级系统接口）
    /// </summary>
    public void Restore(float amount)
    {
        if (config == null) return;
        currentStamina = Mathf.Min(currentStamina + amount, config.maxStamina);
        UpdateStaminaBar();
        OnStaminaChanged?.Invoke();
    }

    void UpdateStaminaBar()
    {
        if (staminaBarFill != null)
            staminaBarFill.fillAmount = StaminaRatio;
    }

    // ── IResettable ──
    public void ResetData()
    {
        StopAllCoroutines();
        isDashing = false;
        isOnCooldown = false;
        recoveryTimer = 0f;

        if (config != null)
            currentStamina = config.maxStamina;
        else
            currentStamina = 100f;

        UpdateStaminaBar();
        OnStaminaChanged?.Invoke();
    }
}
