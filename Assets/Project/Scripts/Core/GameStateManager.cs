using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

/// <summary>
/// 游戏状态管理器 —— 集中管理暂停/胜利/游戏结束状态
/// </summary>
public class GameStateManager : MonoBehaviour
{
    // ── 静态状态 ──

    /// <summary>升级面板打开时的暂停（LevelUpManager 设置）</summary>
    public static bool IsUpgradePaused { get; set; } = false;

    /// <summary>ESC 手动暂停</summary>
    public static bool IsManualPaused { get; private set; } = false;

    /// <summary>统一暂停检查 —— Player 脚本通过此属性冻结输入</summary>
    public static bool IsPaused => IsUpgradePaused || IsManualPaused;

    /// <summary>游戏结束（死亡或胜利），禁止一切暂停操作</summary>
    public static bool IsGameOver { get; private set; } = false;

    // ── 事件 ──

    /// <summary>暂停/恢复时触发（参数：是否暂停中）</summary>
    public static event System.Action<bool> OnPauseToggled;

    /// <summary>WaveManager 所有波次完成时触发</summary>
    public static System.Action OnAllWavesComplete;

    /// <summary>PlayerHealth 玩家死亡时触发</summary>
    public static System.Action OnPlayerDied;

    // ── UI 引用（动态创建）──

    private GameObject pausePanel;
    private GameObject victoryPanel;
    private GameObject gameOverPanel;

    void Start()
    {
        OnAllWavesComplete = null;
        OnAllWavesComplete += ShowVictory;
        OnPlayerDied = null;
        OnPlayerDied += ShowGameOver;
        CreatePauseUI();
        CreateVictoryUI();
        CreateGameOverUI();
    }

    void Update()
    {
        // ESC 键检测
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            // 升级面板打开时或游戏已结束 → ESC 无效
            if (IsUpgradePaused || IsGameOver) return;

            TogglePause();
        }
    }

    // ── 暂停逻辑 ──

    void TogglePause()
    {
        if (IsManualPaused)
        {
            // 恢复
            Time.timeScale = 1f;
            IsManualPaused = false;
            if (pausePanel != null)
                pausePanel.SetActive(false);
            OnPauseToggled?.Invoke(false);
            Debug.Log("[GameStateManager] ▶ 游戏恢复");
        }
        else
        {
            // 暂停
            Time.timeScale = 0f;
            IsManualPaused = true;
            if (pausePanel != null)
                pausePanel.SetActive(true);
            OnPauseToggled?.Invoke(true);
            Debug.Log("[GameStateManager] ⏸ 游戏暂停");
        }
    }

    // ── 胜利逻辑 ──

    void ShowVictory()
    {
        IsGameOver = true;
        Time.timeScale = 0f;
        if (victoryPanel != null)
            victoryPanel.SetActive(true);
        Debug.Log("[GameStateManager] 🎉 胜利面板弹出！");
    }

    // ── 死亡逻辑 ──

    void ShowGameOver()
    {
        IsGameOver = true;
        Time.timeScale = 0f;
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
        Debug.Log("[GameStateManager] 💀 死亡面板弹出！");
    }

    // ── UI 创建 ──

    void CreatePauseUI()
    {
        Canvas canvas = GetCanvas();
        if (canvas == null) return;

        pausePanel = CreateOverlayPanel(canvas, "PausePanel");

        // 标题
        CreateTitle(pausePanel, "游 戏 暂 停", 36, Color.white, 80);

        // 继续按钮
        CreateButton(pausePanel, "继 续 游 戏", 20,
            new Color(0.15f, 0.35f, 0.15f, 1f),
            new Color(0.25f, 0.55f, 0.25f, 1f),
            () => TogglePause());

        // 重新开始按钮
        CreateButton(pausePanel, "重 新 开 始", -70,
            new Color(0.35f, 0.3f, 0.15f, 1f),
            new Color(0.55f, 0.45f, 0.2f, 1f),
            RestartGame);

        pausePanel.SetActive(false);
    }

    void CreateVictoryUI()
    {
        Canvas canvas = GetCanvas();
        if (canvas == null) return;

        victoryPanel = CreateOverlayPanel(canvas, "VictoryPanel");

        // 标题
        CreateTitle(victoryPanel, "🎉 恭 喜 通 关 ！🎉", 42, new Color(1f, 0.85f, 0.3f, 1f), 40);

        // 再来一局按钮
        CreateButton(victoryPanel, "再 来 一 局", -30,
            new Color(0.15f, 0.3f, 0.5f, 1f),
            new Color(0.25f, 0.5f, 0.7f, 1f),
            RestartGame);

        victoryPanel.SetActive(false);
    }

    void CreateGameOverUI()
    {
        Canvas canvas = GetCanvas();
        if (canvas == null) return;

        gameOverPanel = CreateOverlayPanel(canvas, "GameOverPanel");

        // 标题（红色，大号）
        CreateTitle(gameOverPanel, "你 已 阵 亡", 42, new Color(0.9f, 0.2f, 0.2f, 1f), 80);

        // 副标题
        CreateTitle(gameOverPanel, "胜败乃兵家常事", 22, new Color(0.7f, 0.5f, 0.5f, 1f), 20);

        // 重新开始按钮（暗红色）
        CreateButton(gameOverPanel, "重 新 开 始", -50,
            new Color(0.4f, 0.1f, 0.1f, 1f),
            new Color(0.6f, 0.2f, 0.2f, 1f),
            RestartGame);

        gameOverPanel.SetActive(false);
    }

    // ── UI 辅助方法 ──

    Canvas GetCanvas()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
            Debug.LogError("[GameStateManager] 找不到 Canvas！");
        return canvas;
    }

    GameObject CreateOverlayPanel(Canvas canvas, string name)
    {
        GameObject panelGo = new GameObject(name, typeof(RectTransform), typeof(Image));
        panelGo.transform.SetParent(canvas.transform, false);
        RectTransform rect = panelGo.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        panelGo.GetComponent<Image>().color = new Color(0, 0, 0, 0.85f);
        return panelGo;
    }

    void CreateTitle(GameObject parent, string text, int fontSize, Color color, float yOffset)
    {
        GameObject titleGo = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGo.transform.SetParent(parent.transform, false);
        RectTransform rect = titleGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0, yOffset);
        rect.sizeDelta = new Vector2(500, 70);

        TextMeshProUGUI tmp = titleGo.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.font = FontHelper.GetFont();
    }

    void CreateButton(GameObject parent, string label, float yOffset,
        Color normalColor, Color highlightColor, UnityEngine.Events.UnityAction callback)
    {
        GameObject btnGo = new GameObject("Btn_" + label.Replace(" ", ""),
            typeof(RectTransform), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(parent.transform, false);
        RectTransform rect = btnGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0, yOffset);
        rect.sizeDelta = new Vector2(350, 70);

        Image img = btnGo.GetComponent<Image>();
        img.color = normalColor;

        Button btn = btnGo.GetComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = highlightColor;
        btn.colors = colors;
        btn.onClick.AddListener(callback);

        // 按钮文字
        GameObject lblGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        lblGo.transform.SetParent(btnGo.transform, false);
        RectTransform lblRect = lblGo.GetComponent<RectTransform>();
        lblRect.anchorMin = Vector2.zero;
        lblRect.anchorMax = Vector2.one;
        lblRect.offsetMin = Vector2.zero;
        lblRect.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = lblGo.GetComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 26;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.font = FontHelper.GetFont();
    }

    // ── 重新开始 ──

    void RestartGame()
    {
        // 重置暂停状态
        IsManualPaused = false;
        IsUpgradePaused = false;
        IsGameOver = false;
        Time.timeScale = 1f;

        // 隐藏面板
        if (pausePanel != null) pausePanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        // 复用 PlayerHealth 的全局重置逻辑
        PlayerHealth playerHealth = Object.FindAnyObjectByType<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.RestartGame();
        }
        else
        {
            Debug.LogError("[GameStateManager] 找不到 PlayerHealth 来执行重置！");
        }
    }
}
