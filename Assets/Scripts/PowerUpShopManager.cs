using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

[System.Serializable]
public class ShopUpgradeItem
{
    public string upgradeKey;
    public string displayName;
    public Sprite icon;
    [TextArea(2, 3)]
    public string description;
    public int maxLevel = 5;
    public int[] costPerLevel;

    public int GetCurrentCost(int currentLevel)
    {
        if (costPerLevel != null && currentLevel >= 0 && currentLevel < costPerLevel.Length)
        {
            return costPerLevel[currentLevel];
        }
        return 200 + (currentLevel * 300);
    }
}

public class PowerUpShopManager : MonoBehaviour
{
    [Header("Top Bar")]
    public Button btnBack;
    public TextMeshProUGUI totalGoldText;
    public Button btnRefund;

    [Header("Grid Skill List")]
    public Transform contentGrid;
    public GameObject slotPrefab;
    public GameObject pipPrefab;
    public List<ShopUpgradeItem> upgradeItems = new List<ShopUpgradeItem>();

    [Header("Bottom Sheet Detail")]
    public GameObject bottomSheet;
    public Button backdropDismissBtn;
    public Button dragBarCloseBtn;
    public Image detailIcon;
    public TextMeshProUGUI detailName;
    public TextMeshProUGUI detailLv;
    public TextMeshProUGUI detailDesc;
    public TextMeshProUGUI costText;
    public Button btnBuy;
    public TextMeshProUGUI btnBuyText;

    private ShopUpgradeItem currentSelectedItem;
    private Dictionary<string, PowerUpSlotUI> slotUIElements = new Dictionary<string, PowerUpSlotUI>();

    // Quản lý rung nút khi bấm thiếu tiền
    private Coroutine shakeCoroutine;
    private Vector3 originalBtnPos;
    private bool posRecorded = false;

    void Awake()
    {
        if (upgradeItems == null || upgradeItems.Count == 0)
        {
            InitializeDefaultPowerUps();
        }
    }

    void Start()
    {
        if (btnBuy != null && !posRecorded)
        {
            originalBtnPos = btnBuy.transform.localPosition;
            posRecorded = true;
        }

        if (btnBack != null) btnBack.onClick.AddListener(OnBackToMenuClicked);
        if (btnRefund != null) btnRefund.onClick.AddListener(RefundAllUpgrades);

        if (backdropDismissBtn != null) backdropDismissBtn.onClick.AddListener(CloseBottomSheet);
        if (dragBarCloseBtn != null) dragBarCloseBtn.onClick.AddListener(CloseBottomSheet);
        if (btnBuy != null) btnBuy.onClick.AddListener(OnBuyClicked);

        CloseBottomSheet();
        BuildSkillGrid();
        UpdateGoldUI();
    }

    void InitializeDefaultPowerUps()
    {
        upgradeItems = new List<ShopUpgradeItem>()
        {
            new ShopUpgradeItem { upgradeKey = "Dmg", displayName = "Sát Thương", maxLevel = 5, costPerLevel = new int[] { 200, 400, 700, 1100, 1600 }, description = "Tăng 1 sát thương cơ bản mỗi cấp (tối đa +5)." },
            new ShopUpgradeItem { upgradeKey = "Defense", displayName = "Giáp", maxLevel = 3, costPerLevel = new int[] { 200, 700, 1300 }, description = "Tăng 1 giáp giảm sát thương nhận vào (tối đa +3)." },
            new ShopUpgradeItem { upgradeKey = "Health", displayName = "Máu Tối Đa", maxLevel = 3, costPerLevel = new int[] { 200, 400, 700 }, description = "Tăng 10 lượng máu tối đa mỗi cấp (tối đa +30)." },
            new ShopUpgradeItem { upgradeKey = "Healing", displayName = "Hồi Phục", maxLevel = 5, costPerLevel = new int[] { 200, 400, 700, 1100, 1600 }, description = "Hồi 0.1 máu mỗi giây (tối đa +0.5 HP/s)." },
            new ShopUpgradeItem { upgradeKey = "Extender", displayName = "Phạm Vi", maxLevel = 2, costPerLevel = new int[] { 500, 1100 }, description = "Tăng 5% diện tích đòn đánh diện rộng (tối đa +10%)." },
            new ShopUpgradeItem { upgradeKey = "Speed", displayName = "Tốc Độ Chạy", maxLevel = 5, costPerLevel = new int[] { 200, 400, 700, 1100, 1600 }, description = "Tăng 5% tốc độ di chuyển mỗi cấp (tối đa +25%)." },
            new ShopUpgradeItem { upgradeKey = "Magnet", displayName = "Tầm Hút", maxLevel = 2, costPerLevel = new int[] { 500, 1100 }, description = "Tăng 5% bán kính tự hút EXP & vàng (tối đa +10%)." },
            new ShopUpgradeItem { upgradeKey = "Luck", displayName = "May Mắn", maxLevel = 3, costPerLevel = new int[] { 200, 700, 1600 }, description = "Tăng 10% may mắn khi bốc thẻ bài hiếm (tối đa +30%)." },
            new ShopUpgradeItem { upgradeKey = "Growth", displayName = "Tăng EXP", maxLevel = 5, costPerLevel = new int[] { 200, 400, 700, 1100, 1600 }, description = "Tăng 3% lượng kinh nghiệm nhận được (tối đa +15%)." },
            new ShopUpgradeItem { upgradeKey = "Greed", displayName = "Thêm Vàng", maxLevel = 5, costPerLevel = new int[] { 200, 400, 700, 1100, 1600 }, description = "Tăng 2 vàng thu thập mỗi lần nhặt (tối đa +10)." },
            new ShopUpgradeItem { upgradeKey = "CritRate", displayName = "Tỉ Lệ Chí Mạng", maxLevel = 5, costPerLevel = new int[] { 200, 400, 700, 1100, 1600 }, description = "Tăng 3% tỉ lệ đánh chí mạng (tối đa +15%)." },
            new ShopUpgradeItem { upgradeKey = "CritDmg", displayName = "ST Chí Mạng", maxLevel = 5, costPerLevel = new int[] { 200, 400, 700, 1100, 1600 }, description = "Tăng 10% sát thương đòn chí mạng (tối đa +50%)." }
        };
    }

    void OnBackToMenuClicked()
    {
        SceneManager.LoadScene("MainMenu");
    }

    void BuildSkillGrid()
    {
        if (contentGrid == null || slotPrefab == null) return;

        for (int i = contentGrid.childCount - 1; i >= 0; i--)
        {
            Destroy(contentGrid.GetChild(i).gameObject);
        }
        slotUIElements.Clear();

        foreach (var item in upgradeItems)
        {
            GameObject slotObj = Instantiate(slotPrefab, contentGrid);
            slotObj.name = $"Slot_{item.upgradeKey}";

            PowerUpSlotUI slotUI = slotObj.GetComponent<PowerUpSlotUI>();
            if (slotUI != null)
            {
                slotUI.pipPrefab = pipPrefab;
                int currentLevel = GameDataManager.GetUpgradeLevel(item.upgradeKey);
                slotUI.Setup(item, currentLevel, () => OpenBottomSheet(item));
                slotUIElements[item.upgradeKey] = slotUI;
            }
        }
    }

    public void OpenBottomSheet(ShopUpgradeItem item)
    {
        currentSelectedItem = item;
        if (bottomSheet != null) bottomSheet.SetActive(true);

        if (detailIcon != null) detailIcon.sprite = item.icon;
        if (detailName != null) detailName.text = item.displayName;

        RefreshDetailPanel();
    }

    public void CloseBottomSheet()
    {
        StopShake();
        if (bottomSheet != null) bottomSheet.SetActive(false);
        currentSelectedItem = null;
    }

    void RefreshDetailPanel()
    {
        if (currentSelectedItem == null) return;

        int currentLevel = GameDataManager.GetUpgradeLevel(currentSelectedItem.upgradeKey);
        int totalGold = GameDataManager.GetTotalGold();

        // 1. Mô tả chi tiết
        if (detailDesc != null)
        {
            detailDesc.text = currentSelectedItem.description;
        }

        // 2. Text cấp độ
        if (detailLv != null)
        {
            if (currentLevel >= currentSelectedItem.maxLevel)
            {
                detailLv.text = $"Cấp độ: <color=#FFCC00>Lv. MAX</color>";
            }
            else
            {
                detailLv.text = $"Cấp độ: Lv. {currentLevel} -> <color=#00FFAA>Lv. {currentLevel + 1}</color>";
            }
        }

        // 3. Trạng thái giá và nút mua
        StopShake();

        if (currentLevel >= currentSelectedItem.maxLevel)
        {
            if (costText != null) costText.text = "MAX";
            if (btnBuy != null) btnBuy.interactable = false;
            if (btnBuyText != null)
            {
                btnBuyText.text = "Tối Đa";
                btnBuyText.color = new Color(0.6f, 0.6f, 0.6f, 1f);
            }
        }
        else
        {
            int cost = currentSelectedItem.GetCurrentCost(currentLevel);
            if (costText != null) costText.text = cost.ToString();

            // Luôn để nút mở để bắt sự kiện người chơi bấm thử
            if (btnBuy != null) btnBuy.interactable = true;

            bool canAfford = totalGold >= cost;

            if (btnBuyText != null)
            {
                if (canAfford)
                {
                    btnBuyText.text = "Nâng Cấp";
                    btnBuyText.color = Color.white;
                }
                else
                {
                    btnBuyText.text = "Thiếu Vàng";
                    btnBuyText.color = new Color(1f, 0.35f, 0.35f, 1f); // Màu chữ đỏ cam tĩnh
                }
            }
        }
    }

    void OnBuyClicked()
    {
        if (currentSelectedItem == null) return;

        int currentLevel = GameDataManager.GetUpgradeLevel(currentSelectedItem.upgradeKey);
        if (currentLevel >= currentSelectedItem.maxLevel) return;

        int cost = currentSelectedItem.GetCurrentCost(currentLevel);
        int totalGold = GameDataManager.GetTotalGold();

        if (totalGold >= cost)
        {
            // Mua thành công
            GameDataManager.AddGold(-cost);
            GameDataManager.SetUpgradeLevel(currentSelectedItem.upgradeKey, currentLevel + 1);

            UpdateGoldUI();
            RefreshDetailPanel();
            if (slotUIElements.ContainsKey(currentSelectedItem.upgradeKey))
            {
                slotUIElements[currentSelectedItem.upgradeKey].UpdateLevel(currentLevel + 1);
            }
        }
        else
        {
            // Bấm vào khi thiếu tiền -> Chỉ rung nhẹ nút, chữ đứng yên
            if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
            shakeCoroutine = StartCoroutine(ShakeButtonRoutine());
        }
    }

    private IEnumerator ShakeButtonRoutine()
    {
        if (btnBuy == null) yield break;

        float duration = 0.3f;
        float elapsed = 0f;
        float magnitude = 9f; // Biên độ rung ngang

        while (elapsed < duration)
        {
            float offsetX = Random.Range(-1f, 1f) * magnitude;
            btnBuy.transform.localPosition = originalBtnPos + new Vector3(offsetX, 0, 0);

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        StopShake();
    }

    private void StopShake()
    {
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            shakeCoroutine = null;
        }
        if (btnBuy != null && posRecorded)
        {
            btnBuy.transform.localPosition = originalBtnPos;
        }
    }

    void RefundAllUpgrades()
    {
        int refundAmount = 0;

        foreach (var item in upgradeItems)
        {
            int level = GameDataManager.GetUpgradeLevel(item.upgradeKey);
            for (int lv = 0; lv < level; lv++)
            {
                refundAmount += item.GetCurrentCost(lv);
            }
            GameDataManager.SetUpgradeLevel(item.upgradeKey, 0);
        }

        GameDataManager.AddGold(refundAmount);

        UpdateGoldUI();
        BuildSkillGrid();
        CloseBottomSheet();
        Debug.Log($"[REFUND] Đã hoàn trả {refundAmount} Vàng!");
    }

    void UpdateGoldUI()
    {
        if (totalGoldText != null)
        {
            totalGoldText.text = GameDataManager.GetTotalGold().ToString();
        }
    }
}