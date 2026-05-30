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

    [Header("🌟 交互提示UI模块")]
    public GameObject interactionPanel;
    public TextMeshProUGUI interactionText;

    void OnEnable()
    {
        PlayerShooting.OnAmmoChanged += RefreshAmmoUI;
        PlayerShooting.OnReloading += HandleReloadingUI;
        PlayerShooting.OnReloadComplete += HideReloadUI;
        PlayerShooting.OnEmptyClipFired += ShowEmptyWarning;

        // 🌟 核心修改：改为主持订阅 PlayerInteraction 广播出来的交互UI事件
        PlayerInteraction.OnShowInteractionUI += ShowInteractionTip;
        PlayerInteraction.OnHideInteractionUI += HideInteractionTip;
    }

    void OnDisable()
    {
        PlayerShooting.OnAmmoChanged -= RefreshAmmoUI;
        PlayerShooting.OnReloading -= HandleReloadingUI;
        PlayerShooting.OnReloadComplete -= HideReloadUI;
        PlayerShooting.OnEmptyClipFired -= ShowEmptyWarning;

        // 🌟 核心修改：取消订阅
        PlayerInteraction.OnShowInteractionUI -= ShowInteractionTip;
        PlayerInteraction.OnHideInteractionUI -= HideInteractionTip;
    }

    void Start()
    {
        if (reloadSlider != null) reloadSlider.gameObject.SetActive(false);
        if (interactionPanel != null) interactionPanel.gameObject.SetActive(false);
        RefreshAmmoUI();
    }

    void ShowInteractionTip(string promptText)
    {
        if (interactionPanel != null && !interactionPanel.activeSelf) interactionPanel.SetActive(true);
        if (interactionText != null) interactionText.text = $"<color=yellow>按 E</color> {promptText}";
    }

    void HideInteractionTip()
    {
        if (interactionPanel != null && interactionPanel.activeSelf) interactionPanel.SetActive(false);
    }

    void RefreshAmmoUI() { if (playerWeapon == null) return; if (weaponNameText != null) weaponNameText.text = playerWeapon.hasWeapon ? playerWeapon.weaponName : "空手"; if (ammoCountText != null) ammoCountText.text = playerWeapon.hasWeapon ? $"{playerWeapon.currentAmmo} / {playerWeapon.reserveAmmo}" : "- / -"; if (playerWeapon.hasWeapon && playerWeapon.currentAmmo > 0 && !playerWeapon.isReloading) { if (reloadStatusText != null) reloadStatusText.text = ""; } }
    void HandleReloadingUI(float elapsed) { if (playerWeapon == null) return; if (reloadSlider != null) { if (!reloadSlider.gameObject.activeSelf) reloadSlider.gameObject.SetActive(true); reloadSlider.value = elapsed / playerWeapon.reloadTime; } if (reloadStatusText != null) { float remaining = playerWeapon.reloadTime - elapsed; reloadStatusText.text = $"正在换弹... ({remaining.ToString("F1")}s)"; } }
    void HideReloadUI() { if (reloadSlider != null) reloadSlider.gameObject.SetActive(false); if (reloadStatusText != null) { reloadStatusText.text = ""; } }
    void ShowEmptyWarning() { if (reloadStatusText != null) reloadStatusText.text = "没子弹了，请按 R 换弹！"; }
}