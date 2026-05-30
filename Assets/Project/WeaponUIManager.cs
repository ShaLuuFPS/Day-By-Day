using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class WeaponUIManager : MonoBehaviour
{
    [Header("🧩 场景中的 Player 引用")]
    public PlayerShooting playerWeapon; // 连线主角，用来读取名字、子弹等数值

    [Header("🎨 UI 独立模块连线")]
    public TextMeshProUGUI weaponNameText;
    public TextMeshProUGUI ammoCountText;
    public TextMeshProUGUI reloadStatusText;
    public Slider reloadSlider;

    // 🌟 在脚本激活时，订阅 PlayerShooting 的事件通知
    void OnEnable()
    {
        PlayerShooting.OnAmmoChanged += RefreshAmmoUI;
        PlayerShooting.OnReloading += HandleReloadingUI;
        PlayerShooting.OnReloadComplete += HideReloadUI;
        PlayerShooting.OnEmptyClipFired += ShowEmptyWarning;
    }

    // 🚨 在脚本销毁/禁用时，必须取消订阅，防止内存泄漏
    void OnDisable()
    {
        PlayerShooting.OnAmmoChanged -= RefreshAmmoUI;
        PlayerShooting.OnReloading -= HandleReloadingUI;
        PlayerShooting.OnReloadComplete -= HideReloadUI;
        PlayerShooting.OnEmptyClipFired -= ShowEmptyWarning;
    }

    void Start()
    {
        if (reloadSlider != null) reloadSlider.gameObject.SetActive(false);
        RefreshAmmoUI();
    }

    // 1. 刷新弹药与名字
    void RefreshAmmoUI()
    {
        if (playerWeapon == null) return;

        if (weaponNameText != null)
            weaponNameText.text = playerWeapon.hasWeapon ? playerWeapon.weaponName : "空手";

        if (ammoCountText != null)
            ammoCountText.text = playerWeapon.hasWeapon ? $"{playerWeapon.currentAmmo} / {playerWeapon.reserveAmmo}" : "- / -";

        // 平时没换弹且有子弹，清空提示
        if (playerWeapon.hasWeapon && playerWeapon.currentAmmo > 0 && !playerWeapon.isReloading)
        {
            if (reloadStatusText != null) reloadStatusText.text = "";
        }
    }

    // 2. 处理换弹期间的 UI 动画
    void HandleReloadingUI(float elapsed)
    {
        if (playerWeapon == null) return;

        if (reloadSlider != null)
        {
            if (!reloadSlider.gameObject.activeSelf) reloadSlider.gameObject.SetActive(true);
            reloadSlider.value = elapsed / playerWeapon.reloadTime;
        }

        if (reloadStatusText != null)
        {
            float remaining = playerWeapon.reloadTime - elapsed;
            reloadStatusText.text = $"正在换弹... ({remaining.ToString("F1")}s)";
        }
    }

    // 3. 换弹完毕隐藏 UI
    void HideReloadUI()
    {
        if (reloadSlider != null) reloadSlider.gameObject.SetActive(false);
        if (reloadStatusText != null) reloadStatusText.text = "";
    }

    // 4. 空弹夹警告
    void ShowEmptyWarning()
    {
        if (reloadStatusText != null)
        {
            reloadStatusText.text = "没子弹了，请按 R 换弹！";
        }
    }
}