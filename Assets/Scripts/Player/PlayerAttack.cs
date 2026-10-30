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
                // Shotgun: bắn chùm tỏa (mỗi viên ~60% dmg nhân vật, áp sát ăn trọn 180% dmg)
                float shotgunRatio = 0.55f + (multishotLevel * 0.05f);
                int bulletsCount = (multishotLevel >= 3) ? 3 : 2;
                float spread = 12f + (multishotLevel * 2f);

                if (bulletsCount == 2)
                {
                    SpawnSingleBullet(Quaternion.Euler(0, 0, -spread * 0.5f) * baseDir, shotgunRatio);
                    SpawnSingleBullet(Quaternion.Euler(0, 0, spread * 0.5f) * baseDir, shotgunRatio);
                }
                else
                {
                    SpawnSingleBullet(Quaternion.Euler(0, 0, -spread) * baseDir, shotgunRatio);
                    SpawnSingleBullet(baseDir, shotgunRatio);
                    SpawnSingleBullet(Quaternion.Euler(0, 0, spread) * baseDir, shotgunRatio);
                }
            }
            else if (burstFireLevel > 0)
            {
                // Súng Máy: xả chuỗi 2-3 viên liên thanh, mỗi viên 65% dmg nhân vật
                int burstCount = (burstFireLevel >= 3) ? 3 : 2;
                StartCoroutine(BurstShootRoutine(target, burstCount, 0.07f, 0.65f));
            }
            else
            {
                // Súng lục mặc định 1 viên chuẩn 100% dmg nhân vật
                SpawnSingleBullet(baseDir, 1.0f);
            }
        }
    }

    private IEnumerator BurstShootRoutine(Transform target, int count, float interval, float damageRatio = 1.0f)
    {
        for (int i = 0; i < count; i++)
        {
            if (target != null)
            {
                Vector2 dir = (target.position - transform.position).normalized;
                SpawnSingleBullet(dir, damageRatio);
            }
            else
            {
                break;
            }
            yield return new WaitForSeconds(interval);
        }
    }

    private void SpawnSingleBullet(Vector2 shootDirection, float damageRatio = 1.0f)
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

            // Sát thương tính theo tổng sát thương nhân vật * hệ số nhánh súng
            float totalPlayerDmg = PlayerStats.Instance != null ? PlayerStats.Instance.GetTotalAttackDamage() : 12f;
            bl.baseDamage = totalPlayerDmg * damageRatio;
        }

        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = shootDirection * 10f;
        }
    }
}