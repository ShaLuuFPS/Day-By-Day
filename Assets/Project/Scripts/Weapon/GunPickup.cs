using UnityEngine;

// 🌟 让地面枪械继承通用交互契约
public class GunPickup : MonoBehaviour, IInteractable
{
    [Header("核心武器配置")]
    public WeaponData weaponConfig;
    public int runtimeAmmo;
    public int runtimeReserve;

    // 🌟 实现接口：告诉系统我是一把枪，提示文字叫什么
    public string InteractionPrompt => weaponConfig != null ? $"换取 {weaponConfig.weaponName}" : "未知武器";

    private void Start()
    {
        if (weaponConfig != null && runtimeAmmo == 0 && runtimeReserve == 0)
        {
            runtimeAmmo = weaponConfig.maxMagazineSize;
            runtimeReserve = weaponConfig.defaultReserveAmmo;
        }
        RefreshPickupState();
        gameObject.name = "GunPickup_Real";
    }

    public void RefreshPickupState()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null && weaponConfig != null)
        {
            renderer.material.color = weaponConfig.weaponName switch
            {
                "手枪" => Color.green,
                "霰弹枪" => Color.yellow,
                _ => Color.red
            };
        }
    }

    public void Initialize(WeaponData config, int ammo, int reserve)
    {
        this.weaponConfig = config;
        this.runtimeAmmo = ammo;
        this.runtimeReserve = reserve;
    }

    // 🌟 实现接口：按下 E 键后真实干的事
    public void Interact(PlayerShooting player)
    {
        player.ExecuteInteraction(this, gameObject);
    }
}