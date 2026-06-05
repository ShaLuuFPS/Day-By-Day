using UnityEngine;
using System.Collections;

public class TimedDestroyer : MonoBehaviour
{
    [Header("⏳ 销毁时间配置")]
    [Tooltip("物体存在总时间（秒）")]
    public float totalLifeTime = 10f;

    [Tooltip("最后开始闪烁预警的时间（秒）")]
    public float blinkDuration = 3f;

    [Header("🎨 闪烁频次配置")]
    [Tooltip("闪烁时亮灭的间隔时间（秒），数值越小闪得越快")]
    public float blinkInterval = 0.15f;

    private Renderer targetRenderer;
    private Coroutine lifeCoroutine;

    void Start()
    {
        // 自动获取当前物体（或子物体）的渲染器，用来控制显隐闪烁
        targetRenderer = GetComponentInChildren<Renderer>();

        // 开启全自动倒计时生命线
        ResetTimer();
    }

    /// <summary>
    /// 重置倒计时，用于武器交换等场景——旧物体需要重新开始 10 秒生命。
    /// </summary>
    public void ResetTimer()
    {
        if (lifeCoroutine != null) StopCoroutine(lifeCoroutine);
        if (targetRenderer != null) targetRenderer.enabled = true;
        lifeCoroutine = StartCoroutine(LifeTimeRoutine());
    }

    /// <summary>
    /// 核心生命周期协程：完美控制 稳定期 -> 闪烁期 -> 毁灭
    /// </summary>
    private IEnumerator LifeTimeRoutine()
    {
        // 1. 稳定期：先安安静静地躺在地上（总时间 减去 闪烁时间）
        float quietTime = totalLifeTime - blinkDuration;
        if (quietTime > 0)
        {
            yield return new WaitForSeconds(quietTime);
        }

        // 2. 闪烁期：最后的疯狂倒计时
        float elapsedBlinkTime = 0f;
        while (elapsedBlinkTime < blinkDuration)
        {
            if (targetRenderer != null)
            {
                // 通过让渲染器在 true 和 false 之间切换，实现“隐身/显现”的闪烁
                targetRenderer.enabled = !targetRenderer.enabled;
            }

            // 等待设定的间隔时间，再走下一次循环
            yield return new WaitForSeconds(blinkInterval);
            elapsedBlinkTime += blinkInterval;
        }

        // 🌟 绝妙细节：安全确保即使销毁前一瞬间是隐藏的，也不影响物体的物理销毁
        Destroy(gameObject);
    }
}