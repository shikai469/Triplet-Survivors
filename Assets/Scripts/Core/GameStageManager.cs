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
    private int lastDisplayedSecond = -1;

    [Header("Spawners & Boss")]
    public EnemySpawner enemySpawner;
    public GameObject bossPrefab;

    [Header("Boss Mèo Chuối & Fire Ring Arena")]
    public GameObject bossMeoChuoiPrefab;
    public bool forceSpawnMeoChuoi = true; // Kích hoạt Boss Mèo Chuối làm Boss cuối
    public float fireRingRadius = 8.5f;
    private FireRingArena activeFireRing = null;

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

        // Tải Boss Mèo Chuối nếu chưa được gán
        if (bossMeoChuoiPrefab == null)
        {
#if UNITY_EDITOR
            bossMeoChuoiPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Boss-MeoChuoi.prefab");
#endif
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
                countdownText.text = "BOSS MÈO CHUỐI";
                float t = Mathf.PingPong(Time.time * flashSpeed, 1f);
                countdownText.color = Color.Lerp(normalColor, warningColor, t);
            }
        }
    }

    void UpdateStandardProgressUI()
    {
        int totalSec = Mathf.FloorToInt(currentTime);
        if (totalSec != lastDisplayedSecond)
        {
            lastDisplayedSecond = totalSec;
            int minutes = totalSec / 60;
            int seconds = totalSec % 60;
            if (countdownText != null)
            {
                countdownText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
            }
        }

        if (countdownText != null)
        {
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

        // Hết thời gian đếm ngược: Boss xuất hiện!
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

        int totalSec = Mathf.FloorToInt(currentTime);
        if (totalSec != lastDisplayedSecond)
        {
            lastDisplayedSecond = totalSec;
            int minutes = totalSec / 60;
            int seconds = totalSec % 60;
            if (countdownText != null)
            {
                countdownText.text = string.Format("[VÔ TẬN] {0:00}:{1:00}", minutes, seconds);
                countdownText.color = normalColor;
            }
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

    /// <summary>
    /// Kích hoạt đại chiến Boss: Despawn hết quái thường, tạo Vòng Tròn Lửa giam giữ và triệu hồi Boss Mèo Chuối
    /// </summary>
    void TriggerBossFight()
    {
        Debug.Log("<color=red>[BOSS XUẤT HIỆN] BOSS MÈO CHUỐI ĐÃ ĐỔ BỘ! TIÊU DIỆT TRÙM ĐỂ CHIẾN THẮNG!</color>");

        // 1. Tắt hoàn toàn Spawner để không sinh thêm quái con
        if (enemySpawner != null)
        {
            enemySpawner.StopAllCoroutines();
            enemySpawner.enabled = false;
        }

        // 2. Despawn sạch toàn bộ quái vật thường đang sống trên bản đồ về Object Pool
        var activeEnemiesList = new List<EnemyHealth>(EnemyHealth.ActiveEnemies);
        for (int i = 0; i < activeEnemiesList.Count; i++)
        {
            if (activeEnemiesList[i] != null && !activeEnemiesList[i].isBoss)
            {
                if (ObjectPool.Instance != null)
                {
                    ObjectPool.Instance.Return(activeEnemiesList[i].gameObject);
                }
                else
                {
                    Destroy(activeEnemiesList[i].gameObject);
                }
            }
        }

        // 3. Chọn Prefab Boss (Ưu tiên Boss Mèo Chuối theo yêu cầu)
        GameObject targetBossPrefab = bossPrefab;
        if (forceSpawnMeoChuoi && bossMeoChuoiPrefab != null)
        {
            targetBossPrefab = bossMeoChuoiPrefab;
        }
        else if (targetBossPrefab == null && bossMeoChuoiPrefab != null)
        {
            targetBossPrefab = bossMeoChuoiPrefab;
        }

        if (targetBossPrefab != null && PlayerStats.Instance != null)
        {
            Vector3 playerPos = PlayerStats.Instance.transform.position;

            // Tính toán tâm đấu trường vòng tròn lửa (nếu có BrotatoArena thì kẹp an toàn không vượt quá tường)
            Vector3 arenaCenter = playerPos;
            if (BrotatoArena.Instance != null)
            {
                arenaCenter = BrotatoArena.Instance.ClampPosition(playerPos, padding: fireRingRadius * 0.85f);
            }
            arenaCenter.z = 0f;

            // Đặt Boss xuất hiện ở mép trên của vòng tròn đấu trường (cách người chơi khoảng 4.5m)
            Vector3 spawnPos = arenaCenter + new Vector3(0f, 4.5f, 0f);

            currentActiveBoss = ObjectPool.Instance != null
                ? ObjectPool.Instance.Get(targetBossPrefab, spawnPos, Quaternion.identity)
                : Instantiate(targetBossPrefab, spawnPos, Quaternion.identity);

            // 4. Kích hoạt Đấu trường Vòng Tròn Lửa (Không cho Người chơi và Boss thoát ra)
            GameObject ringObj = new GameObject("[FireRingArena]");
            activeFireRing = ringObj.AddComponent<FireRingArena>();
            activeFireRing.ActivateArena(arenaCenter, PlayerStats.Instance.transform, currentActiveBoss.transform, fireRingRadius);

            // 5. Cân chỉnh Máu Boss trâu bò đánh tầm hơn 1 phút mới hạ gục (dựa theo DPS giai đoạn cuối trận ~100-120 DPS)
            float bossHp = 7800f; // Mặc định chế độ Normal (~1 phút 15 giây chiến đấu liên tục)
            int bossArmor = 3;
            int bossDmg = 28;

            if (activeConfig != null)
            {
                switch (activeConfig.level)
                {
                    case DifficultyLevel.Easy:
                        bossHp = 4200f;  // Dễ: ~60 giây với DPS trung bình 70
                        bossArmor = 2;
                        bossDmg = 12;
                        break;
                    case DifficultyLevel.Normal:
                        bossHp = 7800f;  // Bình thường: ~70-80 giây
                        bossArmor = 3;
                        bossDmg = 28;
                        break;
                    case DifficultyLevel.Hard:
                        bossHp = 18000f; // Khó: Máu cực trâu, thử thách build đồ tối thượng
                        bossArmor = 5;
                        bossDmg = 65;
                        break;
                    case DifficultyLevel.Endless:
                        bossHp = 9500f;
                        bossArmor = 4;
                        bossDmg = 40;
                        break;
                }
            }

            EnemyHealth bossHealth = currentActiveBoss.GetComponent<EnemyHealth>();
            if (bossHealth != null)
            {
                bossHealth.InitStats(bossHp, extraArmor: bossArmor, knockbackRes: 0f, boss: true);

                // 6. Hiển thị Thanh Máu Boss hoành tráng trên đỉnh màn hình
                BossHealthBarUI.Show(bossHealth, "MÈO CHUỐI");
            }

            EnemyMovement bossMove = currentActiveBoss.GetComponent<EnemyMovement>();
            if (bossMove != null)
            {
                bossMove.InitMovement(1.35f, bossDmg, 0f);
            }
        }
        else
        {
            // Trường hợp hy hữu không có Boss: Sống sót qua hết giờ là chiến thắng
            TriggerStageVictory();
        }
    }

    void TriggerEndlessBossWave()
    {
        if (enemySpawner != null && (bossMeoChuoiPrefab != null || bossPrefab != null))
        {
            Debug.Log("<color=purple>[VÔ TẬN - ĐỢT TRÙM] MÈO CHUỐI ĐẠI ĐẾ XUẤT HIỆN!</color>");
            float endlessHpScale = 1.0f + (currentTime / 60f) * 0.5f;
            GameObject bPrefab = bossMeoChuoiPrefab ?? bossPrefab;
            enemySpawner.SpawnBoss(bPrefab, endlessHpScale);
        }
    }

    /// <summary>
    /// Nhận thông báo khi Boss bị tiêu diệt
    /// </summary>
    public void OnBossDefeated(EnemyHealth boss)
    {
        // 1. Tắt Vòng Tròn Lửa & Thanh Máu Boss
        if (activeFireRing != null)
        {
            activeFireRing.DeactivateArena();
        }
        BossHealthBarUI.Hide();

        if (isEndlessMode)
        {
            Debug.Log("<color=green>[VÔ TẬN] ĐÃ TIÊU DIỆT BOSS! TIẾP TỤC SINH TỒN...</color>");
            if (PlayerStats.Instance != null)
            {
                int bonusGold = Mathf.RoundToInt(50 * activeConfig.goldMultiplier);
                PlayerStats.Instance.AddGold(bonusGold);
            }
        }
        else
        {
            // Trong chế độ thường: Tiêu diệt Boss Mèo Chuối = CHIẾN THẮNG GAME (STAGE CLEAR)!
            Debug.Log("<color=green>[CHIẾN THẮNG] TRÙM CUỐI MÈO CHUỐI ĐÃ BỊ HẠ GỤC!</color>");
            StartCoroutine(VictorySequenceRoutine());
        }
    }

    private IEnumerator VictorySequenceRoutine()
    {
        // Chờ 1.5 giây để hiệu ứng tan biến và người chơi kịp nhìn thấy chiến lợi phẩm rơi ra
        yield return new WaitForSeconds(1.5f);
        TriggerStageVictory();
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
