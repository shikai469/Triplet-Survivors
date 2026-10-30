using UnityEngine;

/// <summary>
/// Kỹ năng 2: Đạn Xuyên Thấu (Piercing Shot) [Max Lv 3]
/// Cơ chế: Thỉnh thoảng bắn ra 1 viên đạn sniper cực mạnh, bay xuyên qua nhiều quái trên đường thẳng.
/// Tiến trình: Nâng cấp giúp tăng tốc độ bắn (giảm thời gian hồi).
/// </summary>
public class PiercingShotSkill : MonoBehaviour
{
    [Header("Skill Level")]
    public int currentLevel = 0;
    public const int MaxLevel = 3;

    [Header("Stats")]
    public float baseCooldown = 5.0f; // Lv 1: 5s, Lv 2: 3.8s, Lv 3: 2.5s
    public float baseDamage = 40f;
    public float bulletSpeed = 14f;
    public float bulletLifeTime = 4f;

    private float cooldownTimer;
    private GameObject bulletTemplate;

    public void SetLevel(int level)
    {
        currentLevel = Mathf.Clamp(level, 0, MaxLevel);
        enabled = (currentLevel > 0);
    }

    void Update()
    {
        if (currentLevel <= 0) return;

        float effectiveCooldown = GetCurrentCooldown();
        cooldownTimer += Time.deltaTime;

        if (cooldownTimer >= effectiveCooldown)
        {
            if (TryShootSniper())
            {
                cooldownTimer = 0f;
            }
        }
    }

    public float GetCurrentCooldown()
    {
        switch (currentLevel)
        {
            case 1: return 5.0f;
            case 2: return 3.8f;
            case 3: return 2.5f;
            default: return baseCooldown;
        }
    }

    private bool TryShootSniper()
    {
        Transform target = FindClosestEnemy();
        Vector2 shootDirection;

        if (target != null)
        {
            shootDirection = (target.position - transform.position).normalized;
        }
        else
        {
            // Nếu không có quái gần, bắn theo hướng ngẫu nhiên hoặc hướng di chuyển
            PlayerMovement mov = GetComponent<PlayerMovement>();
            if (mov != null && mov.joystick != null && mov.joystick.Direction.sqrMagnitude > 0.01f)
            {
                shootDirection = mov.joystick.Direction.normalized;
            }
            else
            {
                return false; // Không bắn lãng phí khi chưa có mục tiêu
            }
        }

        SpawnSniperBullet(shootDirection);
        return true;
    }

    private void SpawnSniperBullet(Vector2 direction)
    {
        GameObject bullet = new GameObject("SniperBullet");
        bullet.transform.position = transform.position;
        bullet.transform.localScale = new Vector3(0.22f, 0.45f, 1f);

        // Góc xoay
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        bullet.transform.rotation = Quaternion.Euler(0, 0, angle);

        // Sprite
        SpriteRenderer sr = bullet.AddComponent<SpriteRenderer>();
        sr.sprite = ProceduralSkillAssets.GetSprite("SunflowerSeed"); // Dùng hình thoi/oval
        sr.color = new Color(1f, 0.9f, 0.2f, 1f); // Màu vàng sniper rực rỡ
        sr.sortingOrder = 15;

        // Collider & Rigidbody
        CircleCollider2D col = bullet.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.5f;

        Rigidbody2D rb = bullet.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0;

        // Logic bay & xuyên
        PiercingBulletLogic logic = bullet.AddComponent<PiercingBulletLogic>();
        float totalPlayerDmg = PlayerStats.Instance != null ? PlayerStats.Instance.GetTotalAttackDamage() : 12f;
        // Sát thương bắn tỉa xuyên thấu theo cấp: Lv 1: 140% + 4, Lv 2: 170% + 6, Lv 3: 200% + 8
        float ratio = 1.4f + (currentLevel - 1) * 0.3f;
        float flatBonus = 4f + (currentLevel - 1) * 2f;
        float finalDmg = (totalPlayerDmg * ratio) + flatBonus;
        logic.Init(direction, bulletSpeed, bulletLifeTime, finalDmg);
    }

    private Transform FindClosestEnemy()
    {
        Transform closest = null;
        float minDstSqr = Mathf.Infinity;
        Vector3 playerPos = transform.position;

        int count = EnemyHealth.ActiveEnemies.Count;
        for (int i = 0; i < count; i++)
        {
            EnemyHealth e = EnemyHealth.ActiveEnemies[i];
            if (e == null) continue;
            float d2 = (e.transform.position - playerPos).sqrMagnitude;
            if (d2 < minDstSqr)
            {
                minDstSqr = d2;
                closest = e.transform;
            }
        }
        return closest;
    }
}

/// <summary>
/// Đạn Sniper bay xuyên qua mọi quái trên đường thẳng mà không bị hủy khi va chạm
/// </summary>
public class PiercingBulletLogic : MonoBehaviour
{
    private Vector2 moveDir;
    private float speed;
    private float lifeTimer;
    private float maxLife;
    private float damage;
    private readonly System.Collections.Generic.HashSet<Collider2D> hitTargets = new System.Collections.Generic.HashSet<Collider2D>();

    public void Init(Vector2 dir, float spd, float life, float dmg)
    {
        moveDir = dir.normalized;
        speed = spd;
        maxLife = life;
        damage = dmg;
        lifeTimer = 0f;
    }

    void Update()
    {
        transform.position += (Vector3)(moveDir * speed * Time.deltaTime);

        lifeTimer += Time.deltaTime;
        if (lifeTimer >= maxLife)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            if (hitTargets.Contains(other)) return; // Mỗi quái chỉ dính đạn sniper 1 lần
            hitTargets.Add(other);

            EnemyHealth enemy = other.GetComponent<EnemyHealth>();
            if (enemy != null)
            {
                enemy.TakeDamage(Mathf.RoundToInt(damage), true, moveDir);
            }

            LootChest chest = other.GetComponent<LootChest>();
            if (chest != null)
            {
                chest.TakeDamage(1);
            }
        }
    }
}
