using UnityEngine;

/// <summary>
/// 掉落物通用工具 —— 贴地放置，供经验球、弹药箱等所有掉落物复用
/// </summary>
public static class DropHelper
{
    /// <summary>
    /// 将物体放到地面上。自动从高空 RaycastAll 找最低地面，
    /// 并按物体 Renderer 半高偏移确保底部贴地。
    /// </summary>
    public static void PlaceOnGround(Transform t)
    {
        Vector3 pos = t.position;
        Vector3 rayOrigin = new Vector3(pos.x, 100f, pos.z);
        RaycastHit[] hits = Physics.RaycastAll(rayOrigin, Vector3.down, 200f);

        float groundY = pos.y;
        foreach (var h in hits)
            if (h.point.y < groundY) groundY = h.point.y;

        // 取 Renderer 半高使物体底部贴地（而非中心陷入地下）
        float halfHeight = 0f;
        Renderer r = t.GetComponent<Renderer>();
        if (r != null)
            halfHeight = r.bounds.extents.y;

        t.position = new Vector3(pos.x, groundY + halfHeight, pos.z);
    }

    /// <summary>
    /// 仅查询地面 Y 坐标（不移动物体）
    /// </summary>
    public static float GetGroundY(Vector3 position)
    {
        Vector3 rayOrigin = new Vector3(position.x, 100f, position.z);
        RaycastHit[] hits = Physics.RaycastAll(rayOrigin, Vector3.down, 200f);

        float groundY = position.y;
        foreach (var h in hits)
            if (h.point.y < groundY) groundY = h.point.y;
        return groundY;
    }
}
