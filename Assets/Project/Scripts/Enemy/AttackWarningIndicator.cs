using UnityEngine;
using System.Collections;

/// <summary>
/// 通用攻击预警指示器：在敌人脚下显示攻击范围圆环，渐变后自毁。
/// 通过 AttackWarningIndicator.Create() 工厂方法创建。
/// </summary>
public class AttackWarningIndicator : MonoBehaviour
{
    private float _duration;
    private float _elapsed;
    private Material _mat;
    private Renderer _rend;

    /// <summary>
    /// 创建攻击预警指示器（工厂方法）。
    /// </summary>
    /// <param name="worldPos">世界坐标（敌人位置，会调整到地面）</param>
    /// <param name="radius">攻击范围半径</param>
    /// <param name="duration">预警持续时间</param>
    /// <param name="prefab">可选预制体，为空则代码生成 Cylinder 圆盘</param>
    /// <returns>指示器 GameObject（调用方持有引用以提前 Destroy）</returns>
    public static GameObject Create(Vector3 worldPos, float radius, float duration, GameObject prefab = null)
    {
        GameObject indicator;

        if (prefab != null)
        {
            indicator = Instantiate(prefab, worldPos, Quaternion.identity);
            AttackWarningIndicator comp = indicator.GetComponent<AttackWarningIndicator>();
            if (comp != null)
                comp.Init(radius, duration);
            else
                Debug.LogWarning("[AttackWarningIndicator] 预制体上未找到 AttackWarningIndicator 组件，视觉可能异常。");
        }
        else
        {
            indicator = CreateFallback(worldPos, radius, duration);
        }

        return indicator;
    }

    /// <summary>
    /// 代码兜底：创建压扁的 Cylinder 作为地面圆盘。
    /// </summary>
    static GameObject CreateFallback(Vector3 worldPos, float radius, float duration)
    {
        GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disc.name = "AttackWarning_Disc";

        // 置于地面（Cylinder 默认高度 2，中心在原点 → 下移 1 单位贴地）
        disc.transform.position = worldPos;

        // 压扁成圆盘：XZ = 直径, Y = 极薄
        disc.transform.localScale = new Vector3(radius * 2f, 0.02f, radius * 2f);

        // 纯视觉，立即禁用碰撞体再延迟销毁（防止同帧阻挡）
        Collider col = disc.GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
            Destroy(col);
        }

        // ---- 材质：半透明红色 ----
        // 优先 URP Unlit，回退到内置 Unlit/Color
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");

        Material mat = new Material(shader);
        mat.color = new Color(1f, 0.15f, 0.05f, 0.4f);

        // 尝试启用透明度（URP 路径）
        if (shader.name.Contains("Universal"))
        {
            mat.SetFloat("_Surface", 1f);                      // Transparent
            mat.SetFloat("_Blend", 0f);                        // Alpha
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = 3000;
        }

        disc.GetComponent<Renderer>().material = mat;

        AttackWarningIndicator comp = disc.AddComponent<AttackWarningIndicator>();
        comp.Init(radius, duration);

        return disc;
    }

    /// <summary>
    /// 初始化（在 Instantiate 后调用，设置半径和持续时间并启动动画）。
    /// </summary>
    public void Init(float radius, float duration)
    {
        // 确保预制体上也不会残留碰撞体阻挡僵尸
        foreach (Collider c in GetComponentsInChildren<Collider>())
            c.enabled = false;

        _duration = duration;
        _rend = GetComponent<Renderer>();
        if (_rend != null)
            _mat = _rend.material;

        StartCoroutine(AnimateRoutine());
    }

    IEnumerator AnimateRoutine()
    {
        float startAlpha = (_mat != null) ? _mat.color.a : 0.3f;

        while (_elapsed < _duration)
        {
            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _duration);

            // alpha: 0.2 * startAlpha → startAlpha（渐显）
            if (_mat != null)
            {
                Color c = _mat.color;
                c.a = Mathf.Lerp(0.2f * startAlpha, startAlpha, t);
                _mat.color = c;
            }

            yield return null;
        }

        // 动画完成 → 自毁
        CleanupAndDestroy();
    }

    void CleanupAndDestroy()
    {
        if (_mat != null && _mat.name.EndsWith("(Instance)"))
            Destroy(_mat);
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        // 防止材质泄漏（协程被提前终止时兜底）
        if (_mat != null && _mat.name.EndsWith("(Instance)"))
            Destroy(_mat);
    }
}
