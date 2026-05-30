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

    // 🌟 交互核心防抖与雷达队列
    private static float globalPickupTimer = 0f;
    private const float PICKUP_COOLDOWN = 1.0f;
    private List<IInteractable> nearbyInteractables = new List<IInteractable>(); // 附近所有的交互物

    // 🌟 交互事件广播：告诉 UI 该显示什么字，或者隐藏 UI
    public static event Action<string> OnShowInteractionUI;
    public static event Action OnHideInteractionUI;

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
        // 1. 正常的射击与换弹逻辑
        HandleShootingAndReloading();

        // 2. 🌟 通用交互检测
        HandleUniversalInteraction();
    }

    private void HandleShootingAndReloading()
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

        if (Keyboard.current.rKey.wasPressedThisFrame && currentAmmo < maxMagazineSize && reserveAmmo > 0)
        {
            StartCoroutine(ReloadRoutine());
        }
    }

    // 🌟 核心函数：处理通用交互与多物体冲突
    private void HandleUniversalInteraction()
    {
        // 清理列表中可能已经被 Destroy 掉的无效残留（安全防护）
        nearbyInteractables.RemoveAll(x => x == null || (x is MonoBehaviour mb && mb == null));

        if (nearbyInteractables.Count == 0)
        {
            OnHideInteractionUI?.Invoke();
            return;
        }

        // 💡 冲突破局：遍历列表，通过计算物理距离，死死咬住【离玩家最近的那一个】！
        IInteractable closestInteractable = null;
        float closestDistance = Mathf.Infinity;

        foreach (var interactable in nearbyInteractables)
        {
            if (interactable is MonoBehaviour mb)
            {
                float dist = Vector3.Distance(transform.position, mb.transform.position);
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    closestInteractable = interactable;
                }
            }
        }

        if (closestInteractable == null) return;

        // 通知 UI 实时刷新这个最近物体的提示文字（不管它是门还是枪！）
        OnShowInteractionUI?.Invoke(closestInteractable.InteractionPrompt);

        // 监听按 E 键
        if (Keyboard.current.eKey.wasPressedThisFrame && Time.time >= globalPickupTimer)
        {
            globalPickupTimer = Time.time + PICKUP_COOLDOWN;

            // 执行交互前，先把它从队列里移除（因为它马上要被处理或传走）
            IInteractable target = closestInteractable;
            nearbyInteractables.Remove(target);

            // 砰！触发这个物体特有的 Interact 逻辑
            target.Interact(this);
        }
    }

    // 🌟 只要挂了 IInteractable 接口的物体调大了触发箱，走进去就会被雷达捕获
    private void OnTriggerEnter(Collider other)
    {
        IInteractable interactable = other.GetComponent<IInteractable>();
        if (interactable != null && !nearbyInteractables.Contains(interactable))
        {
            nearbyInteractables.Add(interactable);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        IInteractable interactable = other.GetComponent<IInteractable>();
        if (interactable != null && nearbyInteractables.Contains(interactable))
        {
            nearbyInteractables.Remove(interactable);
        }
    }

    // 供外部枪械调用的核心数值交换
    public void ExecuteInteraction(GunPickup groundGun, GameObject groundGameObject)
    {
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

        if (groundGun.weaponConfig.weaponName == this.weaponName)
        {
            currentAmmo = maxMagazineSize;
            int bonusAmmo = groundGun.weaponConfig.maxMagazineSize * 2;
            reserveAmmo += bonusAmmo;
            if (reserveAmmo > groundGun.weaponConfig.defaultReserveAmmo) reserveAmmo = groundGun.weaponConfig.defaultReserveAmmo;
            OnAmmoChanged?.Invoke();
            Destroy(groundGameObject);
        }
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

            // 换枪后，把被吐出来的旧枪重新加回附近雷达列表，允许玩家等1秒后重新换回来
            IInteractable newGroundInteractable = groundGameObject.GetComponent<IInteractable>();
            if (newGroundInteractable != null) nearbyInteractables.Add(newGroundInteractable);

            OnAmmoChanged?.Invoke();
        }
    }

    void Shoot() { /* 保持原样... */ if (currentAmmo > 0) { currentAmmo--; OnAmmoChanged?.Invoke(); if (currentWeaponData.bulletPrefab != null && firePoint != null) { GameObject bulletObj = Instantiate(currentWeaponData.bulletPrefab, firePoint.position, firePoint.rotation); Bullet bulletScript = bulletObj.GetComponent<Bullet>(); if (bulletScript != null) bulletScript.Initialize(currentWeaponData.damage); } } else { OnEmptyClipFired?.Invoke(); } }
    IEnumerator ReloadRoutine() { isReloading = true; float elapsed = 0f; while (elapsed < reloadTime) { elapsed += Time.deltaTime; OnReloading?.Invoke(elapsed); yield return null; } reserveAmmo += currentAmmo; currentAmmo = 0; int ammoNeeded = maxMagazineSize; if (reserveAmmo >= ammoNeeded) { currentAmmo = ammoNeeded; reserveAmmo -= ammoNeeded; } else { currentAmmo = reserveAmmo; reserveAmmo = 0; } isReloading = false; OnReloadComplete?.Invoke(); OnAmmoChanged?.Invoke(); }
    public void ResetData() { OnReloadComplete?.Invoke(); StopAllCoroutines(); isReloading = false; nearbyInteractables.Clear(); if (currentWeaponData != null) { currentAmmo = maxMagazineSize; reserveAmmo = currentWeaponData.defaultReserveAmmo; } OnAmmoChanged?.Invoke(); }
}