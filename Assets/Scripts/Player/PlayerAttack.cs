using System.Collections;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    public static PlayerAttack Instance { get; private set; }

    [Header("Base Attack Settings")]
    public GameObject bulletPrefab; // Kéo Bullet prefab vào đây
    public float attackRange = 5f;  // Tầm đánh
    public float fireRate = 1f;     // Tốc độ bắn cơ bản
    private float fireTimer;

    [Header("Passive Upgrade Levels")]
    public int damageUpLevel = 0;   // Mục 1: Tăng sát thương (Max Lv 5)
    public int multishotLevel = 0;  // Mục 3A: Đạn Tõe (Max Lv 5)
    public int burstFireLevel = 0;  // Mục 3B: Đạn Liên Tục (Max Lv 5)

    [Header("Overdrive State")]
    public bool isOverdriveActive = false;
    private float overdriveFireRate = 0.09f; // Tốc độ bắn siêu tốc khi Xả Chiêu

    void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(this);
    }

    void Update()
    {
        // Nhịp độ bắn phụ thuộc vào trạng thái Xả Chiêu
        float currentRate = isOverdriveActive ? overdriveFireRate : fireRate;

        fireTimer += Time.deltaTime;
        if (fireTimer >= currentRate)
        {
            FindAndShootClosestEnemy();
            fireTimer = 0f;
        }
    }

    public void SetOverdriveMode(bool active)
    {
        isOverdriveActive = active;
        fireTimer = 0f;
    }

    void FindAndShootClosestEnemy()
    {
        Transform closestEnemy = null;
        float attackRangeSqr = attackRange * attackRange;
        float minDistanceSqr = attackRangeSqr;
        Vector3 playerPos = transform.position;

        // 1. Quét tìm Quái vật gần nhất trong tầm bắn
        int count = EnemyHealth.ActiveEnemies.Count;
        for (int i = 0; i < count; i++)
        {
            EnemyHealth enemy = EnemyHealth.ActiveEnemies[i];
            if (enemy == null) continue;

            float distSqr = (enemy.transform.position - playerPos).sqrMagnitude;
            if (distSqr < minDistanceSqr)
            {
                minDistanceSqr = distSqr;
                closestEnemy = enemy.transform;
            }
        }

        // 2. Quét tìm thêm Rương Loot gần nhất
        int chestCount = LootChest.ActiveChests.Count;
        for (int i = 0; i < chestCount; i++)
        {
            LootChest chest = LootChest.ActiveChests[i];
            if (chest == null) continue;

            float distSqr = (chest.transform.position - playerPos).sqrMagnitude;
            if (distSqr < minDistanceSqr)
            {
                minDistanceSqr = distSqr;
                closestEnemy = chest.transform;
            }
        }

        // 3. Thực hiện phát bắn nếu tìm thấy mục tiêu
        if (closestEnemy != null)
        {
            ExecuteShootingPattern(closestEnemy);
        }
    }

    private void ExecuteShootingPattern(Transform target)
    {
        if (bulletPrefab == null || target == null) return;

        Vector2 baseDir = (target.position - transform.position).normalized;

        if (isOverdriveActive)
        {
            // === TRẠNG THÁI XẢ CHIÊU (OVERDRIVE) ===
            if (multishotLevel > 0)
            {
                // Bão đạn tõe góc rộng (7 tia quét sạch hình nón trước mặt)
                float[] overdriveAngles = new float[] { -30f, -20f, -10f, 0f, 10f, 20f, 30f };
                for (int i = 0; i < overdriveAngles.Length; i++)
                {
                    Vector2 dir = Quaternion.Euler(0, 0, overdriveAngles[i]) * baseDir;
                    SpawnSingleBullet(dir);
                }
            }
            else if (burstFireLevel > 0)
            {
                // Xả đạn liên hoàn không ngừng nghỉ trên đường thẳng
                SpawnSingleBullet(baseDir);
            }
            else
            {
                // Súng lục mặc định: xả đạn như súng máy không độ trễ
                SpawnSingleBullet(baseDir);
            }
        }
        else
        {
            // === TRẠNG THÁI BÌNH THƯỜNG ===
            if (multishotLevel > 0)
            {
                // Lựa chọn A: Đạn Tõe (Multishot)
                // Cấp 1-2: 2 viên, Cấp 3-5: 3 viên (Tối đa ở cấp 5 bắn tõe 3 viên)
                int bulletsCount = (multishotLevel >= 3) ? 3 : 2;
                float spread = 12f + (multishotLevel * 2f);

                if (bulletsCount == 2)
                {
                    SpawnSingleBullet(Quaternion.Euler(0, 0, -spread * 0.5f) * baseDir);
                    SpawnSingleBullet(Quaternion.Euler(0, 0, spread * 0.5f) * baseDir);
                }
                else
                {
                    SpawnSingleBullet(Quaternion.Euler(0, 0, -spread) * baseDir);
                    SpawnSingleBullet(baseDir);
                    SpawnSingleBullet(Quaternion.Euler(0, 0, spread) * baseDir);
                }
            }
            else if (burstFireLevel > 0)
            {
                // Lựa chọn B: Đạn Liên Tục (Burst Fire)
                // Cấp 1-2: 2 viên, Cấp 3-5: 3 viên
                int burstCount = (burstFireLevel >= 3) ? 3 : 2;
                StartCoroutine(BurstShootRoutine(target, burstCount, 0.07f));
            }
            else
            {
                // Súng lục mặc định 1 viên
                SpawnSingleBullet(baseDir);
            }
        }
    }

    private IEnumerator BurstShootRoutine(Transform target, int count, float interval)
    {
        for (int i = 0; i < count; i++)
        {
            if (target != null)
            {
                Vector2 dir = (target.position - transform.position).normalized;
                SpawnSingleBullet(dir);
            }
            else
            {
                break;
            }
            yield return new WaitForSeconds(interval);
        }
    }

    private void SpawnSingleBullet(Vector2 shootDirection)
    {
        if (bulletPrefab == null) return;

        GameObject bullet = ObjectPool.Instance != null
            ? ObjectPool.Instance.Get(bulletPrefab, transform.position, Quaternion.identity)
            : Instantiate(bulletPrefab, transform.position, Quaternion.identity);
        if (bullet == null) return;

        BulletLogic bl = bullet.GetComponent<BulletLogic>();
        if (bl != null)
        {
            bl.SetDirection(shootDirection);

            // Mục 1: Tăng Sát Thương (Damage Up) - mỗi cấp +20% sát thương
            float dmgMultiplier = 1.0f + (damageUpLevel * 0.20f);
            bl.baseDamage = 15f * dmgMultiplier;
        }

        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = shootDirection * 10f;
        }
    }
}