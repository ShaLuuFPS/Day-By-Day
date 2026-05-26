using UnityEngine;
using UnityEngine.InputSystem; // 如果你上一步用了新输入系统，保留这行

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    private Vector3 moveDirection;

    void Update()
    {
        // 1. 移动逻辑（保持不变）
        float moveX = 0f;
        float moveZ = 0f;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) moveZ = 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) moveZ = -1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) moveX = -1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) moveX = 1f;
        }

        moveDirection = new Vector3(moveX, 0f, moveZ).normalized;
        transform.Translate(moveDirection * moveSpeed * Time.deltaTime, Space.World);

        // 2. 转向逻辑：让角色面朝鼠标指向的点
        RotateToMouse();
    }

    void RotateToMouse()
    {
        // 1. 安全检查：确保鼠标和键盘对象存在，否则不执行，防止报错
        if (Mouse.current == null || Camera.main == null) return;

        // 2. 获取鼠标在屏幕上的 2D 坐标
        Vector2 mousePosition = Mouse.current.position.ReadValue();

        // 3. 【核心防御】：检查鼠标坐标是否合法
        // 如果鼠标坐标正好是 (0,0)，或者超出了屏幕边界（比如切回桌面时），直接跳过不旋转
        if (mousePosition.x <= 0 && mousePosition.y <= 0) return;

        // 4. 从摄像机发射穿过鼠标位置的 3D 射线
        Ray ray = Camera.main.ScreenPointToRay(mousePosition);

        // 创建位于角色脚底的虚拟水平面
        Plane groundPlane = new Plane(Vector3.up, transform.position);
        float rayLength;

        // 如果射线射中了地面
        if (groundPlane.Raycast(ray, out rayLength))
        {
            Vector3 lookPoint = ray.GetPoint(rayLength);

            // 计算方向向量，并将 Y 轴锁死在角色自身高度，防止胶囊体“低头/翻倒”
            Vector3 targetDirection = new Vector3(lookPoint.x, transform.position.y, lookPoint.z) - transform.position;

            // 5. 再次防御：只有当鼠标距离角色有一点点距离时才旋转，防止鼠标贴着角色时产生剧烈抖动
            if (targetDirection.magnitude > 0.2f)
            {
                Quaternion newRotation = Quaternion.LookRotation(targetDirection);
                transform.rotation = newRotation;
            }
        }
    }
}