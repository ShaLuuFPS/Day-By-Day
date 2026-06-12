using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 近战模块：连击系统、范围预览、攻击判定。
/// 拆自 PlayerShooting 上帝模块。
/// </summary>
public class MeleeModule : MonoBehaviour, IResettable
{
    [Header("近战判定框")]
    public MeleeHitbox meleeHitbox;

    private WeaponManager weaponManager;

    // 连击状态
    private int meleeComboStage = 0;
    private float meleeComboTimer = 0f;
    private float meleeNextAttackTime = 0f;
    private bool meleeHolding = false;

    /// <summary>近战连击段索引，UI 读取用</summary>
    public int ComboStage => meleeComboStage;

    void Awake()
    {
        weaponManager = GetComponent<WeaponManager>();
    }

    void Update()
    {
        if (GameStateManager.IsInputFrozen)
        {
            // 输入冻结时强制中断持刀状态
            if (meleeHolding)
            {
                meleeHolding = false;
                if (meleeHitbox != null)
                    meleeHitbox.HideRangeIndicator();
            }
            return;
        }

        // 非近战武器不处理
        if (weaponManager == null || !weaponManager.hasWeapon) return;
        if (!weaponManager.IsCurrentMelee) return;

        HandleMeleeInput();

        // 连击过期回退
        MeleeData meleeData = weaponManager.CurrentMeleeData;
        if (meleeData != null && meleeComboStage > 0
            && Time.time - meleeComboTimer > meleeData.comboCooldown)
        {
            meleeComboStage = 0;
            weaponManager.InvokeAmmoChanged();
        }
    }

    /// <summary>切枪时 WeaponManager 调用</summary>
    public void ResetOnSwap()
    {
        meleeComboStage = 0;
        meleeComboTimer = 0f;
        meleeHolding = false;
        if (meleeHitbox != null)
            meleeHitbox.HideRangeIndicator();
    }

    // ── 输入处理 ──

    private void HandleMeleeInput()
    {
        // 按住左键：显示范围预览
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (Time.time < meleeNextAttackTime) return;
            meleeHolding = true;
            ShowMeleeRange();
        }

        // 持续按住：刷新范围朝向
        if (Mouse.current.leftButton.isPressed && meleeHolding)
        {
            if (meleeHitbox != null)
                meleeHitbox.KeepShowingRange();
        }

        // 松开左键：执行攻击
        if (!Mouse.current.leftButton.isPressed && meleeHolding)
        {
            meleeHolding = false;
            if (meleeHitbox != null)
                meleeHitbox.HideRangeIndicator();
            ExecuteMelee();
        }
    }

    private void ShowMeleeRange()
    {
        MeleeData meleeData = weaponManager.CurrentMeleeData;
        if (meleeHitbox == null || meleeData == null) return;
        if (meleeData.comboChain == null || meleeData.comboChain.Length == 0) return;

        int stageIdx = meleeComboStage;
        if (stageIdx >= meleeData.comboChain.Length) stageIdx = 0;

        var stage = meleeData.comboChain[stageIdx];
        meleeHitbox.ShowRangeIndicator(stage.range * stage.rangeMultiplier, stage.fanAngle);
    }

    private void ExecuteMelee()
    {
        MeleeData meleeData = weaponManager.CurrentMeleeData;
        if (meleeData == null) return;
        if (meleeData.comboChain == null || meleeData.comboChain.Length == 0) return;
        if (meleeHitbox == null) return;

        if (Time.time < meleeNextAttackTime) return;

        // 连击窗口超时 → 重置
        if (Time.time - meleeComboTimer > meleeData.comboCooldown)
            meleeComboStage = 0;

        if (meleeComboStage >= meleeData.comboChain.Length)
            meleeComboStage = 0;

        ComboStage stage = meleeData.comboChain[meleeComboStage];
        meleeHitbox.Activate(stage);

        meleeNextAttackTime = Time.time + meleeData.attackCooldown;
        meleeComboStage++;
        meleeComboTimer = Time.time;
        weaponManager.InvokeAmmoChanged();
    }

    // ── IResettable ──

    public void ResetData()
    {
        meleeComboStage = 0;
        meleeComboTimer = 0f;
        meleeHolding = false;
        if (meleeHitbox != null)
            meleeHitbox.HideRangeIndicator();
    }
}
