using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections;
using System.Collections.Generic;

public class PlayerShooting : MonoBehaviour, IResettable
{
    [Header("双武器槽位")]
    public WeaponSlot[] slots = new WeaponSlot[2];
    public int activeSlotIndex = 0;

    [Header("换弹状态（只有 activeSlot 能换弹）")]
    public bool isReloading { get; private set; } = false;

    [Header("射击物理枪口")]
    public Transform firePoint;

    private float nextFireTime = 0f;
    private bool interruptReload = false;
    private static HashSet<string> unlockedWeapons = new HashSet<string>();

    // 事件广播
    public static event Action OnAmmoChanged;
    public static event Action<float> OnReloading;
    public static event Action OnReloadComplete;
    public static event Action OnEmptyClipFired;

    // ─── 属性简写：全部委托到 activeSlot ───
    private WeaponSlot activeSlot => (slots != null && slots.Length > activeSlotIndex) ? slots[activeSlotIndex] : null;
    public WeaponData currentWeaponData => activeSlot?.weaponData;
    public bool hasWeapon
    {
        get
        {
            if (slots == null) return false;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null && !slots[i].IsEmpty) return true;
            }
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
    public int maxMagazineSize => currentWeaponData != null ? currentWeaponData.maxMagazineSize : 7;
    public float reloadTime => currentWeaponData != null
        ? (currentWeaponData.isMagazineReload ? currentWeaponData.reloadTime : currentWeaponData.perShellReloadTime)
        : 1.5f;

    /// <summary>
    /// 查找武器名称匹配的槽位索引，没找到返回 -1
    /// </summary>
    public int FindSlotByWeaponName(string name)
    {
        if (slots == null || string.IsNullOrEmpty(name)) return -1;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null && slots[i].weaponData != null && slots[i].weaponData.weaponName == name)
                return i;
        }
        return -1;
    }

    /// <summary>
    /// 找到第一个空槽的索引，没有空槽返回 -1
    /// </summary>
    public int FindEmptySlot()
    {
        if (slots == null) return -1;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null || slots[i].IsEmpty) return i;
        }
        return -1;
    }

    /// <summary>
    /// 公开方法：外部可直接触发弹药 UI 刷新（替代 AmmoSupplyBox 的反射 hack）
    /// </summary>
    public static void InvokeAmmoChanged() => OnAmmoChanged?.Invoke();

    void Awake()
    {
        // 确保两个槽位都被初始化（数组声明只分配了 null 引用）
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null) slots[i] = new WeaponSlot();
        }
    }

    void Start()
    {
        InitializeWeapon();
    }

    void Update()
    {
        HandlePlayerInput();
    }

    private void InitializeWeapon()
    {
        // slots[0] 可能已经有武器（Inspector 中预设），按 WeaponData 初始化
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
    /// 切换到指定槽位的武器。空槽不能切、已在当前槽不操作。
    /// 切枪会打断正在进行的换弹。
    /// </summary>
    private void SwapToSlot(int index)
    {
        if (index == activeSlotIndex) return;
        if (slots == null || index < 0 || index >= slots.Length) return;
        if (slots[index] == null || slots[index].IsEmpty) return;

        // 打断换弹
        if (isReloading)
        {
            StopAllCoroutines();
            isReloading = false;
            OnReloadComplete?.Invoke();
        }

        activeSlotIndex = index;
        OnAmmoChanged?.Invoke();
    }

    private void HandlePlayerInput()
    {
        // ── 切枪输入（同一帧内阻止射击/换弹）──
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            SwapToSlot(0);
            return;
        }
        if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            SwapToSlot(1);
            return;
        }

        if (!hasWeapon || currentWeaponData == null) return;

        // 逐发装填中：允许射击键打断（装完当前发就停），其余输入屏蔽
        if (isReloading)
        {
            if (!currentWeaponData.isMagazineReload && Mouse.current.leftButton.wasPressedThisFrame)
            {
                interruptReload = true;
            }
            return;
        }

        // 处理射击输入
        if (currentWeaponData.isAutomatic)
        {
            if (Mouse.current.leftButton.isPressed && Time.time >= nextFireTime)
            {
                ExecuteShootLogic();
                nextFireTime = Time.time + currentWeaponData.fireRate;
            }
        }
        else
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                ExecuteShootLogic();
            }
        }

        // 处理换弹输入
        if (Keyboard.current.rKey.wasPressedThisFrame
            && activeSlot.currentAmmo < maxMagazineSize
            && activeSlot.reserveAmmo > 0)
        {
            StartCoroutine(ReloadRoutine());
        }
    }

    private void ExecuteShootLogic()
    {
        if (currentAmmo > 0)
        {
            currentAmmo--;
            OnAmmoChanged?.Invoke();

            if (currentWeaponData.bulletPrefab != null && firePoint != null)
            {
                int pellets = currentWeaponData.bulletsPerShot;
                float spread = currentWeaponData.spreadAngle;

                for (int i = 0; i < pellets; i++)
                {
                    Quaternion pelletRotation = firePoint.rotation;

                    // 霰弹枪模式：为每一发弹丸叠加随机散布
                    if (spread > 0f)
                    {
                        float randomYaw   = UnityEngine.Random.Range(-spread, spread);
                        float randomPitch = UnityEngine.Random.Range(-spread, spread);
                        pelletRotation = firePoint.rotation * Quaternion.Euler(randomPitch, randomYaw, 0f);
                    }

                    GameObject bulletObj = Instantiate(currentWeaponData.bulletPrefab, firePoint.position, pelletRotation);
                    Bullet bulletScript = bulletObj.GetComponent<Bullet>();
                    if (bulletScript != null)
                    {
                        bulletScript.Initialize(currentWeaponData.damage);
                    }
                }
            }
        }
        else
        {
            OnEmptyClipFired?.Invoke();
        }
    }

    private IEnumerator ReloadRoutine()
    {
        isReloading = true;
        interruptReload = false;

        if (currentWeaponData.isMagazineReload)
        {
            // ========== 弹夹式换弹（原逻辑）==========
            float elapsed = 0f;
            while (elapsed < reloadTime)
            {
                elapsed += Time.deltaTime;
                OnReloading?.Invoke(elapsed);
                yield return null;
            }

            reserveAmmo += currentAmmo;
            currentAmmo = 0;
            int ammoNeeded = maxMagazineSize;

            if (reserveAmmo >= ammoNeeded)
            {
                currentAmmo = ammoNeeded;
                reserveAmmo -= ammoNeeded;
            }
            else
            {
                currentAmmo = reserveAmmo;
                reserveAmmo = 0;
            }
        }
        else
        {
            // ========== 逐发装填（霰弹枪）==========
            while (currentAmmo < maxMagazineSize && reserveAmmo > 0 && !interruptReload)
            {
                float shellElapsed = 0f;
                float shellTime = currentWeaponData.perShellReloadTime;

                while (shellElapsed < shellTime && !interruptReload)
                {
                    shellElapsed += Time.deltaTime;
                    OnReloading?.Invoke(shellElapsed);
                    yield return null;
                }

                // 无论被打断还是正常完成，当前这一发都装进去
                currentAmmo++;
                reserveAmmo--;
                OnAmmoChanged?.Invoke();
            }
        }

        isReloading = false;
        OnReloadComplete?.Invoke();
        OnAmmoChanged?.Invoke();
    }

    /// <summary>
    /// 核心交互：接收地面武器的交互指令（支持靠近自动触发 + 打断换弹）
    /// 4 分支：双手空 / 同款补满 / 有空槽 / 双手满替换
    /// </summary>
    public void ExecuteInteraction(GunPickup groundGun, GameObject groundGameObject)
    {
        // ── 分支 1：双手都空 → 新枪放入槽 0，切到槽 0 ──
        if (!hasWeapon)
        {
            slots[0].LoadFromConfig(groundGun.weaponConfig, true);
            if (!unlockedWeapons.Contains(groundGun.weaponConfig.weaponName))
                unlockedWeapons.Add(groundGun.weaponConfig.weaponName);
            // 如果有地面记录的弹药则使用地面数据
            slots[0].currentAmmo = groundGun.runtimeAmmo > 0 ? groundGun.runtimeAmmo : slots[0].MaxMagazine;
            slots[0].reserveAmmo = groundGun.runtimeReserve > 0 ? groundGun.runtimeReserve : groundGun.weaponConfig.defaultReserveAmmo;
            activeSlotIndex = 0;
            OnAmmoChanged?.Invoke();
            Destroy(groundGameObject);
            return;
        }

        // ── 分支 2：有同款武器 → 自动补满弹药，不占新槽 ──
        int matchingSlot = FindSlotByWeaponName(groundGun.weaponConfig.weaponName);
        if (matchingSlot >= 0)
        {
            // 如果同款武器正在换弹且恰是 activeSlot → 打断
            if (isReloading && matchingSlot == activeSlotIndex)
            {
                StopAllCoroutines();
                isReloading = false;
                OnReloadComplete?.Invoke();
            }

            WeaponData cfg = groundGun.weaponConfig;
            slots[matchingSlot].currentAmmo = cfg.maxMagazineSize;
            slots[matchingSlot].reserveAmmo = cfg.defaultReserveAmmo;

            // 切到同款武器槽
            if (activeSlotIndex != matchingSlot)
            {
                activeSlotIndex = matchingSlot;
            }

            OnAmmoChanged?.Invoke();
            Destroy(groundGameObject);
            return;
        }

        // ── 分支 3：有空槽 → 新枪放入空槽，切到该槽 ──
        int emptySlot = FindEmptySlot();
        if (emptySlot >= 0)
        {
            slots[emptySlot].LoadFromConfig(groundGun.weaponConfig, true);
            if (!unlockedWeapons.Contains(groundGun.weaponConfig.weaponName))
                unlockedWeapons.Add(groundGun.weaponConfig.weaponName);
            slots[emptySlot].currentAmmo = groundGun.runtimeAmmo > 0 ? groundGun.runtimeAmmo : slots[emptySlot].MaxMagazine;
            slots[emptySlot].reserveAmmo = groundGun.runtimeReserve > 0 ? groundGun.runtimeReserve : groundGun.weaponConfig.defaultReserveAmmo;
            activeSlotIndex = emptySlot;
            OnAmmoChanged?.Invoke();
            Destroy(groundGameObject);
            return;
        }

        // ── 分支 4：双手都满 → 替换当前 activeSlot，旧枪丢地上 ──
        if (isReloading) return;

        // 备份旧枪数据
        WeaponData playerOldConfig = slots[activeSlotIndex].weaponData;
        int playerOldAmmo = slots[activeSlotIndex].currentAmmo;
        int playerOldReserve = slots[activeSlotIndex].reserveAmmo;

        // 灌入新枪
        slots[activeSlotIndex].LoadFromConfig(groundGun.weaponConfig, true);
        if (!unlockedWeapons.Contains(groundGun.weaponConfig.weaponName))
            unlockedWeapons.Add(groundGun.weaponConfig.weaponName);
        slots[activeSlotIndex].currentAmmo = groundGun.runtimeAmmo > 0 ? groundGun.runtimeAmmo : slots[activeSlotIndex].MaxMagazine;
        slots[activeSlotIndex].reserveAmmo = groundGun.runtimeReserve > 0 ? groundGun.runtimeReserve : groundGun.weaponConfig.defaultReserveAmmo;

        // 旧枪数据写到地面物体
        groundGun.Initialize(playerOldConfig, playerOldAmmo, playerOldReserve);
        groundGameObject.transform.position = transform.position + transform.forward * 1.8f + Vector3.up * 0.2f;
        groundGun.RefreshPickupState();

        // 重置地面物体的限时销毁（复用已有组件，避免竞态）
        TimedDestroyer destroyer = groundGameObject.GetComponent<TimedDestroyer>();
        if (destroyer != null)
            destroyer.ResetTimer();
        else
            groundGameObject.AddComponent<TimedDestroyer>();

        // 喂回雷达
        PlayerInteraction radar = GetComponent<PlayerInteraction>();
        if (radar != null)
        {
            IInteractable newGroundInteractable = groundGameObject.GetComponent<IInteractable>();
            radar.RegisterInteractable(newGroundInteractable);
        }

        OnAmmoChanged?.Invoke();
    }
    /// <summary>
    /// 游戏重置契约：复活或重新开局时触发全局重置
    /// </summary>
    public void ResetData()
    {
        OnReloadComplete?.Invoke();
        StopAllCoroutines();
        isReloading = false;

        // 1. 让雷达脚本清空当前的缓存数据
        PlayerInteraction radar = GetComponent<PlayerInteraction>();
        if (radar != null) radar.ClearRadar();

        // 2. 将场景中所有现存的地面枪械全部初始化回默认满状态
        GunPickup[] allSceneGuns = FindObjectsByType<GunPickup>();
        foreach (GunPickup gun in allSceneGuns)
        {
            if (gun != null && gun.weaponConfig != null)
            {
                gun.Initialize(gun.weaponConfig, gun.weaponConfig.maxMagazineSize, gun.weaponConfig.defaultReserveAmmo);
                gun.RefreshPickupState();
            }
        }

        // 3. 遍历两个槽位，重置弹药到满状态
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null && slots[i].weaponData != null)
            {
                slots[i].currentAmmo = slots[i].MaxMagazine;
                slots[i].reserveAmmo = slots[i].weaponData.defaultReserveAmmo;
            }
        }

        OnAmmoChanged?.Invoke();
    }
}