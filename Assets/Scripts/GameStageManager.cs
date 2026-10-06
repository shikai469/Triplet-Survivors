using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameStageManager : MonoBehaviour
{
    public static GameStageManager Instance;

    [Header("UI References")]
    public TextMeshProUGUI countdownText;
    public Slider waveSlider;
    public TextMeshProUGUI killText; // Kéo Text của KillRow vào đây nếu muốn cập nhật số kill trên UI

    [Header("Stage Stats")]
    public int killCount = 0; // Biến lưu số quái đã tiêu diệt

    [Header("Stage Settings (Seconds)")]
    public float totalMatchTime = 60f; // Đang test 60s
    private float currentTime;

    [Header("Warning & Flash Settings")]
    public float warningThreshold = 15f; // 15s cuối bắt đầu nhấp nháy
    public Color normalColor = Color.white;
    public Color warningColor = new Color(1f, 0.2f, 0.2f, 1f); // Đỏ tươi
    public float flashSpeed = 4f; // Tốc độ nháy êm

    [Header("Wave Flags")]
    private bool midWaveTriggered = false;
    private bool bossWaveTriggered = false;

    [Header("Spawners & Boss")]
    public EnemySpawner enemySpawner;
    public GameObject bossPrefab;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        currentTime = totalMatchTime;

        if (waveSlider != null)
        {
            waveSlider.minValue = 0f;
            waveSlider.maxValue = totalMatchTime;
            waveSlider.value = 0f;
        }

        if (enemySpawner == null)
        {
            enemySpawner = FindObjectOfType<EnemySpawner>();
        }

        if (countdownText != null)
        {
            countdownText.color = normalColor;
        }

        UpdateKillUI();
    }

    // Hàm gọi từ EnemyHealth khi quái chết
    public void AddKill(int amount = 1)
    {
        killCount += amount;
        UpdateKillUI();
    }

    void UpdateKillUI()
    {
        if (killText != null)
        {
            killText.text = killCount.ToString();
        }
    }

    void Update()
    {
        if (currentTime > 0f)
        {
            currentTime = Mathf.Max(0f, currentTime - Time.deltaTime);
            UpdateProgressUI();
            CheckWaveEvents();
        }
        else
        {
            if (countdownText != null)
            {
                countdownText.text = "BOSS";
                float t = Mathf.PingPong(Time.time * flashSpeed, 1f);
                countdownText.color = Color.Lerp(normalColor, warningColor, t);
            }
        }
    }

    void UpdateProgressUI()
    {
        int minutes = Mathf.FloorToInt(currentTime / 60f);
        int seconds = Mathf.FloorToInt(currentTime % 60f);

        if (countdownText != null)
        {
            countdownText.text = string.Format("{0:00}:{1:00}", minutes, seconds);

            if (currentTime <= warningThreshold && currentTime > 0f)
            {
                float t = Mathf.PingPong(Time.time * flashSpeed, 1f);
                countdownText.color = Color.Lerp(normalColor, warningColor, t);
            }
            else
            {
                countdownText.color = normalColor;
            }
        }

        float timeElapsed = totalMatchTime - currentTime;
        if (waveSlider != null)
        {
            waveSlider.value = timeElapsed;
        }
    }

    void CheckWaveEvents()
    {
        float timeElapsed = totalMatchTime - currentTime;

        if (timeElapsed >= totalMatchTime * 0.5f && !midWaveTriggered)
        {
            midWaveTriggered = true;
            TriggerMidWaveSwarm();
        }

        if (currentTime <= 0f && !bossWaveTriggered)
        {
            bossWaveTriggered = true;
            TriggerBossFight();
        }
    }

    void TriggerMidWaveSwarm()
    {
        Debug.Log("<color=yellow>[CẢNH BÁO] ĐÀN QUÁI LỚN ĐANG TIẾP CẬN!</color>");
        if (enemySpawner != null)
        {
            enemySpawner.spawnInterval = Mathf.Max(0.3f, enemySpawner.spawnInterval * 0.5f);
            enemySpawner.maxEnemies += 20;
        }
    }

    void TriggerBossFight()
    {
        Debug.Log("<color=red>[BOSS XUẤT HIỆN] TIÊU DIỆT TRÙM ĐỂ CHIẾN THẮNG!</color>");

        if (enemySpawner != null)
        {
            enemySpawner.spawnInterval = 2.0f;
        }

        if (bossPrefab != null && PlayerStats.Instance != null)
        {
            Vector3 spawnPos = PlayerStats.Instance.transform.position + new Vector3(0, 8f, 0);
            Instantiate(bossPrefab, spawnPos, Quaternion.identity);
        }
    }
}