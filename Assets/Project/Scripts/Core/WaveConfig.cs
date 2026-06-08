using UnityEngine;

/// <summary>
/// 单波定义：敌人数、存活上限、生成间隔、类型权重
/// </summary>
[System.Serializable]
public class WaveEntry
{
    [Tooltip("本波总敌人数")]
    public int totalEnemyCount = 5;

    [Tooltip("同时存活上限（防止一波刷太多卡顿）")]
    public int maxAliveCount = 3;

    [Tooltip("生成间隔（秒）")]
    public float spawnInterval = 2f;

    [Tooltip("本波敌人类型权重（复用 EnemySpawner 的生成条目结构）")]
    public ZombieSpawnEntry[] spawnEntries;
}

/// <summary>
/// 波次配置 ScriptableObject —— 定义整场游戏的所有波次
/// </summary>
[CreateAssetMenu(fileName = "NewWaveConfig", menuName = "FPS/Wave Config")]
public class WaveConfig : ScriptableObject
{
    [Tooltip("波次列表（按顺序执行）")]
    public WaveEntry[] waves;

    [Tooltip("波间休息时间（秒）")]
    public float waveRestInterval = 3f;

    [Tooltip("游戏开始后到第一波的延迟（秒）")]
    public float initialDelay = 2f;
}
