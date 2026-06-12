using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 纯状态机 —— 管理暂停/胜利/死亡状态。
/// UI 面板由 GameStateUI 适配器订阅事件后自行响应。
/// </summary>
public class GameStateManager : MonoBehaviour
{
    // ── 场景重置 ──
    [Header("重置编排")]
    [SerializeField] private GameObject spawnerManager;

    // ── 静态状态 ──

    /// <summary>升级面板打开时的暂停（LevelUpManager 设置）</summary>
    public static bool IsUpgradePaused { get; set; } = false;

    /// <summary>ESC 手动暂停</summary>
    public static bool IsManualPaused { get; private set; } = false;

    /// <summary>统一暂停检查 —— Player 脚本通过此属性冻结输入</summary>
    public static bool IsPaused => IsUpgradePaused || IsManualPaused;

    /// <summary>游戏结束（死亡或胜利），禁止一切暂停操作</summary>
    public static bool IsGameOver { get; private set; } = false;

    /// <summary>
    /// 玩家输入冻结检查 —— 所有 Player 脚本的唯一入口。
    /// 新增需要冻结输入的全局状态时，只需在这里加一个 || 条件。
    /// </summary>
    public static bool IsInputFrozen => IsUpgradePaused || IsManualPaused || IsGameOver;

    // ── 事件 ──

    public static event System.Action<bool> OnPauseToggled;
    public static System.Action OnAllWavesComplete;
    public static System.Action OnPlayerDied;
    /// <summary>重新开始游戏时触发，UI 适配器监听此事件隐藏所有面板</summary>
    public static System.Action OnRestartGame;

    void Start()
    {
        // 用 -= 再 += 避免重复订阅，同时不破坏其他订阅者（如 GameStateUI）
        OnAllWavesComplete -= ShowVictory;
        OnAllWavesComplete += ShowVictory;
        OnPlayerDied -= ShowGameOver;
        OnPlayerDied += ShowGameOver;
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

    /// <summary>切换暂停状态（由 GameStateUI 按钮或 ESC 键调用）</summary>
    public void TogglePause()
    {
        if (IsManualPaused)
        {
            Time.timeScale = 1f;
            IsManualPaused = false;
            OnPauseToggled?.Invoke(false);
            Debug.Log("[GameStateManager] ▶ 游戏恢复");
        }
        else
        {
            Time.timeScale = 0f;
            IsManualPaused = true;
            OnPauseToggled?.Invoke(true);
            Debug.Log("[GameStateManager] ⏸ 游戏暂停");
        }
    }

    void ShowVictory()
    {
        IsGameOver = true;
        Time.timeScale = 0f;
        Debug.Log("[GameStateManager] 🎉 胜利");
    }

    void ShowGameOver()
    {
        IsGameOver = true;
        Time.timeScale = 0f;
        Debug.Log("[GameStateManager] 💀 死亡");
    }

    // ── 重新开始 ──

    /// <summary>重新开始游戏（由 GameStateUI 按钮调用）</summary>
    public void RestartGame()
    {
        Debug.Log("[GameStateManager] 【开始有序重置现场...】");

        // 1. 重置静态状态
        IsManualPaused = false;
        IsUpgradePaused = false;
        IsGameOver = false;
        Time.timeScale = 1f;

        // 2. 通知 UI 隐藏所有面板
        OnRestartGame?.Invoke();

        // 3. 有序场景清理：先销毁敌人（防止清理过程中还在生成）
        if (spawnerManager != null)
        {
            foreach (Transform child in spawnerManager.transform)
            {
                Destroy(child.gameObject);
            }
        }

        // 4. 销毁所有掉落物
        LootDrop[] drops = FindObjectsByType<LootDrop>(FindObjectsInactive.Include);
        foreach (LootDrop drop in drops)
            Destroy(drop.gameObject);

        // 5. 有序广播 IResettable：各模块按 System.Object 自然顺序自理
        MonoBehaviour[] allScripts = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include);
        foreach (MonoBehaviour mono in allScripts)
        {
            if (mono is IResettable resettableTarget)
            {
                resettableTarget.ResetData();
            }
        }

        Debug.Log("[GameStateManager] 【重置完毕！】新一轮战斗开始。");
    }
}
