using UnityEngine;

public enum DifficultyLevel
{
    Easy = 0,       // Dễ - Dành cho người mới làm quen (Prologue / Operation 1)
    Normal = 1,     // Bình thường - Thử thách chuẩn mực (Operation 2, 3 / Farming 2)
    Hard = 2,       // Khó - Áp lực cao, quái giáp dày & nổ tự sát (Operation 4, 5, 6)
    Endless = 3     // Vô tận - Chế độ Pheromone / Vô hạn sinh tồn không có điểm dừng
}

[System.Serializable]
public class DifficultyConfig
{
    [Header("General Info")]
    public DifficultyLevel level = DifficultyLevel.Normal;
    public string difficultyName = "Bình Thường";
    [TextArea(2, 4)]
    public string levelDescription = "Chế độ tiêu chuẩn với độ khó tăng dần.";
    public bool isEndless = false;

    [Header("Match Timing (Seconds)")]
    public float stageDuration = 600f;       // Thời lượng màn chơi (Dễ: 300s, Normal: 600s, Hard: 900s)
    public float warningThreshold = 20f;     // Thời gian báo động cuối trận
    public float endlessBossInterval = 120f; // Cứ mỗi 120s trong Endless sẽ xuất hiện 1 Boss

    [Header("Spawner Tuning")]
    public float baseSpawnInterval = 1.0f;   // Thời gian giãn cách giữa các đợt spawn
    public float minSpawnInterval = 0.5f;    // Giới hạn spawn nhanh nhất có thể đạt
    public int baseMaxEnemies = 60;          // Số lượng quái tối đa trên sân ban đầu
    public int maxEnemiesCap = 100;          // Trần quái tối đa cho phép
    public float waveDuration = 30f;         // Thời gian mỗi wave (giây)

    [Header("Monster Stats Scaling (Theo PhanTich_LevelSelectManager_MonsterStats.md)")]
    public float baseHpMultiplier = 1.5f;    // Hệ số máu ban đầu
    public float hpGrowthPerWave = 0.25f;    // Tăng máu mỗi wave (+25%)
    public int armorStartWave = 4;           // Wave bắt đầu xuất hiện giáp phẳng
    public float armorGrowthRate = 0.5f;     // Tốc độ tăng giáp theo wave
    public int minDamage = 8;                // Sát thương tối thiểu quái gây ra
    public int maxDamage = 18;               // Sát thương tối đa quái gây ra
    public float baseMoveSpeed = 2.0f;       // Tốc độ di chuyển ban đầu
    public float maxMoveSpeed = 3.0f;        // Giới hạn tốc độ tối đa
    public float knockbackResistance = 35f;  // Lực cản đẩy lùi (50: dễ đẩy lùi, 35: trung bình, 0: miễn nhiễm)

    [Header("Economic & Score Multipliers")]
    public float goldMultiplier = 1.5f;      // Hệ số thưởng vàng
    public float expMultiplier = 1.25f;      // Hệ số thưởng EXP
    public float scoreMultiplier = 1.5f;     // Hệ số tính điểm tổng kết

    [Header("Boss Prefab Override (Tùy chọn)")]
    public GameObject bossPrefabOverride;

    [Header("Background Map Override (Tùy chọn)")]
    public Sprite backgroundSpriteOverride;
    public Material backgroundMaterialOverride;

    // Factory tạo cấu hình mặc định chuẩn chỉnh theo tài liệu phân tích kỹ thuật
    public static DifficultyConfig GetDefault(DifficultyLevel targetLevel)
    {
        DifficultyConfig cfg = new DifficultyConfig();
        cfg.level = targetLevel;

        switch (targetLevel)
        {
            case DifficultyLevel.Easy:
                cfg.difficultyName = "DỄ";
                cfg.levelDescription = "Màn chơi làm quen (Prologue / Chiến dịch 1). Quái máu thấp, không có giáp, tốc độ chậm và sát thương yếu.";
                cfg.isEndless = false;
                cfg.stageDuration = 300f; // 5 phút (có thể test nhanh 60s tuỳ chỉnh)
                cfg.warningThreshold = 15f;
                cfg.baseSpawnInterval = 1.8f;
                cfg.minSpawnInterval = 1.2f;
                cfg.baseMaxEnemies = 35;
                cfg.maxEnemiesCap = 50;
                cfg.waveDuration = 30f;
                cfg.baseHpMultiplier = 1.0f;
                cfg.hpGrowthPerWave = 0.15f;
                cfg.armorStartWave = 99; // Không xuất hiện quái bọc giáp
                cfg.armorGrowthRate = 0f;
                cfg.minDamage = 1;
                cfg.maxDamage = 4;
                cfg.baseMoveSpeed = 1.5f;
                cfg.maxMoveSpeed = 2.2f;
                cfg.knockbackResistance = 50f; // Dễ dàng thả diều (kiting)
                cfg.goldMultiplier = 1.0f;
                cfg.expMultiplier = 1.0f;
                cfg.scoreMultiplier = 1.0f;
                break;

            case DifficultyLevel.Normal:
                cfg.difficultyName = "BÌNH THƯỜNG";
                cfg.levelDescription = "Màn chơi tiêu chuẩn (Chiến dịch 2 & 3). Quái bắt đầu có giáp 1, sát thương khá và có đợt quái bao vây.";
                cfg.isEndless = false;
                cfg.stageDuration = 600f; // 10 phút
                cfg.warningThreshold = 20f;
                cfg.baseSpawnInterval = 1.0f;
                cfg.minSpawnInterval = 0.6f;
                cfg.baseMaxEnemies = 60;
                cfg.maxEnemiesCap = 80;
                cfg.waveDuration = 30f;
                cfg.baseHpMultiplier = 1.5f;
                cfg.hpGrowthPerWave = 0.25f;
                cfg.armorStartWave = 4;  // Từ wave 4 xuất hiện quái có giáp 1
                cfg.armorGrowthRate = 0.5f;
                cfg.minDamage = 8;
                cfg.maxDamage = 18;
                cfg.baseMoveSpeed = 2.0f;
                cfg.maxMoveSpeed = 3.0f;
                cfg.knockbackResistance = 35f; // Giảm 30% cự ly giật lùi
                cfg.goldMultiplier = 1.5f;
                cfg.expMultiplier = 1.25f;
                cfg.scoreMultiplier = 1.5f;
                break;

            case DifficultyLevel.Hard:
                cfg.difficultyName = "KHÓ";
                cfg.levelDescription = "Màn chơi khắc nghiệt (Chiến dịch 4, 5 & 6). Quái giáp dày 2-5, quái nổ tự sát AOE cực mạnh và kháng đẩy lùi cao.";
                cfg.isEndless = false;
                cfg.stageDuration = 900f; // 15 phút
                cfg.warningThreshold = 30f;
                cfg.baseSpawnInterval = 0.65f;
                cfg.minSpawnInterval = 0.35f;
                cfg.baseMaxEnemies = 90;
                cfg.maxEnemiesCap = 130;
                cfg.waveDuration = 25f;
                cfg.baseHpMultiplier = 2.5f;
                cfg.hpGrowthPerWave = 0.40f;
                cfg.armorStartWave = 2;  // Ngay từ wave 2 đã có giáp dày
                cfg.armorGrowthRate = 0.8f;
                cfg.minDamage = 20;
                cfg.maxDamage = 45;
                cfg.baseMoveSpeed = 2.5f;
                cfg.maxMoveSpeed = 3.8f;
                cfg.knockbackResistance = 10f; // Rất khó bị đẩy lùi, lù lù tiến tới
                cfg.goldMultiplier = 2.0f;
                cfg.expMultiplier = 1.5f;
                cfg.scoreMultiplier = 2.0f;
                break;

            case DifficultyLevel.Endless:
                cfg.difficultyName = "VÔ TẬN (ENDLESS)";
                cfg.levelDescription = "Chế độ Pheromone sinh tồn không giới hạn thời gian. Quái tăng máu, giáp và tốc độ vô hạn; Boss xuất hiện định kỳ mỗi 2 phút!";
                cfg.isEndless = true;
                cfg.stageDuration = 0f;  // Vô tận (Stopwatch đếm tăng dần)
                cfg.warningThreshold = 0f;
                cfg.endlessBossInterval = 120f; // Mỗi 120s ra 1 Boss
                cfg.baseSpawnInterval = 1.0f;
                cfg.minSpawnInterval = 0.25f;
                cfg.baseMaxEnemies = 70;
                cfg.maxEnemiesCap = 200;
                cfg.waveDuration = 30f;
                cfg.baseHpMultiplier = 1.2f;
                cfg.hpGrowthPerWave = 0.35f; // Tăng tiến theo thời gian không giới hạn
                cfg.armorStartWave = 3;
                cfg.armorGrowthRate = 0.6f;
                cfg.minDamage = 10;
                cfg.maxDamage = 25;
                cfg.baseMoveSpeed = 2.0f;
                cfg.maxMoveSpeed = 4.0f;
                cfg.knockbackResistance = 30f;
                cfg.goldMultiplier = 2.5f;
                cfg.expMultiplier = 2.0f;
                cfg.scoreMultiplier = 3.0f;
                break;
        }

        return cfg;
    }
}
