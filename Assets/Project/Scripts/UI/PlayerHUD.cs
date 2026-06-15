using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 常驻 HUD —— 纯数据绑定。
/// 所有 UI 元素在场景中预置，通过 Inspector 拖拽连线。
/// </summary>
public class PlayerHUD : MonoBehaviour
{
    [Header("UI 连线")]
    [SerializeField] private Slider xpBar;
    [SerializeField] private TextMeshProUGUI xpText;
    [SerializeField] private TextMeshProUGUI waveText;
    [SerializeField] private Slider healthBar;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private Slider staminaBar;
    [SerializeField] private TextMeshProUGUI staminaText;

    [Header("数据引用")]
    [SerializeField] private PlayerXP playerXP;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerStamina playerStamina;
    [SerializeField] private WaveManager waveManager;

    private Coroutine xpFlashRoutine;

    void OnEnable()
    {
        if (playerXP != null)
        {
            PlayerXP.OnXPGained += OnXPGained;
            PlayerXP.OnLevelUp += OnLevelUp;
        }
        if (playerHealth != null)
            PlayerHealth.OnHealthChanged += OnHealthChanged;
        if (playerStamina != null)
            PlayerStamina.OnStaminaChanged += OnStaminaChanged;
    }

    void OnDisable()
    {
        PlayerXP.OnXPGained -= OnXPGained;
        PlayerXP.OnLevelUp -= OnLevelUp;
        PlayerHealth.OnHealthChanged -= OnHealthChanged;
        PlayerStamina.OnStaminaChanged -= OnStaminaChanged;
    }

    void Start()
    {
        RefreshAll();
    }

    void Update()
    {
        UpdateWaveText();
    }

    // ── XP ──

    void OnXPGained(float current, float max)
    {
        if (xpBar != null)
            xpBar.value = max > 0f ? Mathf.Clamp01(current / max) : 0f;
        RefreshXPText();
    }

    void OnLevelUp(int level)
    {
        if (xpFlashRoutine != null) StopCoroutine(xpFlashRoutine);
        xpFlashRoutine = StartCoroutine(LevelUpFlashRoutine());
    }

    IEnumerator LevelUpFlashRoutine()
    {
        if (xpBar != null) xpBar.value = 1f;
        if (xpText != null) xpText.text = "MAX!";

        yield return new WaitForSecondsRealtime(0.4f);

        float target = 0f;
        if (playerXP != null && playerXP.XPToNextLevel > 0f)
            target = Mathf.Clamp01(playerXP.CurrentXP / playerXP.XPToNextLevel);

        float duration = 0.5f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            if (xpBar != null)
                xpBar.value = Mathf.Lerp(1f, target, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        if (xpBar != null) xpBar.value = target;
        RefreshXPText();
    }

    void RefreshXPText()
    {
        if (xpText != null && playerXP != null)
            xpText.text = $"Lv.{playerXP.CurrentLevel}  {playerXP.CurrentXP:F0}/{playerXP.XPToNextLevel:F0} XP";
    }

    // ── Wave ──

    void UpdateWaveText()
    {
        if (waveText == null || waveManager == null || waveManager.waveConfig == null) return;

        int total = waveManager.waveConfig.waves.Length;
        if (total == 0) { waveText.text = ""; return; }

        int current = waveManager.CurrentWaveIndex + 1;
        if (current > total) current = total;
        waveText.text = $"波次 {current}/{total}";
    }

    // ── Health ──

    void OnHealthChanged(float current, float max)
    {
        if (healthBar != null)
            healthBar.value = max > 0f ? Mathf.Clamp01(current / max) : 0f;
        if (healthText != null)
            healthText.text = $"{current:F0}";
    }

    // ── Stamina ──

    void OnStaminaChanged()
    {
        if (playerStamina == null) return;
        if (staminaBar != null)
            staminaBar.value = playerStamina.StaminaRatio;
        if (staminaText != null)
            staminaText.text = $"{playerStamina.CurrentStamina:F0}";
    }

    // ── Init ──

    void RefreshAll()
    {
        if (playerXP != null)
        {
            float ratio = playerXP.XPToNextLevel > 0f
                ? Mathf.Clamp01(playerXP.CurrentXP / playerXP.XPToNextLevel) : 0f;
            if (xpBar != null) xpBar.value = ratio;
            RefreshXPText();
        }

        if (playerHealth != null)
            OnHealthChanged(playerHealth.CurrentHealth, playerHealth.maxHealth);

        if (playerStamina != null)
            OnStaminaChanged();

        UpdateWaveText();
    }
}
