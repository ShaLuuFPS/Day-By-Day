using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections;

/// <summary>
/// 枪械模块：射击、瞄准射线、换弹（弹夹式 + 逐发）。
/// 拆自 PlayerShooting 上帝模块。
/// </summary>
public class GunModule : MonoBehaviour, IResettable
{
    [Header("射击物理枪口")]
    public Transform firePoint;

    private WeaponManager weaponManager;

    // 射击状态
    private float nextFireTime = 0f;
    private bool requireFreshPress = false;

    // 换弹状态
    public bool isReloading { get; private set; } = false;
    private bool interruptReload = false;

    // ── 举枪瞄准状态（右键按住） ──
    public bool IsAiming
    {
        get
        {
            if (GameStateManager.IsInputFrozen) return false;
            return !isReloading && Mouse.current != null && Mouse.current.rightButton.isPressed;
        }
    }

    // 空弹左键换弹
    private float emptyClipSince = -999f;
    private bool emptyPromptShown = false;
    private const float EmptyReloadDelay = 0.5f;

    // ── 事件 ──
    public event Action<float> OnReloading;
    public event Action OnReloadComplete;
    public event Action OnEmptyClipFired;

    void Awake()
    {
        weaponManager = GetComponent<WeaponManager>();
    }

    void Update()
    {
        if (GameStateManager.IsInputFrozen) return;

        // 非枪械武器不处理
        if (weaponManager == null || !weaponManager.hasWeapon) return;
        if (weaponManager.IsCurrentMelee) return;

        HandleGunInput();
    }

    /// <summary>切枪时 WeaponManager 调用，重置 requireFreshPress</summary>
    public void ResetOnSwap()
    {
        requireFreshPress = true;
        emptyPromptShown = false;
        emptyClipSince = -999f;
    }

    /// <summary>被打断换弹（切枪/拾取同款武器）</summary>
    public void InterruptReload()
    {
        if (isReloading)
        {
            StopAllCoroutines();
            isReloading = false;
            OnReloadComplete?.Invoke();
        }
    }

    // ── 输入处理 ──

    private void HandleGunInput()
    {
        GunData gunData = weaponManager.CurrentGunData;
        if (gunData == null) return;
        int slotAmmo = weaponManager.currentAmmo;

        // 逐发装填中：允许射击键打断
        if (isReloading)
        {
            if (!gunData.isMagazineReload && Mouse.current.leftButton.wasPressedThisFrame)
                interruptReload = true;
            return;
        }

        // 射击输入 — 必须举枪瞄准（RMB）才能开火
        if (IsAiming)
        {
            if (requireFreshPress)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame && Time.time >= nextFireTime)
                {
                    ExecuteGunShoot();
                    nextFireTime = Time.time + gunData.fireRate;
                    requireFreshPress = false;
                }
            }
            else if (gunData.isAutomatic)
            {
                if (Mouse.current.leftButton.isPressed && Time.time >= nextFireTime)
                {
                    ExecuteGunShoot();
                    nextFireTime = Time.time + gunData.fireRate;
                }
            }
            else
            {
                if (Mouse.current.leftButton.wasPressedThisFrame && Time.time >= nextFireTime)
                {
                    ExecuteGunShoot();
                    nextFireTime = Time.time + gunData.fireRate;
                }
            }
        }

        // 空弹夹左键换弹
        bool clipEmpty = slotAmmo <= 0 && weaponManager.reserveAmmo > 0;
        if (clipEmpty)
        {
            if (!emptyPromptShown)
            {
                emptyClipSince = Time.time;
                emptyPromptShown = true;
                OnEmptyClipFired?.Invoke();
            }

            if ((Time.time - emptyClipSince) >= EmptyReloadDelay
                && Mouse.current.leftButton.isPressed && !isReloading)
            {
                StartCoroutine(ReloadRoutine());
                emptyPromptShown = false;
            }
        }
        else
        {
            emptyPromptShown = false;
        }

        // R 键换弹
        if (Keyboard.current.rKey.wasPressedThisFrame
            && slotAmmo < weaponManager.maxMagazineSize
            && weaponManager.reserveAmmo > 0)
        {
            StartCoroutine(ReloadRoutine());
        }
    }

    // ── 射击 ──

    private void ExecuteGunShoot()
    {
        GunData gunData = weaponManager.CurrentGunData;
        if (gunData == null) return;

        if (weaponManager.currentAmmo > 0)
        {
            weaponManager.currentAmmo--;
            weaponManager.InvokeAmmoChanged();

            if (gunData.bulletPrefab != null && firePoint != null)
            {
                Vector3 aimDirection;
                Camera cam = Camera.main;
                if (cam != null)
                {
                    Ray ray = cam.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0));
                    if (Physics.Raycast(ray, out RaycastHit hit, 1000f, Physics.DefaultRaycastLayers))
                        aimDirection = (hit.point - firePoint.position).normalized;
                    else
                        aimDirection = ray.direction;
                }
                else
                {
                    aimDirection = firePoint.forward;
                }

                int pellets = gunData.bulletsPerShot;
                float spread = gunData.spreadAngle;

                for (int i = 0; i < pellets; i++)
                {
                    Quaternion pelletRotation = Quaternion.LookRotation(aimDirection);
                    if (spread > 0f)
                    {
                        float randomYaw   = UnityEngine.Random.Range(-spread, spread);
                        float randomPitch = UnityEngine.Random.Range(-spread, spread);
                        pelletRotation = Quaternion.LookRotation(aimDirection)
                            * Quaternion.Euler(randomPitch, randomYaw, 0f);
                    }

                    GameObject bulletObj = Instantiate(gunData.bulletPrefab, firePoint.position, pelletRotation);
                    Bullet bulletScript = bulletObj.GetComponent<Bullet>();
                    if (bulletScript != null)
                        bulletScript.Initialize(gunData.damage);
                }
            }
        }
        else
        {
            OnEmptyClipFired?.Invoke();
        }
    }

    // ── 换弹 ──

    private IEnumerator ReloadRoutine()
    {
        GunData gunData = weaponManager.CurrentGunData;
        if (gunData == null) yield break;

        isReloading = true;
        interruptReload = false;

        if (gunData.isMagazineReload)
        {
            float elapsed = 0f;
            float total = weaponManager.reloadTime;
            while (elapsed < total)
            {
                elapsed += Time.deltaTime;
                OnReloading?.Invoke(elapsed);
                yield return null;
            }

            weaponManager.reserveAmmo += weaponManager.currentAmmo;
            weaponManager.currentAmmo = 0;
            int ammoNeeded = weaponManager.maxMagazineSize;

            if (weaponManager.reserveAmmo >= ammoNeeded)
            {
                weaponManager.currentAmmo = ammoNeeded;
                weaponManager.reserveAmmo -= ammoNeeded;
            }
            else
            {
                weaponManager.currentAmmo = weaponManager.reserveAmmo;
                weaponManager.reserveAmmo = 0;
            }
        }
        else
        {
            while (weaponManager.currentAmmo < weaponManager.maxMagazineSize
                && weaponManager.reserveAmmo > 0 && !interruptReload)
            {
                float shellElapsed = 0f;
                float shellTime = gunData.perShellReloadTime;

                while (shellElapsed < shellTime && !interruptReload)
                {
                    shellElapsed += Time.deltaTime;
                    OnReloading?.Invoke(shellElapsed);
                    yield return null;
                }

                weaponManager.currentAmmo++;
                weaponManager.reserveAmmo--;
                weaponManager.InvokeAmmoChanged();
            }
        }

        isReloading = false;
        OnReloadComplete?.Invoke();
        weaponManager.InvokeAmmoChanged();
    }

    // ── IResettable ──

    public void ResetData()
    {
        StopAllCoroutines();
        isReloading = false;
        OnReloadComplete?.Invoke();
    }
}
