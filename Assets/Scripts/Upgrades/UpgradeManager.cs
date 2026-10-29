using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance;

    [Header("Dữ liệu gốc")]
    public List<UpgradeData> allUpgrades = new List<UpgradeData>();

    [Header("Giao diện UI Thẻ (Card_0, Card_1, Card_2)")]
    public GameObject levelUpPanel;
    public UpgradeCardUI[] upgradeCards; // Kéo 3 object Card_0, Card_1, Card_2 vào đây

    [Header("Cơ chế Reroll (Đổi lại)")]
    public Button btnReroll;            // Kéo nút Btn_Reroll vào đây
    public TextMeshProUGUI rerollText;  // Kéo Text (TMP) của Btn_Reroll vào đây
    public int maxRerollCount = 1;      // Số lần Reroll mặc định mỗi trận
    private int currentRerollCount;

    private List<UpgradeData> currentShuffledPool = new List<UpgradeData>();

    // Bộ nhớ cache cho các thẻ dự phòng (Fallback Items)
    private UpgradeData fallbackCoinCard;
    private UpgradeData fallbackHealCard;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        currentRerollCount = maxRerollCount;
        if (btnReroll != null)
        {
            btnReroll.onClick.AddListener(OnRerollClicked);
        }
        UpdateRerollUI();

        // Đảm bảo có đầy đủ dữ liệu 8 kỹ năng & thẻ fallback
        InitializeDefaultCardsIfNeeded();
    }

    /// <summary>
    /// Tự động sinh danh sách 8 kỹ năng chuẩn + 2 thẻ dự phòng nếu người dùng chưa kịp tạo/gán đủ file ScriptableObject
    /// </summary>
    private void InitializeDefaultCardsIfNeeded()
    {
        // 1. Tạo 2 thẻ fallback mặc định
        fallbackCoinCard = ScriptableObject.CreateInstance<UpgradeData>();
        fallbackCoinCard.upgradeType = UpgradeData.UpgradeType.SelectItem_Coin;
        fallbackCoinCard.title = "Thưởng Vàng";
        fallbackCoinCard.description = "Nhận ngay 10 ~ 25 Vàng trực tiếp vào túi đồ trong trận.";
        fallbackCoinCard.maxLevel = int.MaxValue;
        fallbackCoinCard.icon = ProceduralSkillAssets.GetSkillIcon(UpgradeData.UpgradeType.SelectItem_Coin);

        fallbackHealCard = ScriptableObject.CreateInstance<UpgradeData>();
        fallbackHealCard.upgradeType = UpgradeData.UpgradeType.SelectItem_HealPack;
        fallbackHealCard.title = "Túi Cứu Thương";
        fallbackHealCard.description = "Hồi phục ngay 100 HP (hoặc 30% Max HP) cho nhân vật.";
        fallbackHealCard.maxLevel = int.MaxValue;
        fallbackHealCard.icon = ProceduralSkillAssets.GetSkillIcon(UpgradeData.UpgradeType.SelectItem_HealPack);

        // 2. Kiểm tra xem danh sách allUpgrades đã có đủ các kỹ năng mới chưa
        HashSet<UpgradeData.UpgradeType> existingTypes = new HashSet<UpgradeData.UpgradeType>();
        for (int i = 0; i < allUpgrades.Count; i++)
        {
            if (allUpgrades[i] != null)
            {
                existingTypes.Add(allUpgrades[i].upgradeType);
                if (allUpgrades[i].icon == null)
                {
                    allUpgrades[i].icon = ProceduralSkillAssets.GetSkillIcon(allUpgrades[i].upgradeType);
                }
            }
        }

        // Tự động bổ sung các kỹ năng còn thiếu (tên ngắn gọn, không ngoặc)
        AddDefaultCardIfMissing(existingTypes, UpgradeData.UpgradeType.DamageUp, "Tăng Sát Thương", "Tăng trực tiếp sát thương đạn súng lục (+20% mỗi cấp).", 5);
        AddDefaultCardIfMissing(existingTypes, UpgradeData.UpgradeType.PiercingShot, "Đạn Xuyên Thấu", "Bắn ra viên đạn sniper cực mạnh bay xuyên thấu qua nhiều quái trên đường thẳng.", 3);
        AddDefaultCardIfMissing(existingTypes, UpgradeData.UpgradeType.Multishot, "Đạn Tỏa", "Nâng cấp súng lục bắn tỏa ra nhiều hướng. Tối đa ở cấp 5 bắn tỏa 3 viên.", 5);
        AddDefaultCardIfMissing(existingTypes, UpgradeData.UpgradeType.BurstFire, "Đạn Bắn Loạt", "Nâng cấp súng lục bắn một chuỗi đạn liên tiếp trên đường thẳng. Tối đa 3 viên liên tục.", 5);
        AddDefaultCardIfMissing(existingTypes, UpgradeData.UpgradeType.OrbitSwords, "Kiếm Bay", "Tạo kiếm xoay quanh nhân vật gây sát thương cho quái chạm vào. Tối đa 3 kiếm.", 3);
        AddDefaultCardIfMissing(existingTypes, UpgradeData.UpgradeType.SeagullAlly, "Hải Âu Trợ Chiến", "Triệu hồi chú chim hải âu bay kè kè bên cạnh, tự động nhả đạn hạt hướng dương về phía quái.", 3);
        AddDefaultCardIfMissing(existingTypes, UpgradeData.UpgradeType.GrenadeToss, "Ném Lựu Đạn", "Tự động ném quả lựu đạn gây nổ diện rộng (AOE) tiêu diệt đám đông quái.", 3);
        AddDefaultCardIfMissing(existingTypes, UpgradeData.UpgradeType.EnergyShield, "Giáp Hộ Thể", "Tạo lớp giáp ảo bảo vệ đỡ đòn. Tự động hồi đầy lại sau 5 giây không bị đánh.", 3);
        AddDefaultCardIfMissing(existingTypes, UpgradeData.UpgradeType.BouncingBoomerang, "Boomerang Dội Tường", "Ném Boomerang dội ngược khi chạm tường rồi quay về người chơi. Tối đa 3 cái.", 3);
        AddDefaultCardIfMissing(existingTypes, UpgradeData.UpgradeType.MoveSpeed, "Tăng Tốc Chạy", "Tăng 15% tốc độ di chuyển giúp né quái linh hoạt hơn.", 5);
        AddDefaultCardIfMissing(existingTypes, UpgradeData.UpgradeType.FireRate, "Tốc Độ Bắn", "Tăng 20% tốc độ bắn đạn súng lục.", 5);
        AddDefaultCardIfMissing(existingTypes, UpgradeData.UpgradeType.MagnetRange, "Tầm Hút", "Mở rộng bán kính tự động hút vàng và ngọc kinh nghiệm.", 5);
    }

    private void AddDefaultCardIfMissing(HashSet<UpgradeData.UpgradeType> existingTypes, UpgradeData.UpgradeType type, string title, string desc, int maxLv)
    {
        if (!existingTypes.Contains(type))
        {
            UpgradeData card = ScriptableObject.CreateInstance<UpgradeData>();
            card.upgradeType = type;
            card.title = title;
            card.description = desc;
            card.maxLevel = maxLv;
            card.icon = ProceduralSkillAssets.GetSkillIcon(type);
            allUpgrades.Add(card);
            existingTypes.Add(type);
        }
    }

    // Được gọi khi Player nhặt đủ EXP lên cấp
    public void RollUpgrades()
    {
        if (GameOverController.Instance != null && GameOverController.Instance.IsGameOver)
        {
            return;
        }

        if (levelUpPanel != null)
        {
            levelUpPanel.SetActive(true);
        }
        Time.timeScale = 0f;

        ExecuteRoll();
    }

    private void ExecuteRoll()
    {
        PassiveSkillController controller = PassiveSkillController.Instance;
        if (controller == null && PlayerStats.Instance != null)
        {
            controller = PlayerStats.Instance.GetComponent<PassiveSkillController>() ?? PlayerStats.Instance.gameObject.AddComponent<PassiveSkillController>();
        }

        int multishotLv = controller != null ? controller.GetSkillLevel(UpgradeData.UpgradeType.Multishot) : 0;
        int burstFireLv = controller != null ? controller.GetSkillLevel(UpgradeData.UpgradeType.BurstFire) : 0;

        // 1. Lọc danh sách kỹ năng hợp lệ theo các quy tắc
        List<UpgradeData> validPool = new List<UpgradeData>();

        for (int i = 0; i < allUpgrades.Count; i++)
        {
            UpgradeData card = allUpgrades[i];
            if (card == null) continue;

            // Bỏ qua các thẻ fallback trong pool chính
            if (card.upgradeType == UpgradeData.UpgradeType.SelectItem_Coin ||
                card.upgradeType == UpgradeData.UpgradeType.SelectItem_HealPack)
            {
                continue;
            }

            // Quy tắc 1: Loại bỏ kỹ năng đã max level
            if (controller != null && controller.IsMaxLevel(card.upgradeType))
            {
                continue;
            }

            // Quy tắc 2: Kỹ năng Độc Quyền (Exclusive Rule)
            // Nếu đã chọn Multishot (cấp > 0) -> Ẩn BurstFire
            if (card.upgradeType == UpgradeData.UpgradeType.BurstFire && multishotLv > 0)
            {
                continue;
            }
            // Nếu đã chọn BurstFire (cấp > 0) -> Ẩn Multishot
            if (card.upgradeType == UpgradeData.UpgradeType.Multishot && burstFireLv > 0)
            {
                continue;
            }

            validPool.Add(card);
        }

        // 2. Xáo trộn ngẫu nhiên danh sách hợp lệ
        for (int i = 0; i < validPool.Count; i++)
        {
            UpgradeData temp = validPool[i];
            int r = Random.Range(i, validPool.Count);
            validPool[i] = validPool[r];
            validPool[r] = temp;
        }

        // 3. Chọn 3 thẻ để hiển thị
        List<UpgradeData> displayCards = new List<UpgradeData>();
        int pickCount = Mathf.Min(3, validPool.Count);
        for (int i = 0; i < pickCount; i++)
        {
            displayCards.Add(validPool[i]);
        }

        // 4. CẤU TRÚC THẺ DỰ PHÒNG (Fallback Items) KHI CẠN POOL:
        // Nếu số lượng kỹ năng hợp lệ còn lại < 3 thẻ (hoặc bằng 0 khi đã max hết)
        while (displayCards.Count < 3)
        {
            // Luân phiên thêm Coin và HealPack
            if (displayCards.Count % 2 == 0)
            {
                displayCards.Add(fallbackCoinCard);
            }
            else
            {
                displayCards.Add(fallbackHealCard);
            }
        }

        // 5. Đổ dữ liệu vào 3 UpgradeCardUI
        for (int i = 0; i < upgradeCards.Length; i++)
        {
            if (i < displayCards.Count && displayCards[i] != null)
            {
                upgradeCards[i].gameObject.SetActive(true);
                UpgradeData selectedData = displayCards[i];

                // Xác định tên hiển thị kèm cấp độ: dạng chuẩn "Tên skill lv1", bỏ sạch ngoặc
                int curLv = controller != null ? controller.GetSkillLevel(selectedData.upgradeType) : 0;
                string cleanTitle = CleanSkillTitle(selectedData.title);

                string displayTitle = cleanTitle;
                if (selectedData.upgradeType != UpgradeData.UpgradeType.SelectItem_Coin &&
                    selectedData.upgradeType != UpgradeData.UpgradeType.SelectItem_HealPack)
                {
                    displayTitle = $"{cleanTitle} lv{curLv + 1}";
                }

                Sprite displayIcon = selectedData.icon != null ? selectedData.icon : ProceduralSkillAssets.GetSkillIcon(selectedData.upgradeType);
                string displayDesc = selectedData.GetDescriptionForLevel(curLv + 1);

                upgradeCards[i].SetCardData(
                    displayIcon,
                    displayTitle,
                    displayDesc,
                    () => ApplyUpgrade(selectedData)
                );
            }
            else
            {
                upgradeCards[i].gameObject.SetActive(false);
            }
        }
    }

    public void ApplyUpgrade(UpgradeData data)
    {
        if (data == null) return;

        // Áp dụng nâng cấp qua PassiveSkillController
        if (PassiveSkillController.Instance != null)
        {
            PassiveSkillController.Instance.ApplyUpgrade(data.upgradeType);
        }
        else if (PlayerStats.Instance != null)
        {
            PassiveSkillController ctrl = PlayerStats.Instance.GetComponent<PassiveSkillController>() ?? PlayerStats.Instance.gameObject.AddComponent<PassiveSkillController>();
            ctrl.ApplyUpgrade(data.upgradeType);
        }

        // Đóng panel và tiếp tục trận đấu
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.ResumeGame();
        }
        else
        {
            if (levelUpPanel != null) levelUpPanel.SetActive(false);
            Time.timeScale = 1f;
        }
    }

    private void OnRerollClicked()
    {
        if (currentRerollCount > 0)
        {
            currentRerollCount--;
            UpdateRerollUI();
            ExecuteRoll(); // Rút 3 thẻ ngẫu nhiên mới
        }
    }

    private void UpdateRerollUI()
    {
        if (rerollText != null)
        {
            rerollText.text = $"Làm mới ({currentRerollCount})";
        }

        if (btnReroll != null)
        {
            btnReroll.interactable = (currentRerollCount > 0);
        }
    }

    // Hàm public để rương loot cộng thêm lượt Reroll (nhận tối đa maxLimit)
    public bool AddReroll(int maxLimit)
    {
        if (currentRerollCount < maxLimit)
        {
            currentRerollCount++;
            UpdateRerollUI();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Làm sạch tên kỹ năng: loại bỏ mọi chú thích trong ngoặc đơn (...) và ngoặc vuông [...]
    /// Ví dụ: "Kiếm Bay (Orbit Swords)" -> "Kiếm Bay"
    /// </summary>
    public static string CleanSkillTitle(string rawTitle)
    {
        if (string.IsNullOrEmpty(rawTitle)) return "";
        string clean = System.Text.RegularExpressions.Regex.Replace(rawTitle, @"\s*[\(\[][^\)\]]*[\)\]]", "").Trim();
        return clean;
    }
}