using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections;
using System.Collections.Generic;// 🌟 引入集合，用来记录捡枪历史

public class PlayerShooting : MonoBehaviour, IResettable
{
    [Header("核心武器配置")]
    public WeaponData currentWeaponData;

    [Header("枪械实时状态")]
    public bool hasWeapon = false;
    public int currentAmmo;
    public int reserveAmmo;            // 🌟 变成动态初始化，不需要在面板写死
    public bool isReloading { get; private set; } = false;

    [Header("射击物理枪口")]
    public Transform firePoint;

    private float nextFireTime = 0f;

    // 🌟 核心：记录玩家【生前】曾经拥有过哪些武器的名字（防止重复白嫖备弹）
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
            // 如果初始手里有枪，默认视作已解锁，并给予其默认备弹
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
                // 1. 生成子弹
                GameObject bulletObj = Instantiate(currentWeaponData.bulletPrefab, firePoint.position, firePoint.rotation);

                // 2. 🌟 核心注入：获取子弹身上的脚本，直接把配置文件里的伤害灌进去！
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

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.name == "GunPickup")
        {
            hasWeapon = true;

            if (currentWeaponData != null)
            {
                // 🌟 绝妙逻辑判断：看清单里有没有这把枪的名字
                if (!unlockedWeapons.Contains(weaponName))
                {
                    // 第一次捡到：永久记录名字，并赠送初始备弹！
                    unlockedWeapons.Add(weaponName);
                    reserveAmmo = currentWeaponData.defaultReserveAmmo;
                    currentAmmo = maxMagazineSize;
                }
                else
                {
                    // 再次拿到同款枪：只回满当前的弹夹子弹，绝对不触发额外的 reserveAmmo 赠送！
                    currentAmmo = maxMagazineSize;
                }
            }

            OnAmmoChanged?.Invoke();
            Destroy(other.gameObject);
        }
    }

    // 契约重置：复活时，把备弹重新洗回当前配置文件的专属初始数量
    public void ResetData()
    {
        OnReloadComplete?.Invoke();
        StopAllCoroutines();
        isReloading = false;

        if (currentWeaponData != null)
        {
            currentAmmo = maxMagazineSize;
            reserveAmmo = currentWeaponData.defaultReserveAmmo; // 复活时按当前配置重新补充
        }

        OnAmmoChanged?.Invoke();
    }
}