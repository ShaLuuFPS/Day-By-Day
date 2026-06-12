using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;

/// <summary>
/// 武器中枢：槽位管理、拾取、弹药状态、切枪。
/// 拆自 PlayerShooting 上帝模块。
/// </summary>
public class WeaponManager : MonoBehaviour, IResettable
{
    [Header("双武器槽位")]
    public WeaponSlot[] slots = new WeaponSlot[2];
    public int activeSlotIndex = 0;

    [Header("子模块引用（自动发现）")]
    [HideInInspector] public GunModule gunModule;
    [HideInInspector] public MeleeModule meleeModule;

    private static HashSet<string> unlockedWeapons = new HashSet<string>();

    // ── 事件 ──
    public event Action OnAmmoChanged;

    // ── 属性 ──
    private WeaponSlot activeSlot =>
        (slots != null && slots.Length > activeSlotIndex) ? slots[activeSlotIndex] : null;

    public WeaponData currentWeaponData => activeSlot?.weaponData;
    private GunData currentGunData => currentWeaponData as GunData;
    private MeleeData currentMeleeData => currentWeaponData as MeleeData;

    public bool hasWeapon
    {
        get
        {
            if (slots == null) return false;
            for (int i = 0; i < slots.Length; i++)
                if (slots[i] != null && !slots[i].IsEmpty) return true;
            return false;
        }
    }

    public int currentAmmo
    {
        get => activeSlot != null ? activeSlot.currentAmmo : 0;
        set { if (activeSlot != null) activeSlot.currentAmmo = value; }
    }

    public int reserveAmmo
    {
        get => activeSlot != null ? activeSlot.reserveAmmo : 0;
        set { if (activeSlot != null) activeSlot.reserveAmmo = value; }
    }

    public string weaponName => currentWeaponData != null ? currentWeaponData.weaponName : "未知武器";

    public int maxMagazineSize
    {
        get
        {
            GunData gd = currentGunData;
            return gd != null ? gd.maxMagazineSize : 0;
        }
    }

    public float reloadTime
    {
        get
        {
            GunData gd = currentGunData;
            return gd != null
                ? (gd.isMagazineReload ? gd.reloadTime : gd.perShellReloadTime)
                : 0f;
        }
    }

    /// <summary>GunModule/MeleeModule 通过此属性判断当前武器类型</summary>
    public bool IsCurrentMelee => currentWeaponData != null && currentWeaponData.weaponType == WeaponType.Melee;
    public bool IsCurrentGun => currentWeaponData != null && currentWeaponData.weaponType != WeaponType.Melee;
    public GunData CurrentGunData => currentGunData;
    public MeleeData CurrentMeleeData => currentMeleeData;

    // ── 公开方法 ──

    public int FindSlotByWeaponName(string name)
    {
        if (slots == null || string.IsNullOrEmpty(name)) return -1;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null && slots[i].weaponData != null
                && slots[i].weaponData.weaponName == name)
                return i;
        }
        return -1;
    }

    public int FindEmptySlot()
    {
        if (slots == null) return -1;
        for (int i = 0; i < slots.Length; i++)
            if (slots[i] == null || slots[i].IsEmpty) return i;
        return -1;
    }

    /// <summary>触发弹药 UI 刷新（外部如 AmmoSupplyBox 调用）</summary>
    public void InvokeAmmoChanged() => OnAmmoChanged?.Invoke();

    // ── Unity 生命周期 ──

    void Awake()
    {
        for (int i = 0; i < slots.Length; i++)
            if (slots[i] == null) slots[i] = new WeaponSlot();

        gunModule = GetComponent<GunModule>();
        meleeModule = GetComponent<MeleeModule>();
    }

    void Start()
    {
        InitializeWeapon();
    }

    void Update()
    {
        if (GameStateManager.IsInputFrozen) return;
        HandleSwapInput();
    }

    // ── 输入处理 ──

    private void HandleSwapInput()
    {
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
            SwapToSlot(0);
        else if (Keyboard.current.digit2Key.wasPressedThisFrame)
            SwapToSlot(1);
    }

    // ── 槽位操作 ──

    private void InitializeWeapon()
    {
        if (slots[0] != null && slots[0].weaponData != null)
        {
            slots[0].LoadFromConfig(slots[0].weaponData,
                !unlockedWeapons.Contains(slots[0].weaponData.weaponName));
            if (!unlockedWeapons.Contains(slots[0].weaponData.weaponName))
                unlockedWeapons.Add(slots[0].weaponData.weaponName);
        }
        OnAmmoChanged?.Invoke();
    }

    /// <summary>
    /// 切到指定槽位。通知 GunModule/MeleeModule 重置各自状态。
    /// </summary>
    public void SwapToSlot(int index)
    {
        if (index == activeSlotIndex) return;
        if (slots == null || index < 0 || index >= slots.Length) return;
        if (slots[index] == null || slots[index].IsEmpty) return;

        // 打断换弹
        if (gunModule != null)
            gunModule.InterruptReload();

        activeSlotIndex = index;

        // 通知子模块重置
        if (gunModule != null)
            gunModule.ResetOnSwap();
        if (meleeModule != null)
            meleeModule.ResetOnSwap();

        OnAmmoChanged?.Invoke();
    }

    // ── 武器拾取 ──

    /// <summary>
    /// 核心交互：接收地面武器的交互指令。
    /// 4 分支：双手空 / 同款补满 / 有空槽 / 双手满替换
    /// </summary>
    public void PickupWeapon(GunPickup groundGun, GameObject groundGameObject)
    {
        bool isMelee = groundGun.weaponConfig.weaponType == WeaponType.Melee;

        // 分支 1：双手都空
        if (!hasWeapon)
        {
            slots[0].LoadFromConfig(groundGun.weaponConfig, true);
            if (!unlockedWeapons.Contains(groundGun.weaponConfig.weaponName))
                unlockedWeapons.Add(groundGun.weaponConfig.weaponName);
            if (!isMelee)
            {
                slots[0].currentAmmo = groundGun.runtimeAmmo > 0 ? groundGun.runtimeAmmo : slots[0].MaxMagazine;
                slots[0].reserveAmmo = groundGun.runtimeReserve > 0 ? groundGun.runtimeReserve
                    : (groundGun.weaponConfig as GunData)?.defaultReserveAmmo ?? 0;
            }
            activeSlotIndex = 0;
            OnAmmoChanged?.Invoke();
            Destroy(groundGameObject);
            return;
        }

        // 分支 2：有同款武器 → 补满弹药
        int matchingSlot = FindSlotByWeaponName(groundGun.weaponConfig.weaponName);
        if (matchingSlot >= 0)
        {
            if (gunModule != null && gunModule.isReloading && matchingSlot == activeSlotIndex)
                gunModule.InterruptReload();

            if (!isMelee)
            {
                GunData gd = groundGun.weaponConfig as GunData;
                if (gd != null)
                {
                    slots[matchingSlot].currentAmmo = gd.maxMagazineSize;
                    slots[matchingSlot].reserveAmmo = gd.defaultReserveAmmo;
                }
            }

            if (activeSlotIndex != matchingSlot)
                activeSlotIndex = matchingSlot;

            OnAmmoChanged?.Invoke();
            Destroy(groundGameObject);
            return;
        }

        // 分支 3：有空槽
        int emptySlot = FindEmptySlot();
        if (emptySlot >= 0)
        {
            slots[emptySlot].LoadFromConfig(groundGun.weaponConfig, true);
            if (!unlockedWeapons.Contains(groundGun.weaponConfig.weaponName))
                unlockedWeapons.Add(groundGun.weaponConfig.weaponName);
            if (!isMelee)
            {
                slots[emptySlot].currentAmmo = groundGun.runtimeAmmo > 0 ? groundGun.runtimeAmmo : slots[emptySlot].MaxMagazine;
                slots[emptySlot].reserveAmmo = groundGun.runtimeReserve > 0 ? groundGun.runtimeReserve
                    : (groundGun.weaponConfig as GunData)?.defaultReserveAmmo ?? 0;
            }
            activeSlotIndex = emptySlot;
            OnAmmoChanged?.Invoke();
            Destroy(groundGameObject);
            return;
        }

        // 分支 4：双手满 → 替换 activeSlot，旧枪丢地上
        if (gunModule != null && gunModule.isReloading) return;

        WeaponData playerOldConfig = slots[activeSlotIndex].weaponData;
        int playerOldAmmo = slots[activeSlotIndex].currentAmmo;
        int playerOldReserve = slots[activeSlotIndex].reserveAmmo;

        slots[activeSlotIndex].LoadFromConfig(groundGun.weaponConfig, true);
        if (!unlockedWeapons.Contains(groundGun.weaponConfig.weaponName))
            unlockedWeapons.Add(groundGun.weaponConfig.weaponName);
        if (!isMelee)
        {
            slots[activeSlotIndex].currentAmmo = groundGun.runtimeAmmo > 0 ? groundGun.runtimeAmmo : slots[activeSlotIndex].MaxMagazine;
            slots[activeSlotIndex].reserveAmmo = groundGun.runtimeReserve > 0 ? groundGun.runtimeReserve
                : (groundGun.weaponConfig as GunData)?.defaultReserveAmmo ?? 0;
        }

        groundGun.Initialize(playerOldConfig, playerOldAmmo, playerOldReserve);
        groundGameObject.transform.position = transform.position + transform.forward * 1.8f + Vector3.up * 0.2f;
        groundGun.RefreshPickupState();

        TimedDestroyer destroyer = groundGameObject.GetComponent<TimedDestroyer>();
        if (destroyer != null)
            destroyer.ResetTimer();
        else
            groundGameObject.AddComponent<TimedDestroyer>();

        PlayerInteraction radar = GetComponent<PlayerInteraction>();
        if (radar != null)
        {
            IInteractable newGroundInteractable = groundGameObject.GetComponent<IInteractable>();
            radar.RegisterInteractable(newGroundInteractable);
        }

        OnAmmoChanged?.Invoke();
    }

    // ── IResettable ──

    public void ResetData()
    {
        // 重置场景中所有地面枪械
        GunPickup[] allSceneGuns = FindObjectsByType<GunPickup>();
        foreach (GunPickup gun in allSceneGuns)
        {
            if (gun != null && gun.weaponConfig != null)
            {
                int ammo = gun.weaponConfig is GunData gd ? gd.maxMagazineSize : 0;
                int reserve = gun.weaponConfig is GunData gd2 ? gd2.defaultReserveAmmo : 0;
                gun.Initialize(gun.weaponConfig, ammo, reserve);
                gun.RefreshPickupState();
            }
        }

        // 重置两个槽位
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null && slots[i].weaponData != null)
                slots[i].LoadFromConfig(slots[i].weaponData, true);
        }

        OnAmmoChanged?.Invoke();
    }
}
