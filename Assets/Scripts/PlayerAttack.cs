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
        // Quét tất cả các object có Collider trong vòng tròn bán kính attackRange
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, attackRange);
        Transform closestEnemy = null;
        float minDistance = Mathf.Infinity;

        // Duyệt mảng để tìm quái vật gần nhất mang Tag "Enemy"
        foreach (Collider2D hit in hitColliders)
        {
            if (hit.CompareTag("Enemy"))
            {
                float distance = Vector2.Distance(transform.position, hit.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestEnemy = hit.transform;
                }
            }
        }

        // Nếu tìm thấy mục tiêu, bắn đạn
        if (closestEnemy != null)
        {
            Shoot(closestEnemy);
        }
    }

    void Shoot(Transform target)
    {
        // Khởi tạo viên đạn tại vị trí Player
        GameObject bullet = Instantiate(bulletPrefab, transform.position, Quaternion.identity);

        // Tính hướng bay từ người chơi tới quái vật
        Vector2 shootDirection = (target.position - transform.position).normalized;

        // Gắn vận tốc cho đạn (Unity 6 dùng linearVelocity)
        bullet.GetComponent<Rigidbody2D>().linearVelocity = shootDirection * 10f;

        // Tự động hủy viên đạn sau 2 giây để tránh tràn bộ nhớ
        Destroy(bullet, 2f);
    }
}