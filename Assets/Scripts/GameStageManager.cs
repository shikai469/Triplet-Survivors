using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameStageManager : MonoBehaviour
{
    public static GameStageManager Instance;

    [Header("UI References")]
    public TextMeshProUGUI countdownText;
    public Slider waveSlider;
    public TextMeshProUGUI killText;               // Hiển thị số lượng quái tiêu diệt
    public TextMeshProUGUI difficultyBadgeText;    // (Tùy chọn) Hiển thị tên chế độ: DỄ / BÌNH THƯỜNG / KHÓ / VÔ TẬN

    [Header("Difficulty Configurations")]
    public List<DifficultyConfig> customConfigs = new List<DifficultyConfig>(); // Tùy biến trên Inspector nếu muốn
    [HideInInspector]
    public DifficultyConfig activeConfig;
    [HideInInspector]
    public bool isEndlessMode = false;

    [Header("Stage Stats")]
    public int killCount = 0;

    [Header("Stage Timing")]
    public float totalMatchTime = 600f; // Thời lượng đếm ngược (nếu không phải Endless)
    private float currentTime;
    private float nextEndlessBossTime = 120f;

    [Header("Warning & Flash Settings")]
    public float warningThreshold = 20f;
    public Color normalColor = Color.white;
    public Color warningColor = new Color(1f, 0.2f, 0.2f, 1f); // Đỏ tươi
    public float flashSpeed = 4f;

    [Header("Wave & Boss Flags")]
    private bool midWaveTriggered = false;
    private bool bossWaveTriggered = false;
    private GameObject currentActiveBoss = null;

    [Header("Spawners & Boss")]
    public EnemySpawner enemySpawner;
    public GameObject bossPrefab;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        // 1. Lấy độ khó đã được chọn từ LevelSelectManager thông qua GameDataManager
        DifficultyLevel selectedDiff = GameDataManager.GetSelectedDifficulty();

        // 2. Tìm cấu hình tương ứng (Ưu tiên Inspector custom configs, nếu không có thì lấy cấu hình chuẩn)
        activeConfig = null;
        if (customConfigs != null && customConfigs.Count > 0)
        {
            activeConfig = customConfigs.Find(c => c.level == selectedDiff);
        }

        if (activeConfig == null)
        {
            activeConfig = DifficultyConfig.GetDefault(selectedDiff);
        }

        isEndlessMode = activeConfig.isEndless;

        // Cập nhật Boss Prefab nếu cấu hình có ghi đè
        if (activeConfig.bossPrefabOverride != null)
        {
            bossPrefab = activeConfig.bossPrefabOverride;
        }

        // Cập nhật nhãn tên độ khó trên UI (nếu có)
        if (difficultyBadgeText != null)
        {
            difficultyBadgeText.text = activeConfig.difficultyName;
        }

        // 3. Khởi tạo Spawner với cấu hình độ khó
        if (enemySpawner == null)
        {
            enemySpawner = FindAnyObjectByType<EnemySpawner>();
        }

        if (enemySpawner != null)
        {
            enemySpawner.InitializeSpawner(activeConfig);
        }

        // 4. Áp dụng hệ số kinh tế & thưởng vào PlayerStats
        if (PlayerStats.Instance != null)
        {
            // Tăng tỷ lệ EXP theo độ khó
            PlayerStats.Instance.expBonus += (activeConfig.expMultiplier - 1.0f);
        }

        // 5. Thiết lập đồng hồ trận đấu
        if (isEndlessMode)
        {
            currentTime = 0f; // Đếm tăng dần (Stopwatch)
            nextEndlessBossTime = activeConfig.endlessBossInterval;

            if (waveSlider != null)
            {
                waveSlider.minValue = 0f;
                waveSlider.maxValue = activeConfig.endlessBossInterval;
                waveSlider.value = 0f;
            }
        }
        else
        {
            totalMatchTime = activeConfig.stageDuration;
            currentTime = totalMatchTime;
            warningThreshold = activeConfig.warningThreshold;

            if (waveSlider != null)
            {
                waveSlider.minValue = 0f;
                waveSlider.maxValue = totalMatchTime;
                waveSlider.value = 0f;
            }
        }

        if (countdownText != null)
        {
            countdownText.color = normalColor;
        }

        UpdateKillUI();
        Debug.Log($"[STAGE MANAGER] Khởi động Scene thành công! Chế độ: {activeConfig.difficultyName} | Endless: {isEndlessMode} | Thời lượng: {totalMatchTime}s");
    }

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
        if (isEndlessMode)
        {
            UpdateEndlessMode();
        }
        else
        {
            UpdateStandardMode();
        }
    }

    // --- CƠ CHẾ ĐẾM GIỜ & SỰ KIỆN CHO CHẾ ĐỘ THƯỜNG (DỄ, BÌNH THƯỜNG, KHÓ) ---
    void UpdateStandardMode()
    {
        if (currentTime > 0f)
        {
            currentTime = Mathf.Max(0f, currentTime - Time.deltaTime);
            UpdateStandardProgressUI();
            CheckStandardWaveEvents();
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

    void UpdateStandardProgressUI()
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

    void CheckStandardWaveEvents()
    {
        float timeElapsed = totalMatchTime - currentTime;

        // Kích hoạt Swarm bầy quái ở nửa trận
        if (timeElapsed >= totalMatchTime * 0.5f && !midWaveTriggered)
        {
            midWaveTriggered = true;
            TriggerMidWaveSwarm();
        }

        // Hết thời gian: Boss xuất hiện!
        if (currentTime <= 0f && !bossWaveTriggered)
        {
            bossWaveTriggered = true;
            TriggerBossFight();
        }
    }

    // --- CƠ CHẾ SINH TỒN VÔ HẠN CHO CHẾ ĐỘ VÔ TẬN (ENDLESS) ---
    void UpdateEndlessMode()
    {
        currentTime += Time.deltaTime; // Thời gian sinh tồn tăng dần

        int minutes = Mathf.FloorToInt(currentTime / 60f);
        int seconds = Mathf.FloorToInt(currentTime % 60f);

        if (countdownText != null)
        {
            countdownText.text = string.Format("[VÔ TẬN] {0:00}:{1:00}", minutes, seconds);
            countdownText.color = normalColor;
        }

        // Thanh tiến độ hiển thị chu kỳ tới lượt Boss tiếp theo
        if (waveSlider != null && activeConfig.endlessBossInterval > 0f)
        {
            float cycleProgress = currentTime % activeConfig.endlessBossInterval;
            waveSlider.value = cycleProgress;
        }

        // Triệu hồi Boss định kỳ mỗi chu kỳ (ví dụ mỗi 2 phút)
        if (currentTime >= nextEndlessBossTime)
        {
            nextEndlessBossTime += activeConfig.endlessBossInterval;
            TriggerEndlessBossWave();
        }
    }

    void TriggerMidWaveSwarm()
    {
        Debug.Log("<color=yellow>[CẢNH BÁO] ĐÀN QUÁI LỚN ĐANG TIẾP CẬN THEO ĐỢT SWARM!</color>");
        if (enemySpawner != null)
        {
            enemySpawner.spawnInterval = Mathf.Max(0.25f, enemySpawner.spawnInterval * 0.5f);
            enemySpawner.maxEnemies += 25;
        }
    }

    void TriggerBossFight()
    {
        Debug.Log("<color=red>[BOSS XUẤT HIỆN] TIÊU DIỆT TRÙM ĐỂ CHIẾN THẮNG MÀN CHƠI!</color>");

        if (enemySpawner != null)
        {
            enemySpawner.spawnInterval = 2.0f; // Giảm bớt quái con để tập trung đánh Boss
        }

        if (bossPrefab != null && PlayerStats.Instance != null)
        {
            Vector3 spawnPos = PlayerStats.Instance.transform.position + new Vector3(0, 8f, 0);
            currentActiveBoss = Instantiate(bossPrefab, spawnPos, Quaternion.identity);

            // Gán thông số Boss
            EnemyHealth bossHealth = currentActiveBoss.GetComponent<EnemyHealth>();
            if (bossHealth != null)
            {
                float bossHp = 40f * activeConfig.baseHpMultiplier;
                bossHealth.InitStats(bossHp, extraArmor: 2, knockbackRes: 0f, boss: true);
            }

            EnemyMovement bossMove = currentActiveBoss.GetComponent<EnemyMovement>();
            if (bossMove != null)
            {
                bossMove.InitMovement(1.2f, activeConfig.maxDamage * 2, 0f);
            }
        }
        else
        {
            // Trường hợp không có BossPrefab: Người chơi sống sót qua hết giờ là chiến thắng!
            TriggerStageVictory();
        }
    }

    void TriggerEndlessBossWave()
    {
        if (enemySpawner != null && bossPrefab != null)
        {
            Debug.Log("<color=purple>[VÔ TẬN - ĐỢT TRÙM] MỘT THỰC THỂ KHỦNG BỐ XUẤT HIỆN!</color>");
            float endlessHpScale = 1.0f + (currentTime / 60f) * 0.5f; // Máu Boss tăng dần theo thời gian sinh tồn
            enemySpawner.SpawnBoss(bossPrefab, endlessHpScale);
        }
    }

    // Hàm nhận thông báo khi một Boss bị tiêu diệt
    public void OnBossDefeated(EnemyHealth boss)
    {
        if (isEndlessMode)
        {
            Debug.Log("<color=green>[VÔ TẬN] ĐÃ TIÊU DIỆT BOSS! TIẾP TỤC SINH TỒN...</color>");
            // Thưởng thêm vàng cho player khi diệt boss vô tận
            if (PlayerStats.Instance != null)
            {
                int bonusGold = Mathf.RoundToInt(50 * activeConfig.goldMultiplier);
                PlayerStats.Instance.AddGold(bonusGold);
            }
        }
        else
        {
            // Trong chế độ thường: Tiêu diệt Boss = CHIẾN THẮNG (STAGE CLEAR)!
            Debug.Log("<color=green>[CHIẾN THẮNG] TRÙM CUỐI ĐÃ BỊ HẠ GỤC!</color>");
            TriggerStageVictory();
        }
    }

    void TriggerStageVictory()
    {
        int totalGoldCollected = 0;
        if (PlayerStats.Instance != null)
        {
            totalGoldCollected = PlayerStats.Instance.goldCount;
            GameDataManager.AddGold(totalGoldCollected);
        }

        float surviveTime = Time.timeSinceLevelLoad;

        if (GameOverController.Instance != null)
        {
            GameOverController.Instance.TriggerStageClear(killCount, totalGoldCollected, surviveTime);
        }
    }
}