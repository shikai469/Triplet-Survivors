using UnityEngine;

/// <summary>
/// Kỹ năng 5: Hải Âu Trợ Chiến (Seagull Ally) [Max Lv 3]
/// Cơ chế: Triệu hồi một chú chim hải âu bay kè kè bên cạnh người chơi, tự động nhả đạn "hạt hướng dương" về phía kẻ địch.
/// Tiến trình: Lên cấp giúp tăng tầm bắn của đạn hướng dương (đạt tối đa ở Lv 3).
/// </summary>
public class SeagullAllySkill : MonoBehaviour
{
    [Header("Skill Level")]
    public int currentLevel = 0;
    public const int MaxLevel = 3;

    [Header("Settings")]
    public float baseDamage = 18f;
    public Sprite customSeagullSprite;

    private GameObject seagullInstance;
    private SeagullCompanion companionScript;

    public void SetLevel(int level)
    {
        currentLevel = Mathf.Clamp(level, 0, MaxLevel);
        enabled = (currentLevel > 0);

        if (currentLevel > 0)
        {
            if (seagullInstance == null)
            {
                SpawnSeagull();
            }
            if (companionScript != null)
            {
                companionScript.UpdateLevel(currentLevel, baseDamage);
            }
        }
        else
        {
            if (seagullInstance != null)
            {
                Destroy(seagullInstance);
                seagullInstance = null;
            }
        }
    }

    private void SpawnSeagull()
    {
        seagullInstance = new GameObject("SeagullCompanion");
        seagullInstance.transform.position = transform.position + new Vector3(-0.8f, 1.2f, 0f);

        SpriteRenderer sr = seagullInstance.AddComponent<SpriteRenderer>();
        sr.sprite = customSeagullSprite != null ? customSeagullSprite : ProceduralSkillAssets.GetSprite("Seagull");
        sr.sortingOrder = 14;
        seagullInstance.transform.localScale = new Vector3(1.1f, 1.1f, 1f);

        companionScript = seagullInstance.AddComponent<SeagullCompanion>();
        companionScript.owner = transform;
        companionScript.UpdateLevel(currentLevel, baseDamage);
    }

    void OnDisable()
    {
        if (seagullInstance != null)
        {
            Destroy(seagullInstance);
            seagullInstance = null;
        }
    }
}

/// <summary>
/// Quản lý di chuyển bay lượn và hành vi nhả đạn hạt hướng dương của Hải Âu
/// </summary>
public class SeagullCompanion : MonoBehaviour
{
    public Transform owner;
    public float attackRange = 5f;
    public float fireRate = 1.0f;
    public float damage = 18f;

    private float fireTimer = 0f;
    private Vector3 currentVelocity;
    private Vector3 targetOffset = new Vector3(-0.8f, 1.1f, 0f);
    private SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    private int currentLevel = 1;

    public void UpdateLevel(int level, float baseDmg)
    {
        currentLevel = level;

        // Cập nhật tầm bắn và tốc độ bắn theo cấp
        switch (level)
        {
            case 1:
                attackRange = 5.0f;
                fireRate = 1.1f;
                break;
            case 2:
                attackRange = 8.0f;
                fireRate = 0.95f;
                break;
            case 3:
                attackRange = 12.0f; // Tối đa tầm bắn ở Lv 3
                fireRate = 0.8f;
                break;
        }
    }

    void Update()
    {
        if (owner == null) return;

        // Chuyển động bay kè kè và lượn sóng nhẹ nhàng
        float wave = Mathf.Sin(Time.time * 4f) * 0.18f;
        Vector3 desiredPos = owner.position + targetOffset + new Vector3(0f, wave, 0f);
        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref currentVelocity, 0.2f);

        // Bắn đạn hạt hướng dương định kỳ
        fireTimer += Time.deltaTime;
        if (fireTimer >= fireRate)
        {
            Transform target = FindClosestEnemyInRange();
            if (target != null)
            {
                ShootSunflowerSeed(target);
                fireTimer = 0f;
            }
        }
    }

    private Transform FindClosestEnemyInRange()
    {
        Transform closest = null;
        float minDstSqr = attackRange * attackRange;
        Vector3 pos = transform.position;

        int count = EnemyHealth.ActiveEnemies.Count;
        for (int i = 0; i < count; i++)
        {
            EnemyHealth e = EnemyHealth.ActiveEnemies[i];
            if (e == null) continue;
            float d2 = (e.transform.position - pos).sqrMagnitude;
            if (d2 < minDstSqr)
            {
                minDstSqr = d2;
                closest = e.transform;
            }
        }

        // Quét thêm rương
        int chestCount = LootChest.ActiveChests.Count;
        for (int i = 0; i < chestCount; i++)
        {
            LootChest c = LootChest.ActiveChests[i];
            if (c == null) continue;
            float d2 = (c.transform.position - pos).sqrMagnitude;
            if (d2 < minDstSqr)
            {
                minDstSqr = d2;
                closest = c.transform;
            }
        }

        return closest;
    }

    private void ShootSunflowerSeed(Transform target)
    {
        Vector2 dir = (target.position - transform.position).normalized;

        // Quay mặt Hải Âu về phía mục tiêu
        if (sr != null)
        {
            sr.flipX = (dir.x < 0);
        }

        GameObject seed = new GameObject("SunflowerSeedBullet");
        seed.transform.position = transform.position;
        seed.transform.localScale = new Vector3(0.18f, 0.28f, 1f);

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        seed.transform.rotation = Quaternion.Euler(0, 0, angle);

        SpriteRenderer seedSr = seed.AddComponent<SpriteRenderer>();
        seedSr.sprite = ProceduralSkillAssets.GetSprite("SunflowerSeed");
        seedSr.sortingOrder = 13;

        CircleCollider2D col = seed.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.5f;

        Rigidbody2D rb = seed.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        SunflowerSeedLogic logic = seed.AddComponent<SunflowerSeedLogic>();
        float totalPlayerDmg = PlayerStats.Instance != null ? PlayerStats.Instance.GetTotalAttackDamage() : 12f;
        // Đạn hạt hướng dương theo cấp: Lv 1: 55% + 1, Lv 2: 70% + 2, Lv 3: 85% + 3
        float ratio = 0.55f + (currentLevel - 1) * 0.15f;
        float flatBonus = 1f + (currentLevel - 1) * 1f;
        float finalDmg = (totalPlayerDmg * ratio) + flatBonus;
        logic.Init(dir, 11f, 3f, finalDmg);
    }
}

/// <summary>
/// Logic đạn Hạt Hướng Dương
/// </summary>
public class SunflowerSeedLogic : MonoBehaviour
{
    private Vector2 moveDir;
    private float speed;
    private float maxLife;
    private float lifeTimer;
    private float damage;

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
            EnemyHealth enemy = other.GetComponent<EnemyHealth>();
            if (enemy != null)
            {
                enemy.TakeDamage(Mathf.RoundToInt(damage), false, moveDir);
            }

            LootChest chest = other.GetComponent<LootChest>();
            if (chest != null)
            {
                chest.TakeDamage(1);
            }

            Destroy(gameObject);
        }
    }
}
