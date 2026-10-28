using UnityEngine;

// Dòng này giúp bạn click chuột phải trong Unity để tạo ra một file Thẻ kỹ năng mới
[CreateAssetMenu(fileName = "NewUpgrade", menuName = "TripleT/Upgrade Card")]
public class UpgradeData : ScriptableObject
{
    public Sprite icon;
    public string title;
    public string description;
    public int maxLevel = 5;

    // Danh sách các loại kỹ năng
    public enum UpgradeType
    {
        // Loại cũ (giữ nguyên chỉ số 0..3 để tương thích hoàn toàn với file asset cũ)
        MoveSpeed = 0,
        MagnetRange = 1,
        FireRate = 2,
        Heal = 3,

        // 8 KỸ NĂNG BỔ TRỢ & PASSIVE MỚI:
        DamageUp = 4,           // 1. Tăng Sát Thương [Max Lv 5]
        PiercingShot = 5,       // 2. Đạn Xuyên Thấu [Max Lv 3]
        Multishot = 6,          // 3A. Đạn Tõe [Max Lv 5] (Độc quyền với BurstFire)
        BurstFire = 7,          // 3B. Đạn Liên Tục [Max Lv 5] (Độc quyền với Multishot)
        OrbitSwords = 8,        // 4. Kiếm Bay [Max Lv 3]
        SeagullAlly = 9,        // 5. Hải Âu Trợ Chiến [Max Lv 3]
        GrenadeToss = 10,       // 6. Ném Lựu Đạn [Max Lv 3]
        EnergyShield = 11,      // 7. Giáp Hộ Thể [Max Lv 3]
        BouncingBoomerang = 12, // 8. Boomerang Dội Tường [Max Lv 3]

        // CẤU TRÚC THẺ DỰ PHÒNG (Fallback Items) KHI CẠN POOL:
        SelectItem_Coin = 13,    // Thưởng 10 ~ 25 Vàng trực tiếp
        SelectItem_HealPack = 14 // Hồi phục ngay 100 HP (hoặc 30% Max HP)
    }

    public UpgradeType upgradeType;

    [Header("Mô tả theo từng cấp (Tùy chọn)")]
    [Tooltip("Nếu để trống, hệ thống sẽ tự động sinh mô tả theo cấp độ")]
    public string[] levelDescriptions;

    /// <summary>
    /// Lấy mô tả chi tiết cho cấp độ tiếp theo
    /// </summary>
    public string GetDescriptionForLevel(int nextLevel)
    {
        if (levelDescriptions != null && levelDescriptions.Length >= nextLevel && nextLevel > 0)
        {
            return levelDescriptions[nextLevel - 1];
        }
        return description;
    }
}