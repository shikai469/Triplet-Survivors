using UnityEngine;

public static class GameDataManager
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeMobileSettings()
    {
        // Khóa 60 FPS mượt mà cho mobile, tắt VSync để không bị giới hạn 30 FPS trên các màn hình tần số thấp
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
        // Giữ màn hình luôn sáng, không bị sleep/dim khi đang chơi game
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }

    private const string TOTAL_GOLD_KEY = "Player_TotalGold";
    private const string SELECTED_DIFFICULTY_KEY = "Player_SelectedDifficulty";

    // --- QUẢN LÝ TIỀN VÀNG & NÂNG CẤP ---

    // Lấy tổng số vàng vĩnh viễn ngoài Menu
    public static int GetTotalGold()
    {
        return PlayerPrefs.GetInt(TOTAL_GOLD_KEY, 0);
    }

    // Cộng thêm vàng kiếm được từ trận đấu vào tổng vàng
    public static void AddGold(int amount)
    {
        int currentGold = GetTotalGold();
        PlayerPrefs.SetInt(TOTAL_GOLD_KEY, currentGold + amount);
        PlayerPrefs.Save();
    }

    // Tiêu vàng khi nâng cấp chỉ số ở Menu
    public static bool SpendGold(int cost)
    {
        int currentGold = GetTotalGold();
        if (currentGold >= cost)
        {
            PlayerPrefs.SetInt(TOTAL_GOLD_KEY, currentGold - cost);
            PlayerPrefs.Save();
            return true;
        }
        return false;
    }

    // Lưu và lấy Level của từng mục Power-up (theo lvupsk.txt)
    public static int GetUpgradeLevel(string upgradeKey)
    {
        return PlayerPrefs.GetInt("Powerup_" + upgradeKey, 0);
    }

    public static void SetUpgradeLevel(string upgradeKey, int level)
    {
        PlayerPrefs.SetInt("Powerup_" + upgradeKey, level);
        PlayerPrefs.Save();
    }

    // Lấy cấp độ Lời Nguyền (Curse) - Mỗi cấp tăng 10% máu quái vật
    public static int GetCurseLevel()
    {
        return GetUpgradeLevel("Curse");
    }

    // --- QUẢN LÝ DỮ LIỆU BACKGROUND TRUYỀN GIỮA LEVEL SELECT VÀ GAMEPLAY SCENE ---
    public static Sprite SelectedBackgroundSprite { get; set; }
    public static Material SelectedBackgroundMaterial { get; set; }

    // --- QUẢN LÝ ĐỘ KHÓ / LEVEL ĐƯỢC CHỌN (GIAO THỨC CHUYỂN SCENE) ---

    public static DifficultyLevel GetSelectedDifficulty()
    {
        int diffInt = PlayerPrefs.GetInt(SELECTED_DIFFICULTY_KEY, (int)DifficultyLevel.Normal);
        return (DifficultyLevel)diffInt;
    }

    public static void SetSelectedDifficulty(DifficultyLevel difficulty)
    {
        PlayerPrefs.SetInt(SELECTED_DIFFICULTY_KEY, (int)difficulty);
        PlayerPrefs.Save();
    }

    // --- QUẢN LÝ KỶ LỤC TỪNG ĐỘ KHÓ (CLEAR, DEATH, SURVIVAL TIME, SCORE) ---

    private static string GetDiffPrefix(DifficultyLevel diff) => "Record_" + diff.ToString() + "_";

    public static int GetClearCount(DifficultyLevel diff)
    {
        return PlayerPrefs.GetInt(GetDiffPrefix(diff) + "ClearCount", 0);
    }

    public static void AddClearCount(DifficultyLevel diff)
    {
        int count = GetClearCount(diff) + 1;
        PlayerPrefs.SetInt(GetDiffPrefix(diff) + "ClearCount", count);
        PlayerPrefs.Save();
    }

    public static int GetDeathCount(DifficultyLevel diff)
    {
        return PlayerPrefs.GetInt(GetDiffPrefix(diff) + "DeathCount", 0);
    }

    public static void AddDeathCount(DifficultyLevel diff)
    {
        int count = GetDeathCount(diff) + 1;
        PlayerPrefs.SetInt(GetDiffPrefix(diff) + "DeathCount", count);
        PlayerPrefs.Save();
    }

    public static float GetBestSurvivalTime(DifficultyLevel diff)
    {
        return PlayerPrefs.GetFloat(GetDiffPrefix(diff) + "BestTime", 0f);
    }

    public static void SaveSurvivalTime(DifficultyLevel diff, float seconds)
    {
        float currentBest = GetBestSurvivalTime(diff);
        if (seconds > currentBest)
        {
            PlayerPrefs.SetFloat(GetDiffPrefix(diff) + "BestTime", seconds);
            PlayerPrefs.Save();
        }
    }

    public static string GetFormattedBestSurvivalTime(DifficultyLevel diff)
    {
        float best = GetBestSurvivalTime(diff);
        if (best <= 0f) return "--:--";
        int m = Mathf.FloorToInt(best / 60f);
        int s = Mathf.FloorToInt(best % 60f);
        return string.Format("{0:00}:{1:00}", m, s);
    }

    public static int GetBestScore(DifficultyLevel diff)
    {
        return PlayerPrefs.GetInt(GetDiffPrefix(diff) + "BestScore", 0);
    }

    public static void SaveBestScore(DifficultyLevel diff, int score)
    {
        int currentBest = GetBestScore(diff);
        if (score > currentBest)
        {
            PlayerPrefs.SetInt(GetDiffPrefix(diff) + "BestScore", score);
            PlayerPrefs.Save();
        }
    }
}