using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Quản lý Cơ Chế Tụ Điểm Xả Chiêu (Overdrive / Awakening)
/// Tích lũy Kill Point khi diệt quái. Khi đầy ngưỡng, tự động kích hoạt Xả Chiêu cường hóa luồng đạn chính!
/// </summary>
public class OverdriveManager : MonoBehaviour
{
    public static OverdriveManager Instance { get; private set; }

    [Header("Overdrive Settings")]
    [Tooltip("Số điểm tiêu diệt quái cần tích tụ để kích hoạt Xả Chiêu")]
    public int killThreshold = 30;
    public int currentKillPoints = 0;
    public float overdriveDuration = 5.0f; // Thời gian hiệu lực trạng thái Xả Chiêu

    [Header("Status")]
    public bool isOverdriveActive = false;
    private float activeTimer = 0f;

    [Header("Optional UI References")]
    public Slider overdriveBar;
    public TextMeshProUGUI overdriveStatusText;

    private GameObject overdriveAura;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(gameObject);
    }

    void Start()
    {
        UpdateUI();
    }

    /// <summary>
    /// Được gọi mỗi khi 1 quái vật bị hạ gục
    /// </summary>
    public void OnEnemyKilled()
    {
        if (isOverdriveActive) return; // Đang trong trạng thái xả chiêu thì không tích thêm

        currentKillPoints++;
        UpdateUI();

        if (currentKillPoints >= killThreshold)
        {
            ActivateOverdrive();
        }
    }

    private void ActivateOverdrive()
    {
        isOverdriveActive = true;
        activeTimer = overdriveDuration;

        Debug.Log("<color=orange><b>[XẢ CHIÊU - OVERDRIVE ĐÃ KÍCH HOẠT!]</b></color> Luồng đạn chính được cường hóa cực hạn!");

        // Tạo hiệu ứng hào quang lửa quanh Player
        CreateAuraVFX();

        if (PlayerAttack.Instance != null)
        {
            PlayerAttack.Instance.SetOverdriveMode(true);
        }

        UpdateUI();
    }

    void Update()
    {
        if (isOverdriveActive)
        {
            activeTimer -= Time.deltaTime;

            if (overdriveBar != null)
            {
                overdriveBar.maxValue = overdriveDuration;
                overdriveBar.value = activeTimer;
            }

            if (activeTimer <= 0f)
            {
                EndOverdrive();
            }
        }
    }

    private void EndOverdrive()
    {
        isOverdriveActive = false;
        currentKillPoints = 0;

        if (overdriveAura != null)
        {
            overdriveAura.SetActive(false);
        }

        if (PlayerAttack.Instance != null)
        {
            PlayerAttack.Instance.SetOverdriveMode(false);
        }

        Debug.Log("[OVERDRIVE] Đã kết thúc trạng thái Xả Chiêu. Bắt đầu tích lũy lại điểm!");
        UpdateUI();
    }

    private void CreateAuraVFX()
    {
        if (PlayerStats.Instance == null) return;

        if (overdriveAura == null)
        {
            overdriveAura = new GameObject("OverdriveAura");
            overdriveAura.transform.SetParent(PlayerStats.Instance.transform);
            overdriveAura.transform.localPosition = Vector3.zero;
            overdriveAura.transform.localScale = new Vector3(1.6f, 1.6f, 1f);

            SpriteRenderer sr = overdriveAura.AddComponent<SpriteRenderer>();
            sr.sprite = ProceduralSkillAssets.GetSprite("EnergyShieldCircle");
            sr.color = new Color(1f, 0.4f, 0.1f, 0.75f); // Hào quang đỏ lửa
            sr.sortingOrder = 9;
        }

        overdriveAura.SetActive(true);
    }

    private void UpdateUI()
    {
        if (overdriveBar != null)
        {
            if (!isOverdriveActive)
            {
                overdriveBar.maxValue = killThreshold;
                overdriveBar.value = currentKillPoints;
            }
        }

        if (overdriveStatusText != null)
        {
            if (isOverdriveActive)
            {
                overdriveStatusText.text = $"XẢ CHIÊU: {activeTimer:F1}s";
            }
            else
            {
                overdriveStatusText.text = $"Nộ: {currentKillPoints}/{killThreshold}";
            }
        }
    }
}
