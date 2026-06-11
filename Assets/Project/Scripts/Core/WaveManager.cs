using UnityEngine;
using System.Collections;

/// <summary>
/// 波次管理器 —— 控制波次循环：生成 → 清场 → 休息 → 下一波
/// 实现 IResettable，游戏重启时自动重置
/// </summary>
public class WaveManager : MonoBehaviour, IResettable
{
    [Header("配置")]
    [Tooltip("波次配置资产")]
    public WaveConfig waveConfig;

    [Tooltip("场景中的 EnemySpawner（自动查找同 GameObject 或手动拖入）")]
    public EnemySpawner enemySpawner;

    [Header("状态（只读）")]
    [SerializeField] private int currentWaveIndex = 0;
    [SerializeField] private int spawnedCount = 0;
    [SerializeField] private int aliveCount = 0;
    [SerializeField] private WaveState state = WaveState.Idle;

    private Coroutine _waveRoutine;

    private enum WaveState
    {
        Idle,            // 等待初始延迟
        Spawning,        // 正在逐只生成
        WaitingForClear, // 全部生成完毕，等待清场
        WaveRest,        // 波间休息
        AllComplete      // 所有波次完成
    }

    void Start()
    {
        if (enemySpawner == null)
            enemySpawner = GetComponent<EnemySpawner>();

        // 初始启动波次系统
        StartGame();
    }

    void OnEnable()  => EnemyHealth.OnAnyEnemyDied += OnEnemyDied;
    void OnDisable() => EnemyHealth.OnAnyEnemyDied -= OnEnemyDied;

    void OnEnemyDied(EnemyHealth enemy)
    {
        // 只在生成中或等待清场时追踪存活数
        if (state == WaveState.Spawning || state == WaveState.WaitingForClear)
        {
            aliveCount = Mathf.Max(0, aliveCount - 1);
            Debug.Log($"[WaveManager] 敌人死亡，存活: {aliveCount}/{spawnedCount}");
        }
    }

    void Update()
    {
        // 轮询清场条件
        if (state == WaveState.WaitingForClear && aliveCount <= 0)
        {
            OnWaveCleared();
        }
    }

    /// <summary>
    /// 开始游戏（或重新开始）
    /// </summary>
    void StartGame()
    {
        if (_waveRoutine != null)
        {
            StopCoroutine(_waveRoutine);
            _waveRoutine = null;
        }

        currentWaveIndex = 0;
        spawnedCount = 0;
        aliveCount = 0;

        if (waveConfig == null || waveConfig.waves == null || waveConfig.waves.Length == 0)
        {
            Debug.LogError("[WaveManager] WaveConfig 为空或没有定义波次！");
            state = WaveState.AllComplete;
            return;
        }

        _waveRoutine = StartCoroutine(InitialDelayRoutine());
    }

    IEnumerator InitialDelayRoutine()
    {
        state = WaveState.Idle;
        float delay = waveConfig.initialDelay;
        Debug.Log($"[WaveManager] ⏳ {delay} 秒后开始第一波...");
        yield return new WaitForSeconds(delay);
        StartWave();
    }

    /// <summary>
    /// 开始当前波次
    /// </summary>
    void StartWave()
    {
        if (waveConfig == null || currentWaveIndex >= waveConfig.waves.Length)
        {
            state = WaveState.AllComplete;
            Debug.Log("[WaveManager] 🎉 所有波次完成！");
            GameStateManager.OnAllWavesComplete?.Invoke();
            return;
        }

        WaveEntry wave = waveConfig.waves[currentWaveIndex];

        spawnedCount = 0;
        aliveCount = 0;

        Debug.Log($"[WaveManager] 🌊 第 {currentWaveIndex + 1}/{waveConfig.waves.Length} 波开始！" +
                  $"总数={wave.totalEnemyCount} 上限={wave.maxAliveCount} 间隔={wave.spawnInterval}s");

        _waveRoutine = StartCoroutine(SpawnRoutine(wave));
    }

    IEnumerator SpawnRoutine(WaveEntry wave)
    {
        state = WaveState.Spawning;

        while (spawnedCount < wave.totalEnemyCount)
        {
            // 存活数达上限时等待
            while (aliveCount >= wave.maxAliveCount)
                yield return null;

            if (enemySpawner != null)
            {
                enemySpawner.SpawnOneEnemy(wave.spawnEntries);
                spawnedCount++;
                aliveCount++;
            }
            else
            {
                Debug.LogError("[WaveManager] EnemySpawner 引用为空！");
                yield break;
            }

            // 还没生成完则等待间隔
            if (spawnedCount < wave.totalEnemyCount)
                yield return new WaitForSeconds(wave.spawnInterval);
        }

        state = WaveState.WaitingForClear;
        Debug.Log($"[WaveManager] 第 {currentWaveIndex + 1} 波全部生成完毕（{spawnedCount} 只），等待清场...");
    }

    void OnWaveCleared()
    {
        Debug.Log($"[WaveManager] ✅ 第 {currentWaveIndex + 1} 波清场！");

        currentWaveIndex++;

        if (currentWaveIndex >= waveConfig.waves.Length)
        {
            state = WaveState.AllComplete;
            Debug.Log("[WaveManager] 🎉 所有波次完成！恭喜！");
            GameStateManager.OnAllWavesComplete?.Invoke();
            return;
        }

        // 波间休息
        _waveRoutine = StartCoroutine(WaveRestRoutine());
    }

    IEnumerator WaveRestRoutine()
    {
        state = WaveState.WaveRest;
        float rest = waveConfig.waveRestInterval;
        Debug.Log($"[WaveManager] ⏸ 波间休息 {rest} 秒...");
        yield return new WaitForSeconds(rest);
        StartWave();
    }

    /// <summary>
    /// IResettable 实现 —— 游戏重启时自动调用
    /// </summary>
    public void ResetData()
    {
        if (_waveRoutine != null)
        {
            StopCoroutine(_waveRoutine);
            _waveRoutine = null;
        }

        currentWaveIndex = 0;
        spawnedCount = 0;
        aliveCount = 0;
        state = WaveState.Idle;

        // 重新开始波次循环
        if (waveConfig != null && waveConfig.waves != null && waveConfig.waves.Length > 0)
        {
            _waveRoutine = StartCoroutine(InitialDelayRoutine());
        }

        Debug.Log("[WaveManager] 🔄 波次系统已重置");
    }
}
