using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;

public class PlayerInteraction : MonoBehaviour
{
    [Header("交互防抖配置")]
    private static float globalPickupTimer = 0f;
    private const float PICKUP_COOLDOWN = 1.0f;

    [Header("缓存雷达队列")]
    private List<IInteractable> nearbyInteractables = new List<IInteractable>(); // 附近所有的交互物

    // 缓存玩家身上的射击组件，用于传参
    private PlayerShooting playerShooting;

    // 🌟 核心通用交互事件：改由雷达脚本来向 UI Manager 发送通知
    public static event Action<string> OnShowInteractionUI;
    public static event Action OnHideInteractionUI;

    void Awake()
    {
        playerShooting = GetComponent<PlayerShooting>();
    }

    void Update()
    {
        HandleUniversalInteraction();
    }

    /// <summary>
    /// 处理通用交互与多物体冲突雷达
    /// </summary>
    private void HandleUniversalInteraction()
    {
        // 清理列表中可能已经被 Destroy 掉的无效残留（安全防护）
        nearbyInteractables.RemoveAll(x => x == null || (x is MonoBehaviour mb && mb == null));

        if (nearbyInteractables.Count == 0)
        {
            OnHideInteractionUI?.Invoke();
            return;
        }

        // 💡 冲突破局：通过计算物理距离，死死咬住【离玩家最近的那一个】！
        IInteractable closestInteractable = GetClosestInteractable();
        if (closestInteractable == null) return;

        // 通知 UI 实时刷新这个最近物体的提示文字
        OnShowInteractionUI?.Invoke(closestInteractable.InteractionPrompt);

        // 监听按 E 键
        if (Keyboard.current.eKey.wasPressedThisFrame && Time.time >= globalPickupTimer)
        {
            globalPickupTimer = Time.time + PICKUP_COOLDOWN;

            IInteractable target = closestInteractable;
            nearbyInteractables.Remove(target);

            // 砰！触发这个物体特有的 Interact 逻辑，把玩家射击组件传过去
            target.Interact(playerShooting);
        }
    }

    /// <summary>
    /// 算法：获取附近最近的交互物体
    /// </summary>
    private IInteractable GetClosestInteractable()
    {
        IInteractable closest = null;
        float closestDistance = Mathf.Infinity;

        foreach (var interactable in nearbyInteractables)
        {
            if (interactable is MonoBehaviour mb)
            {
                float dist = Vector3.Distance(transform.position, mb.transform.position);
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    closest = interactable;
                }
            }
        }
        return closest;
    }

    // 🌟 外来物体吐出时，提供一个公开接口，允许重新塞回雷达队列
    public void RegisterInteractable(IInteractable interactable)
    {
        if (interactable != null && !nearbyInteractables.Contains(interactable))
        {
            nearbyInteractables.Add(interactable);
        }
    }

    // 清空雷达，常用于死亡复活重置
    public void ClearRadar()
    {
        nearbyInteractables.Clear();
        OnHideInteractionUI?.Invoke();
    }

    private void OnTriggerEnter(Collider other)
    {
        IInteractable interactable = other.GetComponent<IInteractable>();
        if (interactable != null && !nearbyInteractables.Contains(interactable))
        {
            nearbyInteractables.Add(interactable);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        IInteractable interactable = other.GetComponent<IInteractable>();
        if (interactable != null && nearbyInteractables.Contains(interactable))
        {
            nearbyInteractables.Remove(interactable);
        }
    }
}