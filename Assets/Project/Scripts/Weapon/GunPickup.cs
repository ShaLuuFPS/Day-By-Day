using UnityEngine;

public class GunPickup : MonoBehaviour
{
    [Header("🌟 核心武器配置")]
    public WeaponData weaponConfig; // 这把地面枪使用的是哪份配置文件

    [Header("实时地面子弹数据")]
    public int runtimeAmmo;         // 地面这把枪当前弹夹里还剩几发
    public int runtimeReserve;      // 地面这把枪还剩多少备弹

    private void Start()
    {
        // 1. 如果在编辑器里没有手动改子弹，默认用配置文件的初始状态填满它
        if (weaponConfig != null && runtimeAmmo == 0 && runtimeReserve == 0)
        {
            runtimeAmmo = weaponConfig.maxMagazineSize;
            runtimeReserve = weaponConfig.defaultReserveAmmo;
        }

        // 2. ✨ 视觉换装：根据不同的枪械名字，自动改变方块颜色（两个不同的颜色）
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null && weaponConfig != null)
        {
            if (weaponConfig.weaponName == "手枪")
            {
                renderer.material.color = Color.green; // 手枪在地上显示为绿色方块
            }
            else if (weaponConfig.weaponName == "突击步枪")
            {
                renderer.material.color = Color.red;   // 步枪在地上显示为红色方块
            }
        }

        // 3. 规范命名，方便 Player 识别
        gameObject.name = "GunPickup_Real";
    }

    // 提供一个初始化方法，方便玩家丢枪或者动态生成时调用
    public void Initialize(WeaponData config, int ammo, int reserve)
    {
        this.weaponConfig = config;
        this.runtimeAmmo = ammo;
        this.runtimeReserve = reserve;
    }
}