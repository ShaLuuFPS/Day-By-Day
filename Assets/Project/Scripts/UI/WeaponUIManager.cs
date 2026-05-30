using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class WeaponUIManager : MonoBehaviour
{
    [Header("🧩 场景中的 Player 引用")]
    public PlayerShooting playerWeapon;

    [Header("🎨 UI 独立模块连线")]
    public TextMeshProUGUI weaponNameText;
    public TextMeshProUGUI ammoCountText;
    public TextMeshProUGUI reloadStatusText;
    public Slider reloadSlider;

    [Header("🌟 新增：交互提示UI模块")]
    public GameObject interactionPanel;        // 交互提示框的总父物体（用于整体隐藏/显示）
    public TextMeshProUGUI interactionText;    // 显示类似 "按 E 换取 突击步枪"

    void OnEnable()
    {
        PlayerShooting.OnAmmoChanged += RefreshAmmoUI;
        PlayerShooting.OnReloading += HandleReloadingUI;
        PlayerShooting.OnReloadComplete += HideReloadUI;
        PlayerShooting.OnEmptyClipFired += ShowEmptyWarning;

        // 🌟 订阅全新交互事件
        PlayerShooting.OnShowInteractionUI += ShowInteractionTip;
        PlayerShooting.OnHideInteractionUI += HideInteractionTip;
    }

    void OnDisable()
    {
        PlayerShooting.OnAmmoChanged -= RefreshAmmoUI;
        PlayerShooting.OnReloading -= HandleReloadingUI;
        PlayerShooting.OnReloadComplete -= HideReloadUI;
        PlayerShooting.OnEmptyClipFired -= ShowEmptyWarning;

        PlayerShooting.OnShowInteractionUI -= ShowInteractionTip;
        PlayerShooting.OnHideInteractionUI -= HideInteractionTip;
    }

    void Start()
    {
        if (reloadSlider != null) reloadSlider.gameObject.SetActive(false);
        if (interactionPanel != null) interactionPanel.gameObject.SetActive(false); // 默认隐藏交互框
        RefreshAmmoUI();
    }

    // 🌟 显示通用交互 UI
    void ShowInteractionTip(string promptText)
    {
        if (interactionPanel != null && !interactionPanel.activeSelf) interactionPanel.SetActive(true);
        if (interactionText != null) interactionText.text = $"<color=yellow>按 E</color> {promptText}";
    }

    // 🌟 隐藏通用交互 UI
    void HideInteractionTip()
    {
        if (interactionPanel != null && interactionPanel.activeSelf) interactionPanel.SetActive(false);
    }

    void RefreshAmmoUI() { if (playerWeapon == null) return; if (weaponNameText != null) weaponNameText.text = playerWeapon.hasWeapon ? playerWeapon.weaponName : "空手"; if (ammoCountText != null) ammoCountText.text = playerWeapon.hasWeapon ? $"{playerWeapon.currentAmmo} / {playerWeapon.reserveAmmo}" : "- / -"; if (playerWeapon.hasWeapon && playerWeapon.currentAmmo > 0 && !playerWeapon.isReloading) { if (reloadStatusText != null) reloadStatusText.text = ""; } }
    void HandleReloadingUI(float elapsed) { if (playerWeapon == null) return; if (reloadSlider != null) { if (!reloadSlider.gameObject.activeSelf) reloadSlider.gameObject.SetActive(true); reloadSlider.value = elapsed / playerWeapon.reloadTime; } if (reloadStatusText != null) { float remaining = playerWeapon.reloadTime - elapsed; reloadStatusText.text = $"正在换弹... ({remaining.ToString("F1")}s)"; } }
    void HideReloadUI() { if (reloadSlider != null) reloadSlider.gameObject.SetActive(false); if (reloadStatusText != null) reloadStatusText.text = ""; }
    void ShowEmptyWarning() { if (reloadStatusText != null) reloadStatusText.text = "没子弹了，请按 R 换弹！"; }
}