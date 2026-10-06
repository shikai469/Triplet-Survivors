using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject enemyPrefab;
    public Transform player;
    public float spawnInterval = 1.0f;
    public float spawnDistance = 14.0f; // Bán kính 12-15 đơn vị ngoài mép camera
    public int maxEnemies = 40;

    [Header("Loot Chest Settings")]
    public GameObject lootChestPrefab; // Kéo prefab LootChest vào đây trong Inspector
    public float chestSpawnInterval = 45f; // Thời gian sinh rương (ví dụ 45 giây/rương)
    public float chestSpawnDistanceMax = 12f; // Khoảng cách sinh tối đa
    public float chestSpawnDistanceMin = 5f;  // Khoảng cách sinh tối thiểu để không rơi thẳng vào đầu player

    [Header("Wave Progression")]
    public float gameTime = 0f;
    private int currentWave = 1;

    void Start()
    {
        if (player == null && PlayerStats.Instance != null)
        {
            player = PlayerStats.Instance.transform;
        }

        StartCoroutine(SpawnRoutine());
        StartCoroutine(SpawnChestRoutine()); // Chạy coroutine sinh rương
    }

    void Update()
    {
        gameTime += Time.deltaTime;
        // Mỗi 30 giây tăng 1 Wave
        currentWave = 1 + Mathf.FloorToInt(gameTime / 30f);
    }

    IEnumerator SpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            if (player == null) continue;

            // Đếm số lượng quái hiện tại
            int currentEnemyCount = GameObject.FindGameObjectsWithTag("Enemy").Length;
            if (currentEnemyCount >= maxEnemies) continue;

            SpawnEnemy();
        }
    }

    // Coroutine mới để sinh rương loot
    IEnumerator SpawnChestRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(chestSpawnInterval);

            if (player == null || lootChestPrefab == null) continue;

            SpawnChest();
        }
    }

    void SpawnEnemy()
    {
        // 1. Tính toán vị trí spawn ngẫu nhiên ngoài mép màn hình: d * (cos, sin)
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector3 spawnOffset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * spawnDistance;
        Vector3 spawnPos = player.position + spawnOffset;

        GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);

        // 2. Tính hệ số buff chỉ số theo Wave (Wave càng cao máu càng trâu)
        float hpMultiplier = 1.0f + (currentWave - 1) * 0.25f; // Mỗi wave +25% máu
        int extraArmor = (currentWave >= 5) ? 1 : 0;           // Từ wave 5 bắt đầu có giáp

        EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            enemyHealth.InitStats(hpMultiplier, extraArmor);
        }

        // Tăng nhẹ tốc độ quái theo thời gian
        EnemyMovement enemyMovement = enemy.GetComponent<EnemyMovement>();
        if (enemyMovement != null)
        {
            enemyMovement.moveSpeed = Mathf.Min(4.0f, 2.0f + (currentWave - 1) * 0.1f);
        }
    }

    // Hàm mới để xử lý vị trí và sinh rương
    void SpawnChest()
    {
        // Tính toán vị trí ngẫu nhiên trong khoảng Min và Max quanh Player
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float randomDistance = Random.Range(chestSpawnDistanceMin, chestSpawnDistanceMax);
        Vector3 spawnOffset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * randomDistance;
        Vector3 spawnPos = player.position + spawnOffset;

        // Sinh rương tại vị trí đó
        Instantiate(lootChestPrefab, spawnPos, Quaternion.identity);
    }
}