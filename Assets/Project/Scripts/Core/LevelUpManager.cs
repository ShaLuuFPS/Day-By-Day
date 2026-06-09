using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 升级选择管理器 —— 经验达标时暂停游戏，弹出 3 选 1 面板
/// </summary>
public class LevelUpManager : MonoBehaviour, IResettable
{
    [Header("升级池")]
    [Tooltip("所有可用升级（Phase 4 实现具体效果）")]
    public UpgradeData[] upgradePool;

    [Header("配置")]
    [Tooltip("每次升级显示几个选项")]
    public int choicesPerLevel = 3;

    [Header("面板引用（留空则自动创建）")]
    public GameObject levelUpPanel;
    public TextMeshProUGUI titleText;
    public Button[] choiceButtons;
    public TextMeshProUGUI[] choiceLabels;

    private List<UpgradeData> chosenUpgrades = new List<UpgradeData>();

    /// <summary>已废弃，改用 GameStateManager.IsUpgradePaused</summary>
    [System.Obsolete("Use GameStateManager.IsUpgradePaused instead")]
    public static bool IsPaused => GameStateManager.IsUpgradePaused;

    /// <summary>玩家选择升级后触发</summary>
    public static event System.Action<UpgradeData> OnUpgradeChosen;

    void OnEnable()
    {
        PlayerXP.OnLevelUp += OnPlayerLevelUp;
    }

    void OnDisable()
    {
        PlayerXP.OnLevelUp -= OnPlayerLevelUp;
    }

    void Start()
    {
        if (levelUpPanel == null)
            CreateLevelUpUI();

        if (levelUpPanel != null)
            levelUpPanel.SetActive(false);
    }

    void OnPlayerLevelUp(int newLevel)
    {
        if (upgradePool == null || upgradePool.Length == 0)
        {
            Debug.LogWarning("[LevelUpManager] 升级池为空，跳过升级选择");
            return;
        }

        // 检查是否所有升级都已选过
        if (chosenUpgrades.Count >= upgradePool.Length)
        {
            Debug.Log("[LevelUpManager] 所有升级已选完，跳过");
            return;
        }

        // 暂停游戏 + 冻结玩家输入
        Time.timeScale = 0f;
        GameStateManager.IsUpgradePaused = true;
        ShowChoices();
    }

    void ShowChoices()
    {
        if (levelUpPanel == null) return;

        // 随机抽 N 个不重复升级
        UpgradeData[] choices = PickRandomUpgrades(choicesPerLevel);
        if (choices == null || choices.Length == 0) return;

        // 填充按钮
        for (int i = 0; i < choiceButtons.Length && i < choices.Length; i++)
        {
            if (choiceButtons[i] != null)
            {
                int index = i; // 闭包捕获
                choiceButtons[i].onClick.RemoveAllListeners();
                choiceButtons[i].onClick.AddListener(() => OnChoiceSelected(choices[index]));
                choiceButtons[i].gameObject.SetActive(true);
            }
            if (choiceLabels != null && i < choiceLabels.Length && choiceLabels[i] != null)
            {
                var c = choices[i];
                choiceLabels[i].text = $"<b>{c.upgradeName}</b>\n<size=18>{c.description}</size>";
            }
        }

        // 隐藏多余的按钮
        for (int i = choices.Length; i < choiceButtons.Length; i++)
        {
            if (choiceButtons[i] != null)
                choiceButtons[i].gameObject.SetActive(false);
        }

        if (titleText != null)
            titleText.text = "选择升级";

        levelUpPanel.SetActive(true);
    }

    void OnChoiceSelected(UpgradeData selected)
    {
        Debug.Log($"[LevelUpManager] 玩家选择了: {selected.upgradeName} ({selected.upgradeType})");

        chosenUpgrades.Add(selected);
        OnUpgradeChosen?.Invoke(selected);

        // 恢复游戏
        if (levelUpPanel != null)
            levelUpPanel.SetActive(false);
        Time.timeScale = 1f;
        GameStateManager.IsUpgradePaused = false;
    }

    UpgradeData[] PickRandomUpgrades(int count)
    {
        if (upgradePool.Length == 0) return null;

        // 过滤掉已选择过的升级（不重复出现）
        var available = new List<UpgradeData>();
        foreach (var u in upgradePool)
        {
            if (!chosenUpgrades.Contains(u))
                available.Add(u);
        }

        if (available.Count == 0)
        {
            Debug.Log("[LevelUpManager] 所有升级已选完，不再弹出选择面板");
            return null;
        }

        int actualCount = Mathf.Min(count, available.Count);

        // Fisher-Yates 部分洗牌
        for (int i = 0; i < actualCount; i++)
        {
            int randomIndex = Random.Range(i, available.Count);
            var temp = available[i];
            available[i] = available[randomIndex];
            available[randomIndex] = temp;
        }

        UpgradeData[] result = new UpgradeData[actualCount];
        for (int i = 0; i < actualCount; i++)
            result[i] = available[i];
        return result;
    }

    void CreateLevelUpUI()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[LevelUpManager] 找不到 Canvas！");
                return;
            }
        }

        // 全屏遮罩面板
        GameObject panelGo = new GameObject("LevelUpPanel", typeof(RectTransform), typeof(Image));
        panelGo.transform.SetParent(canvas.transform, false);
        RectTransform panelRect = panelGo.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        panelGo.GetComponent<Image>().color = new Color(0, 0, 0, 0.85f);
        levelUpPanel = panelGo;

        // 标题
        GameObject titleGo = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGo.transform.SetParent(panelGo.transform, false);
        RectTransform titleRect = titleGo.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.anchoredPosition = new Vector2(0, 180);
        titleRect.sizeDelta = new Vector2(400, 60);
        titleText = titleGo.GetComponent<TextMeshProUGUI>();
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontSize = 36;
        titleText.color = Color.white;
        titleText.font = FontHelper.GetFont();

        // 3 个选择按钮（竖排）
        choiceButtons = new Button[3];
        choiceLabels = new TextMeshProUGUI[3];

        for (int i = 0; i < 3; i++)
        {
            GameObject btnGo = new GameObject($"Choice_{i}", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(panelGo.transform, false);
            RectTransform btnRect = btnGo.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, 0.5f);
            btnRect.anchorMax = new Vector2(0.5f, 0.5f);
            btnRect.anchoredPosition = new Vector2(0, 80 - i * 90);
            btnRect.sizeDelta = new Vector2(350, 75);

            var btnImg = btnGo.GetComponent<Image>();
            btnImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            choiceButtons[i] = btnGo.GetComponent<Button>();
            var btnColors = choiceButtons[i].colors;
            btnColors.highlightedColor = new Color(0.3f, 0.5f, 0.3f, 1f);
            choiceButtons[i].colors = btnColors;

            // 文字标签
            GameObject lblGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            lblGo.transform.SetParent(btnGo.transform, false);
            RectTransform lblRect = lblGo.GetComponent<RectTransform>();
            lblRect.anchorMin = Vector2.zero;
            lblRect.anchorMax = Vector2.one;
            lblRect.offsetMin = Vector2.zero;
            lblRect.offsetMax = Vector2.zero;

            choiceLabels[i] = lblGo.GetComponent<TextMeshProUGUI>();
            choiceLabels[i].alignment = TextAlignmentOptions.Center;
            choiceLabels[i].fontSize = 20;
            choiceLabels[i].color = Color.white;
            choiceLabels[i].font = FontHelper.GetFont();
        }
    }

    public void ResetData()
    {
        chosenUpgrades.Clear();
        GameStateManager.IsUpgradePaused = false;
        if (levelUpPanel != null)
            levelUpPanel.SetActive(false);
        Time.timeScale = 1f;
        Debug.Log("[LevelUpManager] 🔄 升级系统已重置");
    }
}
