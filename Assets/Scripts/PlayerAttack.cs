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

        // Nếu tìm thấy mục tiêu trong tầm bắn, bắn đạn
        if (closestEnemy != null)
        {
            Shoot(closestEnemy);
        }
    }

    void Shoot(Transform target)
    {
        if (bulletPrefab == null || target == null) return;

        // Khởi tạo viên đạn tại vị trí Player
        GameObject bullet = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
        if (bullet == null) return;

        // Tính hướng bay từ người chơi tới quái vật
        Vector2 shootDirection = (target.position - transform.position).normalized;

        // Gắn vận tốc cho đạn nếu có Rigidbody2D
        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = shootDirection * 10f;
        }

        // Tự động hủy viên đạn sau 2 giây để tránh tràn bộ nhớ
        Destroy(bullet, 2f);
    }
}