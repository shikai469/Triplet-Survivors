using UnityEngine;

public class BulletLogic : MonoBehaviour
{
    [Header("Bullet Settings")]
    public float speed = 10f;
    public float lifeTime = 5f;
    public int pierceCount = 0; // 0: chạm 1 quái là biến mất

    [Header("Damage Settings")]
    public float baseDamage = 15f;
    public float damageRangeMin = -2f;
    public float damageRangeMax = 3f;
    public float critRate = 10f; // 10%
    public float critMultiplier = 1.5f;

    private Vector2 moveDirection = Vector2.up;

    void Start()
    {
        Destroy(gameObject, lifeTime);

        // Tự động tìm và ngắm vào quái vật gần nhất khi vừa sinh ra
        FindNearestEnemy();
    }

    void FindNearestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        GameObject nearestEnemy = null;
        float minDistance = Mathf.Infinity;
        Vector3 currentPosition = transform.position;

        foreach (GameObject enemy in enemies)
        {
            float dist = Vector3.Distance(enemy.transform.position, currentPosition);
            if (dist < minDistance)
            {
                minDistance = dist;
                nearestEnemy = enemy;
            }
        }

        if (nearestEnemy != null)
        {
            Vector2 targetDir = (nearestEnemy.transform.position - transform.position).normalized;
            SetDirection(targetDir);
        }
    }

    public void SetDirection(Vector2 dir)
    {
        moveDirection = dir.normalized;

        // Xoay đầu đạn chúc về phía kẻ địch
        float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    void Update()
    {
        transform.Translate(moveDirection * speed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter2D(Collider2D hitInfo)
    {
        if (hitInfo.CompareTag("Enemy"))
        {
            float rawDamage = Random.Range(baseDamage + damageRangeMin, baseDamage + damageRangeMax);
            float strengthBonus = PlayerStats.Instance != null ? PlayerStats.Instance.strengthBonus : 0f;

            bool isCrit = Random.Range(0f, 100f) < critRate;
            float finalDmg = rawDamage * (isCrit ? critMultiplier : 1.0f) * (1.0f + strengthBonus);

            // Kiểm tra xem đối tượng có phải là rương không
            LootChest chest = hitInfo.GetComponent<LootChest>();
            if (chest != null)
            {
                // Nếu là rương, gọi hàm TakeDamage của rương
                chest.TakeDamage(1); // Rương chỉ cần 1 HP, hoặc bạn truyền finalDmg tùy ý
            }
            else
            {
                // Nếu không phải rương, xử lý như quái vật bình thường
                EnemyHealth enemy = hitInfo.GetComponent<EnemyHealth>();
                if (enemy != null)
                {
                    enemy.TakeDamage(Mathf.RoundToInt(finalDmg), isCrit);
                }
            }

            if (pierceCount <= 0)
            {
                Destroy(gameObject);
            }
            else
            {
                pierceCount--;
            }
        }
    }
}