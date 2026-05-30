using UnityEngine;

public class InteractableDoor : MonoBehaviour, IInteractable
{
    [Header("门的配置")]
    public string doorName = "实验室大门";
    private bool isOpen = false;

    // 🌟 实现契约：告诉雷达 UI 该显示什么
    public string InteractionPrompt => isOpen ? $"关闭 {doorName}" : $"打开 {doorName}";

    // 🌟 实现契约：按 E 后会发生什么
    public void Interact(PlayerShooting player)
    {
        isOpen = !isOpen;

        if (isOpen)
        {
            // 简单演示：开门时向上升起 3 米
            transform.position += Vector3.up * 3f;
            Debug.Log($"{doorName} 已被玩家打开！");
        }
        else
        {
            // 关门时落回原位
            transform.position -= Vector3.up * 3f;
            Debug.Log($"{doorName} 已被玩家关闭！");
        }
    }
}