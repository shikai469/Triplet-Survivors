using UnityEngine;

public static class GameDataManager
{
    private const string TOTAL_GOLD_KEY = "Player_TotalGold";

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
}