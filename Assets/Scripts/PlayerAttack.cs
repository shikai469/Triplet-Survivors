using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    public GameObject bulletPrefab; // Kéo Bullet prefab vào đây
    public float attackRange = 5f;  // Tầm đánh
    public float fireRate = 1f;     // Tốc độ bắn (1 giây/viên)
    private float fireTimer;

    void Update()
    {
        // Bộ đếm thời gian hồi chiêu
        fireTimer += Time.deltaTime;
        if (fireTimer >= fireRate)
        {
            FindAndShootClosestEnemy();
            fireTimer = 0f;
        }
    }

    void FindAndShootClosestEnemy()
    {
        Transform closestEnemy = null;
        float attackRangeSqr = attackRange * attackRange;
        float minDistanceSqr = attackRangeSqr;
        Vector3 playerPos = transform.position;

        // 1. Quét tìm Quái vật gần nhất trong tầm bắn (Zero GC Alloc)
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

        // 2. Quét tìm thêm Rương Loot gần nhất trong tầm bắn
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

        // 3. Nếu tìm thấy mục tiêu (quái hoặc rương) trong tầm bắn, tiến hành bắn đạn
        if (closestEnemy != null)
        {
            Shoot(closestEnemy);
        }
    }

    void Shoot(Transform target)
    {
        if (bulletPrefab == null || target == null) return;

        // Khởi tạo viên đạn từ Object Pool (thay thế Instantiate)
        GameObject bullet = ObjectPool.Instance != null
            ? ObjectPool.Instance.Get(bulletPrefab, transform.position, Quaternion.identity)
            : Instantiate(bulletPrefab, transform.position, Quaternion.identity);
        if (bullet == null) return;

        // Tính hướng bay từ người chơi tới mục tiêu
        Vector2 shootDirection = (target.position - transform.position).normalized;

        // Truyền hướng bay trực tiếp cho BulletLogic
        BulletLogic bl = bullet.GetComponent<BulletLogic>();
        if (bl != null)
        {
            bl.SetDirection(shootDirection);
        }

        // Gắn vận tốc cho đạn nếu có Rigidbody2D
        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = shootDirection * 10f;
        }

        // BulletLogic tự quản lý thời gian sống và trả về pool, không cần Destroy thủ công
    }
}