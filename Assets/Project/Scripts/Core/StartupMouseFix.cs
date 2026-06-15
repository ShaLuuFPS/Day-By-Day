using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 打包后启动时确保 Input System 鼠标位置有效。
/// 编辑器下跳过（编辑器 Game View 不受 exclusive fullscreen 影响）。
/// </summary>
public class StartupMouseFix : MonoBehaviour
{
    void Awake()
    {
#if !UNITY_EDITOR
        // 短暂解锁光标强制 Input System 刷新鼠标坐标
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 如果鼠标位置异常（全 0），Warp 到屏幕中央
        Vector2 mousePos = Mouse.current?.position.ReadValue() ?? Vector2.zero;
        if (mousePos == Vector2.zero)
        {
            Vector2 center = new Vector2(Screen.width / 2f, Screen.height / 2f);
            Mouse.current?.WarpCursorPosition(center);
            Debug.Log($"[StartupMouseFix] 鼠标位置异常 (0,0)，已 Warp 到屏幕中央 {center}");
        }
#endif
    }
}
