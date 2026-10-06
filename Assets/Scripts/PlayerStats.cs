using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance;
    private bool isLevelingUp = false;

    [Header("XP & Level Settings")]
    public Slider xpBar;
    public TextMeshProUGUI levelText;
    public int currentLevel = 1;
    public int currentXp = 0;
    public int xpToNextLevel = 10;
    public GameObject levelUpPanel;

    [Header("Counters UI")]
    public TextMeshProUGUI killText; // Kéo Text_Kill vào đây
    public TextMeshProUGUI goldText; // Kéo Text_Gold vào đây
    public int killCount = 0;
    public int goldCount = 0;

    [Header("Combat & Passive Stats (Isekai Core)")]
    public float strengthBonus = 0f;
    public float speedBonus = 0f;
    public float expBonus = 0f;
    public float magnetRangeBonus = 0f; // Hệ số tăng tầm hút (+0.25 = +25%)
    public int coinBonus = 0;
    public int armor = 0;

    [Header("Upgrades Limit")]
    public int maxUpgradeLevel = 5;
    public int speedLevel = 0;
    public int magnetLevel = 0;
    public int fireRateLevel = 0;

    [Header("Upgrade Buttons (Optional)")]
    public GameObject speedButton;
    public GameObject magnetButton;
    public GameObject fireRateButton;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        // 1. Áp dụng các chỉ số nâng cấp vĩnh viễn ngoài Menu theo lvupsk.txt
        int lvHealth = GameDataManager.GetUpgradeLevel("Health");      // +10 HP mỗi rank
        int lvArmor = GameDataManager.GetUpgradeLevel("Defense");      // +1 Armor mỗi rank
        int lvSpeed = GameDataManager.GetUpgradeLevel("Speed");        // +5% Speed mỗi rank
        int lvMagnet = GameDataManager.GetUpgradeLevel("Magnet");      // +5% tầm hút mỗi rank
        int lvEXP = GameDataManager.GetUpgradeLevel("Growth");         // +3% EXP mỗi rank
        int lvGreed = GameDataManager.GetUpgradeLevel("Greed");        // +2 Gold nhặt mỗi rank

        // Đồng bộ vào chỉ số nhân vật
        armor += lvArmor;
        expBonus += lvEXP * 0.03f;
        magnetRangeBonus += lvMagnet * 0.05f;
        coinBonus += lvGreed * 2;

        if (PlayerHealth.Instance != null)
        {
            PlayerHealth.Instance.maxHealth += lvHealth * 10f;
            PlayerHealth.Instance.currentHealth = PlayerHealth.Instance.maxHealth;
            PlayerHealth.Instance.UpdateHealthUI();
        }

        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null)
        {
            movement.moveSpeed *= (1f + lvSpeed * 0.05f);
        }

        // 2. Khởi tạo mốc EXP cấp 1
        xpToNextLevel = CalculateRequiredXp(currentLevel);
        UpdateUI();
    }

    public int CalculateRequiredXp(int level)
    {
        return Mathf.RoundToInt(10f * Mathf.Pow(1.15f, level - 1));
    }

    public void AddXP(int baseAmount)
    {
        int finalAmount = Mathf.RoundToInt(baseAmount * (1.0f + expBonus));
        currentXp += finalAmount;

        Debug.Log($"[NHẬN XP] +{finalAmount} | Hiện tại: {currentXp}/{xpToNextLevel}");

        while (currentXp >= xpToNextLevel)
        {
            LevelUp();
        }

        UpdateUI();
    }

    void LevelUp()
    {
        currentLevel++;
        currentXp -= xpToNextLevel;
        xpToNextLevel = CalculateRequiredXp(currentLevel);

        isLevelingUp = true;

        // Reset giả thanh XP về 0 để không bị lòi phần dư khi đang chọn thẻ
        if (xpBar != null)
        {
            xpBar.maxValue = xpToNextLevel;
            xpBar.value = 0f;
        }

        if (levelText != null)
        {
            levelText.text = "Lv. " + currentLevel;
        }

        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.RollUpgrades();
        }
    }

    public void AddGold(int baseAmount)
    {
        int finalGold = baseAmount + coinBonus;
        goldCount += finalGold;
        UpdateUI();
    }

    public void AddKill()
    {
        killCount++;
        UpdateUI();
    }

    public void UpdateUI()
    {
        // Khi đang mở bảng nâng cấp thì không cập nhật thanh slider
        if (xpBar != null && !isLevelingUp)
        {
            xpBar.maxValue = xpToNextLevel;
            xpBar.value = currentXp;
        }

        if (levelText != null)
        {
            levelText.text = "Lv. " + currentLevel;
        }

        if (killText != null) killText.text = killCount.ToString();
        if (goldText != null) goldText.text = goldCount.ToString();
    }

    // --- CÁC HÀM XỬ LÝ UPGRADE TRONG TRẬN ---
    public void UpgradeMoveSpeed()
    {
        if (speedLevel < maxUpgradeLevel)
        {
            speedLevel++;
            PlayerMovement movement = GetComponent<PlayerMovement>();
            if (movement != null) movement.moveSpeed += 0.5f;
        }
    }

    public void UpgradeMagnet()
    {
        if (magnetLevel < maxUpgradeLevel)
        {
            magnetLevel++;
            magnetRangeBonus += 0.25f;
        }
    }

    public void UpgradeFireRate()
    {
        if (fireRateLevel < maxUpgradeLevel)
        {
            fireRateLevel++;
            PlayerAttack attack = GetComponent<PlayerAttack>();
            if (attack != null && attack.fireRate > 0.15f)
            {
                attack.fireRate -= 0.1f;
            }
        }
    }

    // Đã thêm từ khóa public để UpgradeManager có thể gọi
    public void ResumeGame()
    {
        isLevelingUp = false;

        if (levelUpPanel != null) levelUpPanel.SetActive(false);
        Time.timeScale = 1f;

        // Trả thanh XP về đúng giá trị thực tế đang có
        if (xpBar != null)
        {
            xpBar.maxValue = xpToNextLevel;
            xpBar.value = currentXp;
        }

        UpdateUI();
    }
}