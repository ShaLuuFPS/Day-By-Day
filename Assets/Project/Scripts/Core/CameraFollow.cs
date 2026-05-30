using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    // 1. 告诉相机我们要跟随的目标是谁（把 Player 拖进来）
    public Transform target;

    // 2. 缓存相机与玩家之间的固定相对距离（偏移量）
    private Vector3 offset;

    void Start()
    {
        // 在游戏刚开始的第一帧，计算出相机和玩家目前的相对距离
        // 比如相机在 (0, 10, -10)，玩家在 (0, 0, 0)，那 offset 就是 (0, 10, -10)
        if (target != null)
        {
            offset = transform.position - target.position;
        }
    }

    // LateUpdate 是在所有物体的 Update（移动、转向）执行完毕后，才最后执行的
    // 这能确保玩家先移动完，相机再跟过去，防止画面产生诡异的抖动
    void LateUpdate()
    {
        if (target == null) return;

        // 3. 核心逻辑：相机的目标位置 = 玩家当前的新位置 + 游戏开始时算好的固定相对距离
        Vector3 targetPosition = target.position + offset;

        // 4. 把相机的坐标更新过去
        // 注意：这里只改变了相机的 Position（坐标），完全没有动相机的 Rotation（旋转）！
        transform.position = targetPosition;
    }
}