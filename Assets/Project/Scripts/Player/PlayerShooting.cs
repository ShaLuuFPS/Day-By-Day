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
    /// 核心交互：由于雷达拆分，地面武器交互在此脚本被动执行
    /// </summary>
    public void ExecuteInteraction(GunPickup groundGun, GameObject groundGameObject)
    {
        // 1. 如果空手
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

        // 2. 拾取同款武器
        if (groundGun.weaponConfig.weaponName == this.weaponName)
        {
            currentAmmo = maxMagazineSize;
            int bonusAmmo = groundGun.weaponConfig.maxMagazineSize * 2;
            reserveAmmo += bonusAmmo;
            if (reserveAmmo > groundGun.weaponConfig.defaultReserveAmmo) reserveAmmo = groundGun.weaponConfig.defaultReserveAmmo;
            OnAmmoChanged?.Invoke();
            Destroy(groundGameObject);
        }
        // 3. 换不同款的枪
        else
        {
            if (isReloading) return;

            WeaponData playerOldConfig = this.currentWeaponData;
            int playerOldAmmo = this.currentAmmo;
            int playerOldReserve = this.reserveAmmo;

            this.currentWeaponData = groundGun.weaponConfig;
            this.currentAmmo = groundGun.runtimeAmmo;

            if (!unlockedWeapons.Contains(weaponName))
            {
                unlockedWeapons.Add(weaponName);
                this.reserveAmmo = groundGun.weaponConfig.defaultReserveAmmo;
            }
            else
            {
                this.reserveAmmo = groundGun.runtimeReserve;
            }

            groundGun.Initialize(playerOldConfig, playerOldAmmo, playerOldReserve);
            groundGameObject.transform.position = transform.position + transform.forward * 1.8f + Vector3.up * 0.2f;
            groundGun.RefreshPickupState();

            // 🌟 关键衔接：通过调用玩家身上的雷达组件，把吐出来的旧枪重新喂回交互列表
            PlayerInteraction radar = GetComponent<PlayerInteraction>();
            if (radar != null)
            {
                IInteractable newGroundInteractable = groundGameObject.GetComponent<IInteractable>();
                radar.RegisterInteractable(newGroundInteractable);
            }

            OnAmmoChanged?.Invoke();
        }
    }

    public void ResetData()
    {
        OnReloadComplete?.Invoke();
        StopAllCoroutines();
        isReloading = false;

        // 重置时一并让雷达脚本清空数据
        PlayerInteraction radar = GetComponent<PlayerInteraction>();
        if (radar != null) radar.ClearRadar();

        if (currentWeaponData != null)
        {
            currentAmmo = maxMagazineSize;
            reserveAmmo = currentWeaponData.defaultReserveAmmo;
        }
        OnAmmoChanged?.Invoke();
    }
}