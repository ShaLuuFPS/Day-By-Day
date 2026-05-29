using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class PlayerShooting : MonoBehaviour, IResettable
{
    [Header("枪械基础设置")]
    public string weaponName = "手枪";
    public int maxMagazineSize = 7;    // 弹夹容量上限
    public int reserveAmmo = 28;       // 备弹总量
    public float reloadTime = 1.5f;

    [Header("枪械实时状态")]
    public bool hasWeapon = false;     // 是否已经捡到枪
    public int currentAmmo = 7;        // 当前弹夹子弹
    private bool isReloading = false;

    [Header("射击预制体")]
    public GameObject bulletPrefab;    // 子弹预制体
    public Transform firePoint;        // 枪口位置

    [Header("🧩 UI 独立模块连线")]
    public TextMeshProUGUI weaponNameText;  // 模块 1：只负责武器名字
    public TextMeshProUGUI ammoCountText;   // 模块 2：只负责弹药数字（7 / 28）
    public TextMeshProUGUI reloadStatusText;// 模块 3：只负责换弹状态提示（正在换弹/请按R）
    public Slider reloadSlider;

    void Start()
    {
        UpdateWeaponUI(); // 初始化所有独立的 UI 模块
        if (reloadSlider != null) reloadSlider.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!hasWeapon) return; //

        // 如果正在换弹，拦截开火和重复换弹
        if (isReloading) return;

        // 1. 监听鼠标左键开火
        if (Mouse.current.leftButton.wasPressedThisFrame) //
        {
            Shoot(); //
        }

        // 2. 监听 R 键换弹
        if (Keyboard.current.rKey.wasPressedThisFrame) //
        {
            if (currentAmmo < maxMagazineSize && reserveAmmo > 0) //
            {
                StartCoroutine(ReloadRoutine());
            }
        }
    }

    void Shoot()
    {
        if (currentAmmo > 0) //
        {
            currentAmmo--; //
            UpdateWeaponUI(); // 射击后，只刷新弹药数字
            Instantiate(bulletPrefab, firePoint.position, firePoint.rotation); //
        }
        else
        {
            // 模块 3 闪亮登场：空弹开火时，只在提示模块显示警告，不干扰名字和数字
            if (reloadStatusText != null)
            {
                reloadStatusText.text = "没子弹了，请按 R 换弹！";
            }
        }
    }

    IEnumerator ReloadRoutine()
    {
        isReloading = true;

        // 激活进度条 UI
        if (reloadSlider != null)
        {
            reloadSlider.value = 0f;
            reloadSlider.gameObject.SetActive(true);
        }

        float elapsed = 0f;
        while (elapsed < reloadTime)
        {
            elapsed += Time.deltaTime;

            if (reloadSlider != null)
            {
                reloadSlider.value = elapsed / reloadTime;
            }

            // 模块 3 独立控制：在屏幕中央动态打印倒计时，完全不影响右下角弹药数
            if (reloadStatusText != null)
            {
                reloadStatusText.text = $"正在换弹... ({(reloadTime - elapsed).ToString("F1")}s)";
            }

            yield return null;
        }

        // --- ⏳ 时间到！执行数值交换 ---
        reserveAmmo += currentAmmo; //
        currentAmmo = 0; //
        int ammoNeeded = maxMagazineSize; //

        if (reserveAmmo >= ammoNeeded) //
        {
            currentAmmo = ammoNeeded; //
            reserveAmmo -= ammoNeeded; //
        }
        else //
        {
            currentAmmo = reserveAmmo; //
            reserveAmmo = 0; //
        }

        // 换弹完毕，清理提示模块和进度条
        if (reloadSlider != null) reloadSlider.gameObject.SetActive(false);
        if (reloadStatusText != null) reloadStatusText.text = ""; // 清空换弹提示

        isReloading = false;
        UpdateWeaponUI(); // 最终全面刷新一次 UI
    }

    private void OnTriggerEnter(Collider other) //
    {
        if (other.gameObject.name == "GunPickup") //
        {
            hasWeapon = true; //
            UpdateWeaponUI(); //
            Destroy(other.gameObject); //
        }
    }

    // 🎯 核心重构：三大模块各司其职，各回各家
    void UpdateWeaponUI()
    {
        // 1. 名字模块处理
        if (weaponNameText != null)
        {
            weaponNameText.text = hasWeapon ? weaponName : "空手";
        }

        // 2. 弹药模块处理
        if (ammoCountText != null)
        {
            ammoCountText.text = hasWeapon ? $"{currentAmmo} / {reserveAmmo}" : "- / -";
        }

        // 3. 提示模块处理：平时默认清空
        if (hasWeapon && currentAmmo > 0 && !isReloading)
        {
            if (reloadStatusText != null) reloadStatusText.text = "";
        }
    }

    // 契约重置：如果死后重置，强行洗白所有 UI 模块现场
    public void ResetData()
    {
        StopAllCoroutines();

        if (reloadSlider != null) reloadSlider.gameObject.SetActive(false);
        if (reloadStatusText != null) reloadStatusText.text = ""; // 清空提示

        isReloading = false;
        currentAmmo = maxMagazineSize;
        reserveAmmo = 28;
        UpdateWeaponUI();
    }
}