using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject enemyPrefab;
    public GameObject[] enemyPrefabs; // Danh sách các loại quái để spawn ngẫu nhiên (nếu có)
    public Transform player;
    public float spawnInterval = 1.0f;
    public float spawnDistance = 14.0f; // Bán kính chuẩn ngoài mép camera
    public int maxEnemies = 40;

    [Header("Distance & Arena Bounds Tuning")]
    [Tooltip("Khoảng cách spawn tối thiểu so với người chơi (đảm bảo luôn ở xa ngoài tầm nhìn)")]
    public float spawnDistanceMin = 12.0f;
    [Tooltip("Khoảng cách spawn tối đa xung quanh người chơi")]
    public float spawnDistanceMax = 15.5f;
    [Tooltip("Khoảng cách tuyệt đối quái phải cách xa người chơi khi bị tường arena kẹp")]
    public float minSafeDistance = 9.5f;

    [Header("Level Scaling (Quái spawn nhiều dần theo cấp độ)")]
    [Tooltip("Trần quái tối đa an toàn để giữ vững 60 FPS trên mobile")]
    public int maxEnemiesCap = 85;
    [Tooltip("Thời gian giãn cách tối thiểu giữa các đợt spawn")]
    public float minSpawnInterval = 0.35f;
    [Tooltip("Số quái tối đa sinh ra cùng lúc trong 1 nhịp (sẽ được sinh dàn trải qua vài frame)")]
    public int maxBatchSpawnCount = 4;

    [Header("Loot Chest Settings")]
    public GameObject lootChestPrefab;
    public float chestSpawnInterval = 45f;
    public float chestFirstSpawnDelay = 5f; // Rương đầu tiên sinh sau 5s để test thấy ngay (sau đó lặp lại mỗi 45s)
    public float chestSpawnDistanceMax = 12f;
    public float chestSpawnDistanceMin = 5f;

    [Header("Fire Spirit Ring Ambush (Vòng Tròn Tinh Linh Lửa)")]
    [Tooltip("Prefab Quái Tinh Linh Lửa")]
    public GameObject fireSpiritPrefab;
    [Tooltip("Bật/Tắt sự kiện vòng tròn bao vây")]
    public bool enableRingAmbush = true;
    [Tooltip("Khoảng thời gian ngẫu nhiên tối thiểu giữa các lần xuất hiện vòng tròn (giây)")]
    public float ringMinInterval = 30f;
    [Tooltip("Khoảng thời gian ngẫu nhiên tối đa giữa các lần xuất hiện vòng tròn (giây)")]
    public float ringMaxInterval = 40f;
    [Tooltip("Số lượng quái tinh linh lửa tạo thành vòng tròn")]
    public int ringMonsterCount = 20;
    [Tooltip("Bán kính vòng tròn bao vây (độ to rộng)")]
    public float ringRadius = 10f;

    [Header("Wave Progression")]
    public float gameTime = 0f;
    public int currentWave = 1;

    [Header("Active Difficulty Config")]
    public DifficultyConfig config;

    private float baseSpawnIntervalStored = 1.0f;
    private int baseMaxEnemiesStored = 40;

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
        else
        {
            baseSpawnIntervalStored = config.baseSpawnInterval > 0 ? config.baseSpawnInterval : spawnInterval;
            baseMaxEnemiesStored = config.baseMaxEnemies > 0 ? config.baseMaxEnemies : maxEnemies;
        }

        // 1. Tối ưu hóa cực hạn: Pre-warm sẵn toàn bộ quái vào ObjectPool ngay từ đầu trận (Zero GC lag)
        PrewarmEnemyPools();

        // 2. Bắt đầu các vòng lặp sinh quái & rương
        StartCoroutine(SpawnRoutine());
        StartCoroutine(SpawnChestRoutine());
        if (enableRingAmbush)
        {
            StartCoroutine(SpawnRingAmbushRoutine());
        }
    }

    /// <summary>
    /// Chuẩn bị sẵn một lượng quái trong Object Pool để khi cấp cao quái ra đông không bao giờ bị khựng khung hình.
    /// </summary>
    private void PrewarmEnemyPools()
    {
        if (ObjectPool.Instance == null) return;

        if (enemyPrefab != null)
        {
            ObjectPool.Instance.Prewarm(enemyPrefab, 20);
        }

        if (enemyPrefabs != null && enemyPrefabs.Length > 0)
        {
            for (int i = 0; i < enemyPrefabs.Length; i++)
            {
                if (enemyPrefabs[i] != null)
                {
                    ObjectPool.Instance.Prewarm(enemyPrefabs[i], 18);
                }
            }
        }

        if (fireSpiritPrefab != null)
        {
            ObjectPool.Instance.Prewarm(fireSpiritPrefab, 20);
        }

        if (lootChestPrefab != null)
        {
            ObjectPool.Instance.Prewarm(lootChestPrefab, 3);
        }
    }

    public void InitializeSpawner(DifficultyConfig difficultyConfig)
    {
        config = difficultyConfig;
        baseSpawnIntervalStored = config.baseSpawnInterval;
        baseMaxEnemiesStored = config.baseMaxEnemies;
        spawnInterval = baseSpawnIntervalStored;
        maxEnemies = baseMaxEnemiesStored;
        if (config.maxEnemiesCap > 0) maxEnemiesCap = config.maxEnemiesCap;
        if (config.minSpawnInterval > 0) minSpawnInterval = config.minSpawnInterval;

        Debug.Log($"[SPAWNER] Nạp cấu hình: {config.difficultyName} | Interval: {spawnInterval}s | BaseMax: {maxEnemies} | Cap: {maxEnemiesCap}");
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

        // Lấy cấp độ hiện tại của người chơi
        int playerLevel = (PlayerStats.Instance != null) ? PlayerStats.Instance.currentLevel : 1;

        // --- CƠ CHẾ QUÁI SPAWN NHIỀU DẦN THEO CẤP ĐỘ ---
        // 1. Tần suất spawn (spawnInterval) giảm dần khi cấp độ người chơi tăng
        float levelFactor = 1.0f + (playerLevel - 1) * 0.06f + (currentWave - 1) * 0.04f;
        float calculatedInterval = baseSpawnIntervalStored / levelFactor;
        spawnInterval = Mathf.Max(minSpawnInterval, calculatedInterval);

        // 2. Trần số lượng quái sống cùng lúc (maxEnemies) tăng dần theo cấp độ
        int calculatedMax = baseMaxEnemiesStored + (playerLevel - 1) * 3 + (currentWave - 1) * 2;
        maxEnemies = Mathf.Min(maxEnemiesCap, calculatedMax);
    }

    IEnumerator SpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            // Bảo vệ 60 FPS: Nếu thiết bị có hiện tượng giật cục frame time cao, tạm nghỉ nhẹ để hồi FPS
            if (Time.unscaledDeltaTime > 0.04f)
            {
                yield return new WaitForSeconds(0.1f);
            }

            if (player == null)
            {
                if (PlayerStats.Instance != null) player = PlayerStats.Instance.transform;
                continue;
            }

            int currentEnemyCount = EnemyHealth.ActiveEnemies.Count;
            if (currentEnemyCount >= maxEnemies) continue;

            int playerLevel = (PlayerStats.Instance != null) ? PlayerStats.Instance.currentLevel : 1;
            int batchCount = CalculateBatchCount(playerLevel, currentWave);
            int allowedToSpawn = Mathf.Min(batchCount, maxEnemies - currentEnemyCount);

            if (allowedToSpawn > 1)
            {
                // Sinh quái dàn trải qua các frame (Staggered Spawning) để chống giật lag
                StartCoroutine(SpawnBatchStaggered(allowedToSpawn, playerLevel));
            }
            else if (allowedToSpawn == 1)
            {
                SpawnEnemy(playerLevel);
            }
        }
    }

    /// <summary>
    /// Tính toán số lượng quái sinh ra mỗi nhịp tăng dần theo cấp độ người chơi.
    /// </summary>
    private int CalculateBatchCount(int level, int wave)
    {
        int batch = 1;
        if (level >= 15 || wave >= 8) batch = 4;
        else if (level >= 10 || wave >= 5) batch = 3;
        else if (level >= 5 || wave >= 3) batch = (Random.value < 0.5f) ? 2 : 1;
        else batch = 1;

        // Cơ hội ngẫu nhiên thêm 1 quái khi người chơi đạt cấp cao
        if (level >= 8 && Random.value < 0.35f) batch++;

        return Mathf.Clamp(batch, 1, maxBatchSpawnCount);
    }

    /// <summary>
    /// Sinh quái dàn trải cách nhau một khoảng thời gian cực ngắn (60ms) để giữ vững 60 FPS mượt mà.
    /// </summary>
    private IEnumerator SpawnBatchStaggered(int count, int playerLevel)
    {
        for (int i = 0; i < count; i++)
        {
            if (EnemyHealth.ActiveEnemies.Count < maxEnemies)
            {
                SpawnEnemy(playerLevel);
            }
            yield return new WaitForSeconds(0.06f);
        }
    }

    IEnumerator SpawnChestRoutine()
    {
        // 1. Chờ nhịp rương đầu tiên (5 giây) để test thấy ngay lập tức
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

    /// <summary>
    /// Tính toán vị trí sinh quái xung quanh người chơi ở khoảng cách xa (ngoài mép camera).
    /// Đảm bảo dù người chơi đứng ở góc hoặc sát tường sân đấu, quái luôn sinh từ xa chạy lại, không bao giờ xuất hiện đè lên người chơi.
    /// </summary>
    public Vector3 GetSpawnPositionAroundPlayer(Transform targetPlayer)
    {
        Vector3 playerPos = targetPlayer.position;

        if (BrotatoArena.Instance != null)
        {
            Vector3 arenaCenter = BrotatoArena.Instance.transform.position;
            Vector2 dirToCenter = ((Vector2)(arenaCenter - playerPos)).normalized;

            Vector3 bestPos = Vector3.zero;
            float maxValidDistance = -1f;

            // Thử các góc khác nhau xung quanh người chơi
            for (int attempt = 0; attempt < 8; attempt++)
            {
                float angle;
                // Nếu người chơi ở xa trung tâm (>5m), ưu tiên góc hướng về phần sân rộng / trung tâm
                if (attempt < 4 && dirToCenter.sqrMagnitude > 0.01f && (playerPos - arenaCenter).magnitude > 5f)
                {
                    float centerAngle = Mathf.Atan2(dirToCenter.y, dirToCenter.x) * Mathf.Rad2Deg;
                    angle = (centerAngle + Random.Range(-65f, 65f)) * Mathf.Deg2Rad;
                }
                else
                {
                    angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                }

                float dist = Random.Range(spawnDistanceMin, spawnDistanceMax);
                Vector3 rawPos = playerPos + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * dist;
                Vector3 candidate = BrotatoArena.Instance.ClampPosition(rawPos, padding: 1.0f);
                candidate.z = 0f;

                float distFromPlayer = Vector2.Distance(playerPos, candidate);
                if (distFromPlayer >= minSafeDistance)
                {
                    return candidate;
                }

                if (distFromPlayer > maxValidDistance)
                {
                    maxValidDistance = distFromPlayer;
                    bestPos = candidate;
                }
            }

            // Nếu người chơi đứng kẹt sát góc tường, chọn vị trí xa người chơi nhất tìm được
            if (maxValidDistance > 0f) return bestPos;

            // Fallback: Lấy vị trí mép đấu trường xa người chơi
            return BrotatoArena.Instance.GetBorderSpawnPosition(edgeMargin: 1.0f);
        }
        else
        {
            // Bản đồ mở: Sinh 360 độ xung quanh người chơi ở bán kính xa ngoài tầm nhìn
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float dist = Random.Range(spawnDistanceMin, spawnDistanceMax);
            Vector3 pos = playerPos + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * dist;
            pos.z = 0f;
            return pos;
        }
    }

    /// <summary>
    /// Chọn loại quái thông minh thích ứng theo cấp độ người chơi.
    /// </summary>
    private GameObject SelectEnemyPrefabByLevel(int playerLevel)
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0) return enemyPrefab;
        if (enemyPrefabs.Length == 1) return enemyPrefabs[0];

        float roll = Random.value;
        if (playerLevel <= 3)
        {
            // Cấp 1-3: 85% quái đầu (cơ bản), 15% quái tiếp theo
            return (roll < 0.85f || enemyPrefabs.Length < 2) ? enemyPrefabs[0] : enemyPrefabs[1];
        }
        else if (playerLevel <= 7)
        {
            // Cấp 4-7: Quái cơ bản 50%, quái bậc 2 35%, quái mạnh 15%
            if (roll < 0.50f) return enemyPrefabs[0];
            if (roll < 0.85f && enemyPrefabs.Length >= 2) return enemyPrefabs[1];
            return enemyPrefabs[Mathf.Min(2, enemyPrefabs.Length - 1)];
        }
        else
        {
            // Cấp 8+: Tỷ lệ ngẫu nhiên đều giữa các quái để tạo sự phong phú và áp lực
            return enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
        }
    }

    void SpawnEnemy(int playerLevel = 1)
    {
        if (player == null) return;

        // 1. Tính toán vị trí sinh quái xung quanh người chơi ở khoảng cách xa
        Vector3 spawnPos = GetSpawnPositionAroundPlayer(player);

        // 2. Chọn Prefab quái theo cấp độ
        GameObject targetPrefab = SelectEnemyPrefabByLevel(playerLevel);
        if (targetPrefab == null) targetPrefab = enemyPrefab;
        if (targetPrefab == null) return;

        // 3. Lấy thực thể từ Object Pool (Zero GC Allocation)
        GameObject enemy = ObjectPool.Instance != null
            ? ObjectPool.Instance.Get(targetPrefab, spawnPos, Quaternion.identity)
            : Instantiate(targetPrefab, spawnPos, Quaternion.identity);

        // 4. Thuật toán cân chỉnh chỉ số quái theo Wave, Cấp độ và Lời nguyền (Curse)
        int curseLv = GameDataManager.GetCurseLevel();
        float curseMultiplier = 1.0f + (curseLv * 0.10f);

        float baseHpMult = config != null ? config.baseHpMultiplier : 1.0f;
        float growthRate = config != null ? config.hpGrowthPerWave : 0.25f;
        float levelHpBonus = (playerLevel - 1) * 0.03f; // Tăng nhẹ máu quái theo cấp người chơi
        float finalHpMult = baseHpMult * (1.0f + (currentWave - 1) * growthRate + levelHpBonus) * curseMultiplier;

        // Giáp phẳng (Armor)
        int extraArmor = 0;
        if (config != null && currentWave >= config.armorStartWave)
        {
            extraArmor = Mathf.FloorToInt((currentWave - config.armorStartWave + 1) * config.armorGrowthRate);
        }

        // Sát thương quái
        int minDmg = config != null ? config.minDamage : 8;
        int maxDmg = config != null ? config.maxDamage : 15;
        int rolledDamage = Random.Range(minDmg, maxDmg + 1);

        // Tốc độ di chuyển
        float baseSpeed = config != null ? config.baseMoveSpeed : 2.0f;
        float maxSpeed = config != null ? config.maxMoveSpeed : 3.0f;
        float calculatedSpeed = Mathf.Min(maxSpeed, baseSpeed + (currentWave - 1) * 0.08f + (playerLevel - 1) * 0.02f);

        // Kháng đẩy lùi
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

        GameObject bossToSpawn = customBossPrefab;
        if (bossToSpawn == null && config != null) bossToSpawn = config.bossPrefabOverride;

        if (bossToSpawn == null)
        {
            Debug.Log("[SPAWNER] Chưa có Boss Prefab. Bạn có thể thêm sau khi thiết kế xong Boss!");
            return null;
        }

        Vector3 spawnPos = player.position + new Vector3(0, 10f, 0);
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

        spawnPos.z = 0f;

        // Spawn rương từ Object Pool
        GameObject chestObj = ObjectPool.Instance != null
            ? ObjectPool.Instance.Get(lootChestPrefab, spawnPos, Quaternion.identity)
            : Instantiate(lootChestPrefab, spawnPos, Quaternion.identity);
        Debug.Log($"<color=cyan>[LOOT CHEST] Rương đã xuất hiện tại tọa độ: {spawnPos} (Cách người chơi {randomDistance:F1}m)</color>");
    }

    IEnumerator SpawnRingAmbushRoutine()
    {
        while (enableRingAmbush)
        {
            float waitTime = Random.Range(ringMinInterval, ringMaxInterval);
            yield return new WaitForSeconds(waitTime);

            if (player == null)
            {
                if (PlayerStats.Instance != null) player = PlayerStats.Instance.transform;
                if (player == null) continue;
            }

            if (fireSpiritPrefab == null)
            {
#if UNITY_EDITOR
                fireSpiritPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy-FireHighSpirit.prefab");
#endif
                if (fireSpiritPrefab == null) continue;
            }

            SpawnFireSpiritRing();
        }
    }

    [ContextMenu("Test Spawn Fire Spirit Ring Now")]
    public void SpawnFireSpiritRing()
    {
        if (player == null)
        {
            if (PlayerStats.Instance != null) player = PlayerStats.Instance.transform;
            if (player == null) return;
        }

        if (fireSpiritPrefab == null)
        {
#if UNITY_EDITOR
            fireSpiritPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy-FireHighSpirit.prefab");
#endif
            if (fireSpiritPrefab == null) return;
        }

        Vector3 center = player.position;
        float angleStep = 360f / Mathf.Max(1, ringMonsterCount);

        Debug.Log($"<color=orange>[SPAWNER] Bão Tinh Linh Lửa xuất hiện! {ringMonsterCount} quái bao vây người chơi (Bán kính {ringRadius}m)</color>");

        int curseLv = GameDataManager.GetCurseLevel();
        float curseMultiplier = 1.0f + (curseLv * 0.10f);

        float baseHpMult = config != null ? config.baseHpMultiplier : 1.0f;
        float growthRate = config != null ? config.hpGrowthPerWave : 0.25f;
        float finalHpMult = baseHpMult * (1.0f + (currentWave - 1) * growthRate) * curseMultiplier;

        int extraArmor = 0;
        if (config != null && currentWave >= config.armorStartWave)
        {
            extraArmor = Mathf.FloorToInt((currentWave - config.armorStartWave + 1) * config.armorGrowthRate);
        }

        int minDmg = config != null ? config.minDamage : 8;
        int maxDmg = config != null ? config.maxDamage : 15;
        int rolledDamage = Random.Range(minDmg, maxDmg + 1);

        float baseSpeed = config != null ? config.baseMoveSpeed : 1.8f;
        float maxSpeed = config != null ? config.maxMoveSpeed : 2.8f;
        float calculatedSpeed = Mathf.Min(maxSpeed, baseSpeed + (currentWave - 1) * 0.08f);

        float knockback = config != null ? config.knockbackResistance : 40f;

        for (int i = 0; i < ringMonsterCount; i++)
        {
            float angleDeg = i * angleStep;
            float angleRad = angleDeg * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad), 0f) * ringRadius;
            Vector3 rawPos = center + offset;

            Vector3 spawnPos = BrotatoArena.Instance != null
                ? BrotatoArena.Instance.ClampPosition(rawPos, padding: 0.8f)
                : rawPos;

            spawnPos.z = 0f;

            GameObject spirit = ObjectPool.Instance != null
                ? ObjectPool.Instance.Get(fireSpiritPrefab, spawnPos, Quaternion.identity)
                : Instantiate(fireSpiritPrefab, spawnPos, Quaternion.identity);

            EnemyHealth enemyHealth = spirit.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.InitStats(finalHpMult, extraArmor, knockback, boss: false);
            }

            EnemyMovement enemyMovement = spirit.GetComponent<EnemyMovement>();
            if (enemyMovement != null)
            {
                enemyMovement.InitMovement(calculatedSpeed, rolledDamage, knockback);
            }
        }
    }
}
