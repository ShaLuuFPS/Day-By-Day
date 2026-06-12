using UnityEngine;

public interface IInteractable
{
    // 每一个交互物要显示的提示文字（例如："换枪"、"开门"、"开启补给箱"）
    string InteractionPrompt { get; }

    // 真正按下 E 键时触发的核心逻辑
    void Interact(WeaponManager weaponManager);
}