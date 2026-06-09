using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

/// <summary>
/// 游戏状态管理器 —— 集中管理暂停/胜利/游戏结束状态。
/// UI 使用锚点百分比布局，配合 Canvas Scaler 自适应分辨率。
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

    public static event System.Action<bool> OnPauseToggled;
    public static System.Action OnAllWavesComplete;
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
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (IsUpgradePaused || IsGameOver) return;
            TogglePause();
        }
    }

    // ── 暂停逻辑 ──

    void TogglePause()
    {
        if (IsManualPaused)
        {
            Time.timeScale = 1f;
            IsManualPaused = false;
            if (pausePanel != null) pausePanel.SetActive(false);
            OnPauseToggled?.Invoke(false);
            Debug.Log("[GameStateManager] ▶ 游戏恢复");
        }
        else
        {
            Time.timeScale = 0f;
            IsManualPaused = true;
            if (pausePanel != null) pausePanel.SetActive(true);
            OnPauseToggled?.Invoke(true);
            Debug.Log("[GameStateManager] ⏸ 游戏暂停");
        }
    }

    void ShowVictory()
    {
        IsGameOver = true;
        Time.timeScale = 0f;
        if (victoryPanel != null) victoryPanel.SetActive(true);
        Debug.Log("[GameStateManager] 🎉 胜利面板弹出！");
    }

    void ShowGameOver()
    {
        IsGameOver = true;
        Time.timeScale = 0f;
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        Debug.Log("[GameStateManager] 💀 死亡面板弹出！");
    }

    // ── UI 创建 ──

    void CreatePauseUI()
    {
        Canvas canvas = GetCanvas();
        if (canvas == null) return;

        pausePanel = CreateOverlayPanel(canvas, "PausePanel");

        // 标题（锚点在面板上半部）
        CreateTitle(pausePanel, "游 戏 暂 停", 36, Color.white,
            new Vector2(0.1f, 0.55f), new Vector2(0.9f, 0.7f));

        // 按钮容器（VerticalLayoutGroup 自动排列）
        GameObject btnGroup = CreateButtonGroup(pausePanel,
            new Vector2(0.25f, 0.30f), new Vector2(0.75f, 0.52f));

        CreateButton(btnGroup, "继 续 游 戏",
            new Color(0.15f, 0.35f, 0.15f, 1f),
            new Color(0.25f, 0.55f, 0.25f, 1f),
            () => TogglePause());

        CreateButton(btnGroup, "重 新 开 始",
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

        CreateTitle(victoryPanel, "🎉 恭 喜 通 关 ！🎉", 42,
            new Color(1f, 0.85f, 0.3f, 1f),
            new Vector2(0.05f, 0.50f), new Vector2(0.95f, 0.68f));

        GameObject btnGroup = CreateButtonGroup(victoryPanel,
            new Vector2(0.25f, 0.35f), new Vector2(0.75f, 0.48f));

        CreateButton(btnGroup, "再 来 一 局",
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

        // 主标题（红色）
        CreateTitle(gameOverPanel, "你 已 阵 亡", 42,
            new Color(0.9f, 0.2f, 0.2f, 1f),
            new Vector2(0.1f, 0.55f), new Vector2(0.9f, 0.70f));

        // 副标题
        CreateTitle(gameOverPanel, "胜败乃兵家常事", 22,
            new Color(0.7f, 0.5f, 0.5f, 1f),
            new Vector2(0.15f, 0.43f), new Vector2(0.85f, 0.53f));

        GameObject btnGroup = CreateButtonGroup(gameOverPanel,
            new Vector2(0.25f, 0.22f), new Vector2(0.75f, 0.38f));

        CreateButton(btnGroup, "重 新 开 始",
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

    /// <summary>全屏遮罩面板（锚点撑满父容器）</summary>
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

    /// <summary>标题文字（锚点区域定位，不写死像素）</summary>
    void CreateTitle(GameObject parent, string text, int fontSize, Color color,
        Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject titleGo = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGo.transform.SetParent(parent.transform, false);
        RectTransform rect = titleGo.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;

        TextMeshProUGUI tmp = titleGo.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.font = FontHelper.GetFont();
    }

    /// <summary>按钮组容器（VerticalLayoutGroup 自动排列）</summary>
    GameObject CreateButtonGroup(GameObject parent, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject groupGo = new GameObject("ButtonGroup", typeof(RectTransform),
            typeof(VerticalLayoutGroup));
        groupGo.transform.SetParent(parent.transform, false);
        RectTransform rect = groupGo.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;

        VerticalLayoutGroup vlg = groupGo.GetComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childForceExpandWidth = false;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.spacing = 10;

        return groupGo;
    }

    /// <summary>按钮（加入 LayoutGroup 自动排列，高度按父容器比例）</summary>
    void CreateButton(GameObject parent, string label,
        Color normalColor, Color highlightColor,
        UnityEngine.Events.UnityAction callback)
    {
        GameObject btnGo = new GameObject("Btn_" + label.Replace(" ", ""),
            typeof(RectTransform), typeof(Image), typeof(Button),
            typeof(LayoutElement));
        btnGo.transform.SetParent(parent.transform, false);

        // LayoutElement 控制按钮最小高度
        LayoutElement le = btnGo.GetComponent<LayoutElement>();
        le.minHeight = 50;
        le.flexibleWidth = 1;

        Image img = btnGo.GetComponent<Image>();
        img.color = normalColor;

        Button btn = btnGo.GetComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = highlightColor;
        btn.colors = colors;
        btn.onClick.AddListener(callback);

        // 按钮文字（撑满按钮）
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
        IsManualPaused = false;
        IsUpgradePaused = false;
        IsGameOver = false;
        Time.timeScale = 1f;

        if (pausePanel != null) pausePanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

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
