using UnityEngine;
using TMPro;

/// <summary>
/// 中文字体查找工具 —— 优先 MSYH/微软雅黑，fallback 到场景中已有的 TMP 字体
/// </summary>
public static class FontHelper
{
    private static TMP_FontAsset _cached;

    public static TMP_FontAsset GetFont()
    {
        if (_cached != null) return _cached;

        var allFonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        foreach (var f in allFonts)
        {
            if (f.name.Contains("MSYH") || f.name.Contains("YaHei") || f.name.Contains("Microsoft"))
            {
                _cached = f;
                return _cached;
            }
        }

        // Fallback: 复用场景中已有的 TMP 字体
        TextMeshProUGUI existing = Object.FindAnyObjectByType<TextMeshProUGUI>();
        _cached = existing != null ? existing.font : null;
        return _cached;
    }

    /// <summary>强制刷新缓存（切换场景或字体变化时调用）</summary>
    public static void InvalidateCache() => _cached = null;
}
