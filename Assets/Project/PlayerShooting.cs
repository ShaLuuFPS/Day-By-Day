using UnityEngine;
using UnityEngine.InputSystem; // 使用新输入系统
using TMPro;
public class PlayerShooting : MonoBehaviour
{
    [Header("枪械状态")]
    public bool hasWeapon = false;     // 是否已经捡到枪
    public int currentAmmo = 7;        // 当前弹夹子弹（上限7）
    public int maxMagazineSize = 7;    // 弹夹容量上限
    public int reserveAmmo = 28;       // 备弹总量

    [Header("射击预制体")]
    public GameObject bulletPrefab;    // 子弹的预制体（等会制作）
    public Transform firePoint;        // 枪口位置（也就是角色的face/nose）

    [Header("UI 元素")]
    public TextMeshProUGUI ammoText;

    private PlayerHealth playerHealth;

    void Start()
    {
        playerHealth = GetComponent<PlayerHealth>();
        UpdateAmmoUI();
    }


    void Update()
    {
        // 🚨【火力拦截】：死人是不能扣动扳机和换弹的！
        if (playerHealth != null && playerHealth.IsDead)
        {
            return;
        }
        // 如果还没捡到枪，后面所有的射击、换弹逻辑都不执行
        if (!hasWeapon) return;

        // 1. 监听鼠标左键开火
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Shoot();
        }

        // 2. 监听 R 键换弹
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            Reload();
        }
    }

    void Shoot()
    {
        // 检查弹夹是否有弹药
        if (currentAmmo > 0)
        {
            currentAmmo--; // 扣除一发子弹
            Debug.Log($"开火！弹夹剩余: {currentAmmo}/{maxMagazineSize}");

            UpdateAmmoUI();
            // 在枪口位置（firePoint），以枪口的旋转方向，克隆（生成）一颗子弹
            Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        }
        else
        {
            Debug.Log("没子弹了，请按 R 换弹！");
        }
    }

    void Reload()
    {
        // 如果弹夹已经满了，或者已经没有备弹了，就不需要换弹
        if (currentAmmo == maxMagazineSize || reserveAmmo <= 0) return;

        // 核心逻辑：把当前弹夹剩下的子弹退回备弹
        reserveAmmo += currentAmmo;
        currentAmmo = 0; // 此时弹夹清空

        // 计算需要从备弹里取多少发子弹（通常是 7 发）
        int ammoNeeded = maxMagazineSize;

        // 如果备弹数量足够填满弹夹
        if (reserveAmmo >= ammoNeeded)
        {
            currentAmmo = ammoNeeded;
            reserveAmmo -= ammoNeeded;
        }
        else // 如果备弹不够填满弹夹了（比如只剩3发备弹）
        {
            currentAmmo = reserveAmmo;
            reserveAmmo = 0;
        }
        UpdateAmmoUI();
        Debug.Log($"换弹成功！当前弹夹: {currentAmmo}，剩余备弹: {reserveAmmo}");
    }

    // Unity 内置的触发检测函数：当有物体撞进自己的 Trigger 时自动触发
    private void OnTriggerEnter(Collider other)
    {
        // 检查撞到的物体是不是叫 "GunPickup"
        if (other.gameObject.name == "GunPickup")
        {
            hasWeapon = true; // 激活武器状态
            Debug.Log("捡起手枪！可以开始射击了。");
            UpdateAmmoUI();
            Destroy(other.gameObject); // 让地上的物资箱消失
        }
    }


    void UpdateAmmoUI()
    {
        // 健壮性检查：防止忘记拖入 UI 组件导致游戏崩溃
        if (ammoText == null) return;

        // 如果还没有捡到武器，屏幕上什么都不显示（或者显示横杠 --）
        if (!hasWeapon)
        {
            ammoText.text = "- / -";
        }
        else
        {
            // 将数字拼接成字符串：例如 "7" + " / " + "28" = "7 / 28"
            ammoText.text = currentAmmo.ToString() + " / " + reserveAmmo.ToString();
        }
    }
}