using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class WeaponUIManager : MonoBehaviour
{
    [Header("🧩 场景中的 Player 引用")]
    public PlayerShooting playerWeapon;

    [Header("🎨 UI 独立模块连线")]
    public TextMeshProUGUI reloadStatusText;
    public Slider reloadSlider;

    [Header("🎮 双武器 UI — 每槽: 弹药行(左对齐) + 武器名(居中)")]
    public TextMeshProUGUI slot0AmmoText;
    public TextMeshProUGUI slot0NameText;
    public TextMeshProUGUI slot1AmmoText;
    public TextMeshProUGUI slot1NameText;

    [Header("🎯 主手左突偏移")]
    [Tooltip("主手武器向左突出多少像素（负值=更左）")]
    public float activeSlotLeftShift = -50f;

    // 记录每个文本框在 Editor 中的初始位置（非主手状态的位置）
    private Vector2 slot0AmmoBase, slot0NameBase;
    private Vector2 slot1AmmoBase, slot1NameBase;
    private bool basesCaptured = false;

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

        // ── 双槽 UI：弹药行 + 武器名 ──
        RefreshSlot(slot0AmmoText, slot0NameText, 0);
        RefreshSlot(slot1AmmoText, slot1NameText, 1);

        // ── 清除空弹警告（有弹药时） ──
        if (playerWeapon.hasWeapon && playerWeapon.currentAmmo > 0 && !playerWeapon.isReloading)
        {
            if (reloadStatusText != null) reloadStatusText.text = "";
        }
    }

    // ─── 颜色层级 ───
    static readonly Color Gray1 = new Color(0.914f, 0.914f, 0.914f);  // #E9E9E9  接近白
    static readonly Color Gray2 = new Color(0.816f, 0.816f, 0.816f);  // #D0D0D0  比灰1深一档

    void RefreshSlot(TextMeshProUGUI ammoText, TextMeshProUGUI nameText, int slotIndex)
    {
        // 首次调用时抓取 Editor 中设置的初始位置（非主手基准位置）
        if (!basesCaptured) CaptureBasePositions();

        WeaponSlot slot = (playerWeapon.slots != null && slotIndex < playerWeapon.slots.Length)
            ? playerWeapon.slots[slotIndex] : null;

        bool isActive = (slotIndex == playerWeapon.activeSlotIndex);

        if (slot == null || slot.IsEmpty)
        {
            if (ammoText != null) { ammoText.text = "-"; ammoText.color = Gray2; }
            if (nameText != null) { nameText.text = "空";  nameText.color = Gray2; }
            ApplySlotShift(ammoText, nameText, slotIndex, false);
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
                    string comboStr = combo > 0 ? $" <size=24><color=#E9E9E9>连{combo}</color></size>" : "";
                    nameText.text = $"<size=36>{weaponName}{comboStr}</size>";
                }
                else
                {
                    nameText.color = Gray1;
                    nameText.text = $"<size=28>{weaponName}</size>";
                }
            }

            ApplySlotShift(ammoText, nameText, slotIndex, isActive);
            return;
        }

        // ── 弹药行：主弹药(换行)备弹 ──
        if (ammoText != null)
        {
            if (isActive)
            {
                ammoText.color = Color.white;
                ammoText.text = $"<size=+6>{slot.currentAmmo}</size>\n<color=#E9E9E9>  {slot.reserveAmmo}</color>";
            }
            else
            {
                ammoText.color = Gray1;
                ammoText.text = $"<size=24>{slot.currentAmmo}</size>\n<size=20><color=#D0D0D0>  {slot.reserveAmmo}</color></size>";
            }
        }

        // ── 武器名：主手白36号，非主手灰1 28号 ──
        if (nameText != null)
        {
            if (isActive)
            {
                nameText.color = Color.white;
                nameText.text = $"<size=36>{weaponName}</size>";
            }
            else
            {
                nameText.color = Gray1;
                nameText.text = $"<size=28>{weaponName}</size>";
            }
        }

        // ── 位置偏移：主手左突 ──
        ApplySlotShift(ammoText, nameText, slotIndex, isActive);
    }

    void CaptureBasePositions()
    {
        basesCaptured = true;
        if (slot0AmmoText != null) slot0AmmoBase = slot0AmmoText.rectTransform.anchoredPosition;
        if (slot0NameText != null) slot0NameBase = slot0NameText.rectTransform.anchoredPosition;
        if (slot1AmmoText != null) slot1AmmoBase = slot1AmmoText.rectTransform.anchoredPosition;
        if (slot1NameText != null) slot1NameBase = slot1NameText.rectTransform.anchoredPosition;
    }

    void ApplySlotShift(TextMeshProUGUI ammoText, TextMeshProUGUI nameText, int slotIndex, bool isActive)
    {
        Vector2 ammoBase = (slotIndex == 0) ? slot0AmmoBase : slot1AmmoBase;
        Vector2 nameBase = (slotIndex == 0) ? slot0NameBase : slot1NameBase;
        float shift = isActive ? activeSlotLeftShift : 0f;

        if (ammoText != null)
        {
            Vector2 p = ammoBase;
            p.x += shift;
            ammoText.rectTransform.anchoredPosition = p;
        }
        if (nameText != null)
        {
            Vector2 p = nameBase;
            p.x += shift;
            nameText.rectTransform.anchoredPosition = p;
        }
    }
    void HandleReloadingUI(float elapsed) { if (playerWeapon == null) return; if (reloadSlider != null) { if (!reloadSlider.gameObject.activeSelf) reloadSlider.gameObject.SetActive(true); reloadSlider.value = elapsed / playerWeapon.reloadTime; } if (reloadStatusText != null) { float remaining = playerWeapon.reloadTime - elapsed; reloadStatusText.text = $"正在换弹... ({remaining.ToString("F1")}s)"; } }
    void HideReloadUI() { if (reloadSlider != null) reloadSlider.gameObject.SetActive(false); if (reloadStatusText != null) { reloadStatusText.text = ""; } }
    void ShowEmptyWarning() { if (reloadStatusText != null) reloadStatusText.text = "没子弹了，请按 R 换弹！"; }
}