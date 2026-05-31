using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections;
using System.Collections.Generic;

public class PlayerShooting : MonoBehaviour, IResettable
{
    [Header("核心武器配置")]
    public WeaponData currentWeaponData;

    [Header("枪械实时状态")]
    public bool hasWeapon = false;
    public int currentAmmo;
    public int reserveAmmo;
    public bool isReloading { get; private set; } = false;

    [Header("射击物理枪口")]
    public Transform firePoint;

    private float nextFireTime = 0f;
    private static HashSet<string> unlockedWeapons = new HashSet<string>();

    // 事件广播
    public static event Action OnAmmoChanged;
    public static event Action<float> OnReloading;
    public static event Action OnReloadComplete;
    public static event Action OnEmptyClipFired;

    // 属性简写
    public string weaponName => currentWeaponData != null ? currentWeaponData.weaponName : "未知武器";
    public int maxMagazineSize => currentWeaponData != null ? currentWeaponData.maxMagazineSize : 7;
    public float reloadTime => currentWeaponData != null ? currentWeaponData.reloadTime : 1.5f;

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
        if (currentWeaponData != null)
        {
            currentAmmo = maxMagazineSize;
            if (!unlockedWeapons.Contains(weaponName))
            {
                unlockedWeapons.Add(weaponName);
                reserveAmmo = currentWeaponData.defaultReserveAmmo;
            }
        }
        OnAmmoChanged?.Invoke();
    }

    private void HandlePlayerInput()
    {
        if (!hasWeapon || currentWeaponData == null || isReloading) return;

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
        if (Keyboard.current.rKey.wasPressedThisFrame && currentAmmo < maxMagazineSize && reserveAmmo > 0)
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
                GameObject bulletObj = Instantiate(currentWeaponData.bulletPrefab, firePoint.position, firePoint.rotation);
                Bullet bulletScript = bulletObj.GetComponent<Bullet>();
                if (bulletScript != null)
                {
                    bulletScript.Initialize(currentWeaponData.damage);
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

        isReloading = false;
        OnReloadComplete?.Invoke();
        OnAmmoChanged?.Invoke();
    }

    /// <summary>
    /// 核心交互：接收地面武器的交互指令（支持靠近自动触发 + 打断换弹）
    /// </summary>
    public void ExecuteInteraction(GunPickup groundGun, GameObject groundGameObject)
    {
        // 🧱 情况 1：如果玩家当前处于空手状态，直接拿起来
        if (!hasWeapon)
        {
            currentWeaponData = groundGun.weaponConfig;
            currentAmmo = groundGun.runtimeAmmo;
            reserveAmmo = groundGun.runtimeReserve;
            hasWeapon = true;

            if (!unlockedWeapons.Contains(weaponName)) unlockedWeapons.Add(weaponName);

            OnAmmoChanged?.Invoke();
            Destroy(groundGameObject);
            return;
        }

        // 🧱 情况 2：如果是【同款武器】（靠近自动触发流）
        if (groundGun.weaponConfig.weaponName == this.weaponName)
        {
            // 🌟 绝妙细节：如果玩家此时正在换弹，直接暴力打断换弹过程！
            if (isReloading)
            {
                StopAllCoroutines(); // 强行杀死正在数秒的换弹协程
                isReloading = false; // 状态重置为正常射击状态
                OnReloadComplete?.Invoke(); // 触发事件让 UI Manager 把换弹进度条隐藏掉
            }

            // 瞬间将当前弹夹与备弹全部补满到最大上限
            currentAmmo = maxMagazineSize;
            reserveAmmo = groundGun.weaponConfig.defaultReserveAmmo;

            // 刷新子弹 UI 并销毁地面物体
            OnAmmoChanged?.Invoke();
            Destroy(groundGameObject);
            return;
        }

        // 🧱 情况 3：如果是【不同款武器】，依然维持原样，走严格的按 E 灵魂互换流程
        if (isReloading) return;

        // 备份旧枪
        WeaponData playerOldConfig = this.currentWeaponData;
        int playerOldAmmo = this.currentAmmo;
        int playerOldReserve = this.reserveAmmo;

        // 灌入新枪
        this.currentWeaponData = groundGun.weaponConfig;
        this.currentAmmo = groundGun.runtimeAmmo;
        this.reserveAmmo = groundGun.runtimeReserve;

        if (!unlockedWeapons.Contains(weaponName)) unlockedWeapons.Add(weaponName);

        //数据互换：旧枪留给地面的方块
        groundGun.Initialize(playerOldConfig, playerOldAmmo, playerOldReserve);
        groundGameObject.transform.position = transform.position + transform.forward * 1.8f + Vector3.up * 0.2f;
        groundGun.RefreshPickupState();

        // 🌟 【新增细节】：如果是抛出来的旧枪，强行重置它的限时销毁组件，让它重新活 10 秒！
        TimedDestroyer oldDestroyer = groundGameObject.GetComponent<TimedDestroyer>();
        if (oldDestroyer != null) Destroy(oldDestroyer); // 拔掉快要过期的旧组件
        groundGameObject.AddComponent<TimedDestroyer>(); // 啪！粘上一个全新的，重新开始算10秒倒计时！

        // 喂回雷达组件
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

        // 2. 核心修复：将场景中所有现存的地面枪械（GunPickup）全部初始化回默认满状态
        GunPickup[] allSceneGuns = FindObjectsByType<GunPickup>(FindObjectsSortMode.None);
        foreach (GunPickup gun in allSceneGuns)
        {
            if (gun != null && gun.weaponConfig != null)
            {
                // 将它们的实时残弹重置为最大弹夹，备弹重置为配置文件的预设值
                gun.Initialize(gun.weaponConfig, gun.weaponConfig.maxMagazineSize, gun.weaponConfig.defaultReserveAmmo);
                gun.RefreshPickupState();
            }
        }

        // 3. 将玩家自身的武器数据归位
        if (currentWeaponData != null)
        {
            currentAmmo = maxMagazineSize;
            reserveAmmo = currentWeaponData.defaultReserveAmmo;
        }

        OnAmmoChanged?.Invoke();
    }
}