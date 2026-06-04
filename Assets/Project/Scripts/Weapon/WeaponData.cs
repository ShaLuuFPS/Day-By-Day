using UnityEngine;

[CreateAssetMenu(fileName = "NewWeaponData", menuName = "FPS/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("枪械核心身份")]
    public string weaponName = "未命名武器";
    public int maxMagazineSize = 30;    // 弹夹容量上限
    public float reloadTime = 2.0f;     // 换弹时间

    [Header("射击核心属性")]
    public float fireRate = 0.1f;       // 射速
    public bool isAutomatic = true;     // 是否是全自动
    public float damage = 10f;          // 🌟 新增：这把枪每发子弹的伤害！

    [Header("背弹核心属性")]
    public int defaultReserveAmmo = 90; // 🌟 新增：这把枪第一次被捡到时，赠送的初始备弹量！

    [Header("换弹机制")]
    [Tooltip("true = 弹夹式一次性装满 / false = 逐发装填（霰弹枪）")]
    public bool isMagazineReload = true;
    [Tooltip("逐发装填时每发耗时（秒），仅 isMagazineReload=false 时生效")]
    public float perShellReloadTime = 0.3f;

    [Header("霰弹枪专属")]
    [Tooltip("每次开火射出的弹丸数量，默认 1 = 普通枪械")]
    public int bulletsPerShot = 1;
    [Tooltip("弹丸散布角度（度），0 = 完全精准，建议霰弹枪 5~10")]
    public float spreadAngle = 0f;

    [Header("视觉外显")]
    public GameObject bulletPrefab;     // 所有的枪现在可以共享同一个通用子弹预制体了！
}