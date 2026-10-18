using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject enemyPrefab;
    public GameObject[] enemyPrefabs; // Danh sách các loại quái để spawn ngẫu nhiên (nếu có)
    public Transform player;
    public float spawnInterval = 1.0f;
    public float spawnDistance = 14.0f; // Bán kính ngoài mép camera
    public int maxEnemies = 40;

    [Header("Loot Chest Settings")]
    public GameObject lootChestPrefab;
    public float chestSpawnInterval = 45f;
    public float chestFirstSpawnDelay = 5f; // Rương đầu tiên sinh sau 5s để test thấy ngay (sau đó lặp lại mỗi 45s)
    public float chestSpawnDistanceMax = 12f;
    public float chestSpawnDistanceMin = 5f;

    [Header("Wave Progression")]
    public float gameTime = 0f;
    public int currentWave = 1;

    [Header("Active Difficulty Config")]
    public DifficultyConfig config;

    void Start()
    {
        if (player == null && PlayerStats.Instance != null)
        {
            player = PlayerStats.Instance.transform;
        }

        // Nếu chưa được khởi tạo từ GameStageManager thì nạp cấu hình mặc định
        if (config == null)
        {
            DifficultyLevel currentDiff = GameDataManager.GetSelectedDifficulty();
            InitializeSpawner(DifficultyConfig.GetDefault(currentDiff));
        }

        StartCoroutine(SpawnRoutine());
        StartCoroutine(SpawnChestRoutine());
    }

    public void InitializeSpawner(DifficultyConfig difficultyConfig)
    {
        config = difficultyConfig;
        spawnInterval = config.baseSpawnInterval;
        maxEnemies = config.baseMaxEnemies;
        Debug.Log($"[SPAWNER] Nạp cấu hình: {config.difficultyName} | Interval: {spawnInterval}s | Max: {maxEnemies}");
    }

    void Update()
    {
        gameTime += Time.deltaTime;

        if (config != null && config.waveDuration > 0f)
        {
            currentWave = 1 + Mathf.FloorToInt(gameTime / config.waveDuration);
        }
        else
        {
            currentWave = 1 + Mathf.FloorToInt(gameTime / 30f);
        }

        // Tăng tiến độ khó theo thời gian thực (Đặc biệt cho Endless hoặc late wave)
        if (config != null && config.isEndless)
        {
            // Rút ngắn dần thời gian chờ spawn theo từng phút
            float minutesPassed = gameTime / 60f;
            spawnInterval = Mathf.Max(config.minSpawnInterval, config.baseSpawnInterval - (minutesPassed * 0.08f));

            // Nới rộng trần quái tối đa trên sân
            maxEnemies = Mathf.Min(config.maxEnemiesCap, config.baseMaxEnemies + Mathf.FloorToInt(minutesPassed * 12));
        }
    }

    IEnumerator SpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            if (player == null)
            {
                if (PlayerStats.Instance != null) player = PlayerStats.Instance.transform;
                continue;
            }

            int currentEnemyCount = EnemyHealth.ActiveEnemies.Count;
            if (currentEnemyCount >= maxEnemies) continue;

            SpawnEnemy();
        }
    }

    IEnumerator SpawnChestRoutine()
    {
        // 1. Chờ nhịp rương đầu tiên (5 giây) để bạn test thấy ngay lập tức
        yield return new WaitForSeconds(chestFirstSpawnDelay);

        if (player == null && PlayerStats.Instance != null) player = PlayerStats.Instance.transform;
        if (player != null && lootChestPrefab != null)
        {
            SpawnChest();
        }

        // 2. Sau đó tiếp tục lặp lại theo chu kỳ chestSpawnInterval (45 giây)
        while (true)
        {
            yield return new WaitForSeconds(chestSpawnInterval);

            if (player == null && PlayerStats.Instance != null) player = PlayerStats.Instance.transform;
            if (player == null || lootChestPrefab == null) continue;

            SpawnChest();
        }
    }

    void SpawnEnemy()
    {
        // 1. Tính toán vị trí sinh quái
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector3 spawnOffset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * spawnDistance;
        Vector3 spawnPos;

        if (BrotatoArena.Instance != null)
        {
            // Chuẩn phong cách Brotato: 75% quái tràn vào từ 4 cạnh mép đấu trường, 25% sinh rải rác quanh người chơi (đã được clamp an toàn)
            if (Random.value < 0.75f)
            {
                spawnPos = BrotatoArena.Instance.GetBorderSpawnPosition(edgeMargin: 1.0f);
            }
            else
            {
                spawnPos = BrotatoArena.Instance.ClampPosition(player.position + spawnOffset, padding: 1.2f);
            }
        }
        else
        {
            spawnPos = player.position + spawnOffset;
        }

        // Chọn Prefab quái
        GameObject targetPrefab = enemyPrefab;
        if (enemyPrefabs != null && enemyPrefabs.Length > 0)
        {
            targetPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
        }

        if (targetPrefab == null) return;

        // Spawn từ Object Pool (thay thế Instantiate)
        GameObject enemy = ObjectPool.Instance != null
            ? ObjectPool.Instance.Get(targetPrefab, spawnPos, Quaternion.identity)
            : Instantiate(targetPrefab, spawnPos, Quaternion.identity);

        // 2. Thuật toán cân chỉnh chỉ số quái theo PhanTich_LevelSelectManager_MonsterStats.md
        // Lấy hiệu ứng lời nguyền Curse (+10% máu quái mỗi cấp Curse)
        int curseLv = GameDataManager.GetCurseLevel();
        float curseMultiplier = 1.0f + (curseLv * 0.10f);

        float baseHpMult = config != null ? config.baseHpMultiplier : 1.0f;
        float growthRate = config != null ? config.hpGrowthPerWave : 0.25f;
        float finalHpMult = baseHpMult * (1.0f + (currentWave - 1) * growthRate) * curseMultiplier;

        // Tính giáp phẳng (Armor)
        int extraArmor = 0;
        if (config != null && currentWave >= config.armorStartWave)
        {
            extraArmor = Mathf.FloorToInt((currentWave - config.armorStartWave + 1) * config.armorGrowthRate);
        }

        // Tính sát thương quái
        int minDmg = config != null ? config.minDamage : 8;
        int maxDmg = config != null ? config.maxDamage : 15;
        int rolledDamage = Random.Range(minDmg, maxDmg + 1);

        // Tốc độ di chuyển
        float baseSpeed = config != null ? config.baseMoveSpeed : 2.0f;
        float maxSpeed = config != null ? config.maxMoveSpeed : 3.0f;
        float calculatedSpeed = Mathf.Min(maxSpeed, baseSpeed + (currentWave - 1) * 0.1f);

        // Kháng đẩy lùi (50: dễ giật lùi, 35: trung bình, 0: không bị giật lùi)
        float knockback = config != null ? config.knockbackResistance : 50f;

        // Gán chỉ số vào thực thể quái
        EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            enemyHealth.InitStats(finalHpMult, extraArmor, knockback, boss: false);
        }

        EnemyMovement enemyMovement = enemy.GetComponent<EnemyMovement>();
        if (enemyMovement != null)
        {
            enemyMovement.InitMovement(calculatedSpeed, rolledDamage, knockback);
        }
    }

    public GameObject SpawnBoss(GameObject customBossPrefab, float hpMultiplier = 1.0f)
    {
        if (player == null) return null;

        // Nếu người chơi chưa tạo Boss Prefab thì bỏ qua an toàn, không báo lỗi
        GameObject bossToSpawn = customBossPrefab;
        if (bossToSpawn == null && config != null) bossToSpawn = config.bossPrefabOverride;

        if (bossToSpawn == null)
        {
            Debug.Log("[SPAWNER] Chưa có Boss Prefab. Bạn có thể thêm sau khi thiết kế xong Boss!");
            return null;
        }

        Vector3 spawnPos = player.position + new Vector3(0, 8f, 0);
        if (BrotatoArena.Instance != null)
        {
            spawnPos = BrotatoArena.Instance.ClampPosition(spawnPos, padding: 2.5f);
        }

        // Spawn Boss từ Object Pool
        GameObject boss = ObjectPool.Instance != null
            ? ObjectPool.Instance.Get(bossToSpawn, spawnPos, Quaternion.identity)
            : Instantiate(bossToSpawn, spawnPos, Quaternion.identity);

        EnemyHealth bossHealth = boss.GetComponent<EnemyHealth>();
        if (bossHealth != null)
        {
            float bossHp = 50f * hpMultiplier * (config != null ? config.baseHpMultiplier : 1.0f);
            bossHealth.InitStats(bossHp, extraArmor: 2, knockbackRes: 0f, boss: true);
        }

        EnemyMovement bossMovement = boss.GetComponent<EnemyMovement>();
        if (bossMovement != null)
        {
            int bossDmg = config != null ? config.maxDamage * 2 : 30;
            bossMovement.InitMovement(1.2f, bossDmg, 0f);
        }

        return boss;
    }

    void SpawnChest()
    {
        if (player == null) return;

        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float randomDistance = Random.Range(chestSpawnDistanceMin, chestSpawnDistanceMax);
        Vector3 spawnOffset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * randomDistance;
        Vector3 spawnPos = player.position + spawnOffset;

        if (BrotatoArena.Instance != null)
        {
            spawnPos = BrotatoArena.Instance.ClampPosition(spawnPos, padding: 2.0f);
        }

        spawnPos.z = 0f; // Khóa chặt trục Z = 0 để rương luôn nổi lên trên mặt phẳng 2D, không chìm sau nền

        // Spawn rương từ Object Pool
        GameObject chestObj = ObjectPool.Instance != null
            ? ObjectPool.Instance.Get(lootChestPrefab, spawnPos, Quaternion.identity)
            : Instantiate(lootChestPrefab, spawnPos, Quaternion.identity);
        Debug.Log($"<color=cyan>[LOOT CHEST] Rương đã xuất hiện tại tọa độ: {spawnPos} (Cách người chơi {randomDistance:F1}m)</color>");
    }
}