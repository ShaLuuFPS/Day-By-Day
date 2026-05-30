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
    public int reserveAmmo;            // 动态初始化
    public bool isReloading { get; private set; } = false;

    [Header("射击物理枪口")]
    public Transform firePoint;

    private float nextFireTime = 0f;

    // 记录玩家拥有过的武器，用来判断是不是第一次摸到该武器类型
    private static HashSet<string> unlockedWeapons = new HashSet<string>();

    public static event Action OnAmmoChanged;
    public static event Action<float> OnReloading;
    public static event Action OnReloadComplete;
    public static event Action OnEmptyClipFired;

    public string weaponName => currentWeaponData != null ? currentWeaponData.weaponName : "未知武器";
    public int maxMagazineSize => currentWeaponData != null ? currentWeaponData.maxMagazineSize : 7;
    public float reloadTime => currentWeaponData != null ? currentWeaponData.reloadTime : 1.5f;

    void Start()
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

    void Update()
    {
        if (!hasWeapon || currentWeaponData == null || isReloading) return;

        // 射击输入监听
        if (currentWeaponData.isAutomatic)
        {
            if (Mouse.current.leftButton.isPressed && Time.time >= nextFireTime)
            {
                Shoot();
                nextFireTime = Time.time + currentWeaponData.fireRate;
            }
        }
        else
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                Shoot();
            }
        }

        // 换弹键盘监听
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            if (currentAmmo < maxMagazineSize && reserveAmmo > 0)
            {
                StartCoroutine(ReloadRoutine());
            }
        }
    }

    void Shoot()
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

    IEnumerator ReloadRoutine()
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

    // 🌟 核心：检测走入地上的武器碰撞箱
    private void OnTriggerEnter(Collider other)
    {
        // 确保踩到的是挂有新脚本的真实地面武器
        GunPickup groundGun = other.GetComponent<GunPickup>();
        if (groundGun == null || groundGun.weaponConfig == null) return;

        // 情况一：如果玩家当前空手（hasWeapon为false），直接捡起它
        if (!hasWeapon)
        {
            currentWeaponData = groundGun.weaponConfig;
            currentAmmo = groundGun.runtimeAmmo;
            reserveAmmo = groundGun.runtimeReserve;
            hasWeapon = true;

            if (!unlockedWeapons.Contains(weaponName)) unlockedWeapons.Add(weaponName);

            OnAmmoChanged?.Invoke();
            Destroy(other.gameObject); // 销毁地上的方块
            return;
        }

        // 情况二：玩家手里有枪，且踩到了【同款枪】 $\rightarrow$ 触发你的细节设计（自动吸弹）
        if (groundGun.weaponConfig.weaponName == this.weaponName)
        {
            // 1. 瞬间补满当前弹夹
            currentAmmo = maxMagazineSize;

            // 2. 补充两个弹夹的备弹量
            int bonusAmmo = groundGun.weaponConfig.maxMagazineSize * 2;
            reserveAmmo += bonusAmmo;

            // 3. 限制不能溢出配置的“最大默认备弹量”（比如突击步枪最多带90发备弹）
            if (reserveAmmo > groundGun.weaponConfig.defaultReserveAmmo)
            {
                reserveAmmo = groundGun.weaponConfig.defaultReserveAmmo;
            }

            OnAmmoChanged?.Invoke();
            Destroy(other.gameObject); // 吸收完毕，销毁地上的方块
            Debug.Log($"[吸收同款武器] 弹夹已回满，备弹补充了 {bonusAmmo} 发！");
        }
        // 情况三：玩家手里有枪，且踩到了【不同种类的枪】 $\rightarrow$ 优雅换枪（灵魂互换）
        else
        {
            // 如果玩家正在换弹，不允许执行换枪，防止协程卡死
            if (isReloading) return;

            Debug.Log($"[触发武器交换] 正在将手里的 {this.weaponName} 替换为地面的 {groundGun.weaponConfig.weaponName}");

            // 1. 暂存玩家手里这把枪的真实数据包
            WeaponData playerOldConfig = this.currentWeaponData;
            int playerOldAmmo = this.currentAmmo;
            int playerOldReserve = this.reserveAmmo;

            // 2. 将地面枪的数据“吸入”玩家的主角容器里
            this.currentWeaponData = groundGun.weaponConfig;
            this.currentAmmo = groundGun.runtimeAmmo;

            // 绝妙细节：如果玩家是历史上第一次拿到这把新枪，强行赋予它满额的默认备弹；否则拿取地上的备弹
            if (!unlockedWeapons.Contains(weaponName))
            {
                unlockedWeapons.Add(weaponName);
                this.reserveAmmo = groundGun.weaponConfig.defaultReserveAmmo;
            }
            else
            {
                this.reserveAmmo = groundGun.runtimeReserve;
            }

            // 3. 将玩家原本旧枪的数据“灌回”地上的方块里，保留给未来！
            groundGun.Initialize(playerOldConfig, playerOldAmmo, playerOldReserve);

            // 4. 让地上的方块在玩家脚下弹开一点点（防止连续触发换枪），并重置颜色
            other.transform.position = transform.position + transform.forward * 1.5f + Vector3.up * 0.5f;
            groundGun.SendMessage("Start"); // 强行让地上的方块重新根据新枪配置刷一次颜色！

            // 5. 广播界面刷新
            OnAmmoChanged?.Invoke();
        }
    }

    public void ResetData()
    {
        OnReloadComplete?.Invoke();
        StopAllCoroutines();
        isReloading = false;

        if (currentWeaponData != null)
        {
            currentAmmo = maxMagazineSize;
            reserveAmmo = currentWeaponData.defaultReserveAmmo;
        }

        OnAmmoChanged?.Invoke();
    }
}