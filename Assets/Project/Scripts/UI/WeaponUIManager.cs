using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class WeaponUIManager : MonoBehaviour
{
    [Header("🧩 场景中的 Player 引用")]
    public PlayerShooting playerWeapon;

    [Header("🎨 UI 独立模块连线")]
    public TextMeshProUGUI reloadActionText;  // "按R/左键换弹"
    public TextMeshProUGUI reloadStatusText;  // "正在换弹..."
    public Slider reloadSlider;

    [Header("🎮 双武器 UI — 每槽: 弹药行(左) + 武器名(右)")]
    public TextMeshProUGUI slot0AmmoText;
    public TextMeshProUGUI slot0NameText;
    public TextMeshProUGUI slot1AmmoText;
    public TextMeshProUGUI slot1NameText;
    public RectTransform slot0Container;
    public RectTransform slot1Container;

    [Header("🎯 主手突出偏移")]
    [Tooltip("主手武器向左突出（负值=更左）")]
    public float activeSlotLeftShift = -30f;

    [Header("📐 布局偏移")]
    [Tooltip("槽位 1 垂直偏移")]
    public float slot1VerticalOffset = -52f;

    [Header("🔤 字体大小")]
    [Tooltip("主手武器名字号")]
    public float activeNameFontSize = 36f;
    [Tooltip("非主手武器名字号")]
    public float inactiveNameFontSize = 28f;
    [Tooltip("主手弹药主数字号（相对偏移）")]
    public float activeAmmoMainFontSize = 6f;
    [Tooltip("非主手弹药主数字号")]
    public float inactiveAmmoMainFontSize = 24f;
    [Tooltip("非主手弹药备弹字号")]
    public float inactiveAmmoSubFontSize = 20f;
    [Tooltip("连击指示器字号")]
    public float comboFontSize = 24f;

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

    void RefreshAmmoUI()
    {
        if (playerWeapon == null) return;

        // ── 双槽 UI ──
        RefreshSlot(slot0AmmoText, slot0NameText, 0);
        RefreshSlot(slot1AmmoText, slot1NameText, 1);

        // ── 主手突进：活跃槽左移，非活跃槽复位 ──
        ApplySlotShift();

        // ── 清除空弹警告（有弹药时 / 切到近战时） ──
        bool isMelee = playerWeapon.hasWeapon && playerWeapon.currentWeaponData?.weaponType == WeaponType.Melee;
        if (isMelee || (playerWeapon.currentAmmo > 0 && !playerWeapon.isReloading))
        {
            if (reloadActionText != null) reloadActionText.text = "";
            if (reloadStatusText != null) reloadStatusText.text = "";
            if (reloadSlider != null) reloadSlider.gameObject.SetActive(false);
        }
    }

    void ApplySlotShift()
    {
        int activeIdx = playerWeapon.activeSlotIndex;
        float shift = activeSlotLeftShift;

        if (slot0Container != null)
        {
            slot0Container.anchoredPosition = new Vector2(
                (activeIdx == 0) ? shift : 0f,
                0f);
        }
        if (slot1Container != null)
        {
            slot1Container.anchoredPosition = new Vector2(
                (activeIdx == 1) ? shift : 0f,
                slot1VerticalOffset);
        }
    }

    // ─── 颜色层级 ───
    static readonly Color Gray1 = new Color(0.914f, 0.914f, 0.914f);  // #E9E9E9  接近白
    static readonly Color Gray2 = new Color(0.816f, 0.816f, 0.816f);  // #D0D0D0  比灰1深一档

    void RefreshSlot(TextMeshProUGUI ammoText, TextMeshProUGUI nameText, int slotIndex)
    {
        WeaponSlot slot = (playerWeapon.slots != null && slotIndex < playerWeapon.slots.Length)
            ? playerWeapon.slots[slotIndex] : null;

        bool isActive = (slotIndex == playerWeapon.activeSlotIndex);

        if (slot == null || slot.IsEmpty)
        {
            if (ammoText != null) { ammoText.text = "-"; ammoText.color = Gray2; }
            if (nameText != null) { nameText.text = "空";  nameText.color = Gray2; }
            return;
        }

        string weaponName = slot.weaponData != null ? slot.weaponData.weaponName : "?";

        // ── 近战武器：不显示弹药，武器名加连击段数 ──
        if (slot.IsMelee)
        {
            if (ammoText != null) { ammoText.text = ""; ammoText.color = Gray2; }

            if (nameText != null)
            {
                if (isActive)
                {
                    nameText.color = Color.white;
                    int combo = playerWeapon.MeleeComboStage;
                    string comboStr = combo > 0 ? $" <size={comboFontSize}><color=#E9E9E9>连{combo}</color></size>" : "";
                    nameText.text = $"<size={activeNameFontSize}>{weaponName}{comboStr}</size>";
                }
                else
                {
                    nameText.color = Gray1;
                    nameText.text = $"<size={inactiveNameFontSize}>{weaponName}</size>";
                }
            }
            return;
        }

        // ── 弹药行：主弹药(换行)备弹 ──
        if (ammoText != null)
        {
            if (isActive)
            {
                ammoText.color = Color.white;
                ammoText.text = $"<size=+{activeAmmoMainFontSize}>{slot.currentAmmo}</size>\n<color=#E9E9E9>  {slot.reserveAmmo}</color>";
            }
            else
            {
                ammoText.color = Gray1;
                ammoText.text = $"<size={inactiveAmmoMainFontSize}>{slot.currentAmmo}</size>\n<size={inactiveAmmoSubFontSize}><color=#D0D0D0>  {slot.reserveAmmo}</color></size>";
            }
        }

        // ── 武器名：主手白，非主手灰 ──
        if (nameText != null)
        {
            if (isActive)
            {
                nameText.color = Color.white;
                nameText.text = $"<size={activeNameFontSize}>{weaponName}</size>";
            }
            else
            {
                nameText.color = Gray1;
                nameText.text = $"<size={inactiveNameFontSize}>{weaponName}</size>";
            }
        }
    }

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

    void HideReloadUI()
    {
        if (reloadSlider != null) reloadSlider.gameObject.SetActive(false);
        if (reloadActionText != null) reloadActionText.text = "";
        if (reloadStatusText != null) reloadStatusText.text = "";
    }

    void ShowEmptyWarning()
    {
        if (reloadActionText != null) reloadActionText.text = "按R/左键换弹";
    }
}
