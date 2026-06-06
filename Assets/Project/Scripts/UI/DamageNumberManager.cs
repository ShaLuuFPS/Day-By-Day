using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 伤害数字管理器：屏幕空间 Canvas UI。
/// 同一目标在合并窗口内的多次伤害自动合并为一个数字（解决霰弹枪多弹丸重叠问题）。
/// 挂到 HUD Canvas 上，拖入字体即可。
/// </summary>
public class DamageNumberManager : MonoBehaviour
{
    public static DamageNumberManager Instance { get; private set; }

    [Header("必须连线")]
    [Tooltip("屏幕空间 Canvas（留空则自动取所在 GameObject 上的 Canvas）")]
    public Canvas canvas;
    [Tooltip("主摄像机（留空则用 Camera.main）")]
    public Camera mainCamera;
    [Tooltip("TMP 字体资产")]
    public TMP_FontAsset fontAsset;

    [Header("视觉效果")]
    [Tooltip("字体大小")]
    public float fontSize = 28f;
    [Tooltip("数字显示时长（秒）")]
    public float duration = 0.5f;
    [Tooltip("数字向上飘的屏幕像素")]
    public float floatPixels = 60f;

    [Header("合并")]
    [Tooltip("同一目标在此时间窗口（秒）内的伤害会合并显示")]
    public float mergeWindow = 0.08f;

    private RectTransform _canvasRect;

    /// <summary>
    /// 每个目标当前活跃的伤害数字条目
    /// </summary>
    private class ActiveEntry
    {
        public Transform target;
        public GameObject gameObject;
        public TextMeshProUGUI text;
        public RectTransform rect;
        public float totalDamage;
        public float lastHitTime;
        public Coroutine coroutine;
    }

    private Dictionary<Transform, ActiveEntry> _entries = new Dictionary<Transform, ActiveEntry>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (canvas == null)
            canvas = GetComponent<Canvas>();
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (canvas != null)
            _canvasRect = canvas.GetComponent<RectTransform>();
    }

    /// <summary>
    /// 在世界坐标目标头顶生成伤害数字。
    /// </summary>
    /// <param name="target">受伤的目标 Transform（用于同目标合并）</param>
    /// <param name="worldPos">目标世界坐标</param>
    /// <param name="damage">伤害值</param>
    /// <param name="color">数字颜色</param>
    public static void Spawn(Transform target, Vector3 worldPos, float damage, Color color)
    {
        if (Instance == null)
        {
            Debug.LogWarning("[DamageNumberManager] 场景中没有 DamageNumberManager 实例。");
            return;
        }
        Instance.SpawnInternal(target, worldPos, damage, color);
    }

    void SpawnInternal(Transform target, Vector3 worldPos, float damage, Color color)
    {
        if (canvas == null || mainCamera == null || fontAsset == null)
        {
            Debug.LogWarning("[DamageNumberManager] Canvas / Camera / Font 未配置完整。");
            return;
        }

        // ── 同目标合并检测 ──
        if (target != null && _entries.TryGetValue(target, out ActiveEntry entry))
        {
            if (Time.time - entry.lastHitTime <= mergeWindow)
            {
                // 还在合并窗口内 → 累加伤害，刷新显示和计时
                entry.totalDamage += damage;
                entry.lastHitTime = Time.time;
                entry.text.text = $"-{Mathf.RoundToInt(entry.totalDamage)}";

                // 重新开始渐隐动画
                if (entry.coroutine != null)
                    StopCoroutine(entry.coroutine);
                entry.coroutine = StartCoroutine(AnimateNumber(entry, true));
                return;
            }
            else
            {
                // 窗口外 → 清理旧条目（旧数字已飞走/淡出）
                if (entry.coroutine != null)
                    StopCoroutine(entry.coroutine);
                if (entry.gameObject != null)
                    Destroy(entry.gameObject);
                _entries.Remove(target);
            }
        }

        // ── 创建新数字 ──
        Vector3 headPos = worldPos + Vector3.up * 1.5f;
        Vector3 screenPoint = mainCamera.WorldToScreenPoint(headPos);
        if (screenPoint.z < 0f) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRect, screenPoint, canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : mainCamera,
            out Vector2 localPoint);

        localPoint.x += UnityEngine.Random.Range(-8f, 8f);

        GameObject obj = new GameObject("DamageNumber", typeof(RectTransform));
        obj.transform.SetParent(canvas.transform, false);
        obj.layer = canvas.gameObject.layer;

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchoredPosition = localPoint;

        TextMeshProUGUI text = obj.AddComponent<TextMeshProUGUI>();
        text.text = $"-{Mathf.RoundToInt(damage)}";
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.font = fontAsset;
        text.raycastTarget = false;

        ActiveEntry newEntry = new ActiveEntry
        {
            target = target,
            gameObject = obj,
            text = text,
            rect = rect,
            totalDamage = damage,
            lastHitTime = Time.time,
        };
        newEntry.coroutine = StartCoroutine(AnimateNumber(newEntry, false));

        if (target != null)
            _entries[target] = newEntry;
    }

    IEnumerator AnimateNumber(ActiveEntry entry, bool isRefresh)
    {
        // 刷新时从当前位置继续，不重新设置起点
        if (!isRefresh)
        {
            // 等待合并窗口过去后再开始计时（防止霰弹枪后续弹丸重置动画）
            yield return new WaitForSeconds(mergeWindow);
        }

        float elapsed = 0f;
        Vector2 startPos = entry.rect.anchoredPosition;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            entry.rect.anchoredPosition = startPos + Vector2.up * t * floatPixels;

            Color c = entry.text.color;
            c.a = 1f - t;
            entry.text.color = c;

            yield return null;
        }

        // 清理
        if (entry.target != null)
            _entries.Remove(entry.target);
        Destroy(entry.gameObject);
    }
}
