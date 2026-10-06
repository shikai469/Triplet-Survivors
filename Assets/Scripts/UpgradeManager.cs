using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance;

    [Header("Dữ liệu gốc")]
    public List<UpgradeData> allUpgrades;

    [Header("Giao diện UI Thẻ (Card_0, Card_1, Card_2)")]
    public GameObject levelUpPanel;
    public UpgradeCardUI[] upgradeCards; // Kéo 3 object Card_0, Card_1, Card_2 vào đây

    [Header("Cơ chế Reroll (Đổi lại)")]
    public Button btnReroll;            // Kéo nút Btn_Reroll vào đây
    public TextMeshProUGUI rerollText;  // Kéo Text (TMP) của Btn_Reroll vào đây
    public int maxRerollCount = 1;      // Số lần Reroll mặc định mỗi trận
    private int currentRerollCount;

    private List<UpgradeData> currentShuffledPool = new List<UpgradeData>();

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
    }

    // Được gọi khi Player nhặt đủ EXP lên cấp
    public void RollUpgrades()
    {
        // 1. Nếu đã Game Over thì TUYỆT ĐỐI không mở bảng nâng cấp nữa
        if (GameOverController.Instance != null && GameOverController.Instance.IsGameOver)
        {
            return;
        }

        if (levelUpPanel != null)
        {
            levelUpPanel.SetActive(true);
            // ĐÃ XÓA dòng SetAsLastSibling() để không đè lên GameOverPanel nữa!
        }
        Time.timeScale = 0f;

        ExecuteRoll();
    }

    private void ExecuteRoll()
    {
        if (allUpgrades == null || allUpgrades.Count == 0) return;

        // Xáo trộn ngẫu nhiên danh sách thẻ
        currentShuffledPool = new List<UpgradeData>(allUpgrades);
        for (int i = 0; i < currentShuffledPool.Count; i++)
        {
            UpgradeData temp = currentShuffledPool[i];
            int randomIndex = Random.Range(i, currentShuffledPool.Count);
            currentShuffledPool[i] = currentShuffledPool[randomIndex];
            currentShuffledPool[randomIndex] = temp;
        }

        // Đổ dữ liệu vào 3 UpgradeCardUI
        for (int i = 0; i < upgradeCards.Length; i++)
        {
            if (i < currentShuffledPool.Count)
            {
                upgradeCards[i].gameObject.SetActive(true);
                UpgradeData selectedData = currentShuffledPool[i];

                // Gán icon, title, desc và sự kiện click
                upgradeCards[i].SetCardData(
                    selectedData.icon,
                    selectedData.title,
                    selectedData.description,
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
        if (PlayerStats.Instance != null)
        {
            switch (data.upgradeType)
            {
                case UpgradeData.UpgradeType.MoveSpeed:
                    PlayerStats.Instance.UpgradeMoveSpeed();
                    break;
                case UpgradeData.UpgradeType.MagnetRange:
                    PlayerStats.Instance.UpgradeMagnet();
                    break;
                case UpgradeData.UpgradeType.FireRate:
                    PlayerStats.Instance.UpgradeFireRate();
                    break;
            }

            // Gọi ResumeGame để đóng panel, tiếp tục thời gian và đồng bộ lại thanh XP
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
        return false; // Trả về false nếu đã đạt max
    }
}