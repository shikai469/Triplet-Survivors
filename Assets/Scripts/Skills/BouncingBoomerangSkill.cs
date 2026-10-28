using UnityEngine;

/// <summary>
/// Kỹ năng 8: Boomerang Dội Tường (Bouncing Boomerang) [Max Lv 3]
/// Cơ chế: Ném ra Boomerang. Khi đập vào cạnh màn hình hoặc tường đấu trường, nó dội ngược lại và quay trở về vị trí người chơi.
/// Tiến trình: Lên cấp tăng số lượng Boomerang ném ra (Lv 1: 1, Lv 2: 2, Lv 3: 3).
/// </summary>
public class BouncingBoomerangSkill : MonoBehaviour
{
    [Header("Skill Level")]
    public int currentLevel = 0;
    public const int MaxLevel = 3;

    [Header("Settings")]
    public float throwCooldown = 3.2f;
    public float boomerangSpeed = 10f;
    public float baseDamage = 28f;

    private float throwTimer = 0f;

    public void SetLevel(int level)
    {
        currentLevel = Mathf.Clamp(level, 0, MaxLevel);
        enabled = (currentLevel > 0);
    }

    void Update()
    {
        if (currentLevel <= 0) return;

        throwTimer += Time.deltaTime;
        if (throwTimer >= throwCooldown)
        {
            ThrowBoomerangs();
            throwTimer = 0f;
        }
    }

    private void ThrowBoomerangs()
    {
        int count = currentLevel; // 1..3 boomerang
        Vector2 baseDir = Vector2.right;

        // Ưu tiên ném về phía quái gần nhất
        Transform closestEnemy = FindClosestEnemy();
        if (closestEnemy != null)
        {
            baseDir = (closestEnemy.position - transform.position).normalized;
        }
        else
        {
            // Bắn theo hướng di chuyển nếu có
            PlayerMovement mov = GetComponent<PlayerMovement>();
            if (mov != null && mov.joystick != null && mov.joystick.Direction.sqrMagnitude > 0.01f)
            {
                baseDir = mov.joystick.Direction.normalized;
            }
        }

        float spreadAngle = 20f;
        float startAngle = -((count - 1) * spreadAngle) * 0.5f;

        for (int i = 0; i < count; i++)
        {
            float currentAng = startAngle + i * spreadAngle;
            Quaternion rot = Quaternion.Euler(0, 0, currentAng);
            Vector2 dir = rot * baseDir;

            SpawnSingleBoomerang(dir);
        }
    }

    private void SpawnSingleBoomerang(Vector2 dir)
    {
        GameObject bm = new GameObject("BoomerangProjectile");
        bm.transform.position = transform.position;
        bm.transform.localScale = new Vector3(1f, 1f, 1f);

        SpriteRenderer sr = bm.AddComponent<SpriteRenderer>();
        sr.sprite = ProceduralSkillAssets.GetSprite("Boomerang");
        sr.sortingOrder = 14;

        CircleCollider2D col = bm.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.5f;

        Rigidbody2D rb = bm.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        BoomerangProjectileLogic logic = bm.AddComponent<BoomerangProjectileLogic>();
        float strength = PlayerStats.Instance != null ? PlayerStats.Instance.strengthBonus : 0f;
        float finalDmg = baseDamage * (1f + strength);

        logic.Launch(transform, dir, boomerangSpeed, finalDmg);
    }

    private Transform FindClosestEnemy()
    {
        Transform closest = null;
        float minDstSqr = 12f * 12f;
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
        return closest;
    }
}

/// <summary>
/// Quản lý đường bay, dội tường và quay về người chơi của Boomerang
/// </summary>
public class BoomerangProjectileLogic : MonoBehaviour
{
    private Transform playerTransform;
    private Vector2 currentDir;
    private float speed;
    private float damage;
    private bool hasBounced = false;
    private float lifeTimer = 0f;
    private const float MaxLifetime = 4.5f;
    private readonly System.Collections.Generic.Dictionary<Collider2D, float> hitCooldowns = new System.Collections.Generic.Dictionary<Collider2D, float>();

    public void Launch(Transform player, Vector2 initialDir, float spd, float dmg)
    {
        playerTransform = player;
        currentDir = initialDir.normalized;
        speed = spd;
        damage = dmg;
        hasBounced = false;
        lifeTimer = 0f;
    }

    void Update()
    {
        lifeTimer += Time.deltaTime;

        // Boomerang xoay tít
        transform.Rotate(0, 0, 720f * Time.deltaTime);

        // Kiểm tra dội tường hoặc dội ranh giới đấu trường
        if (!hasBounced)
        {
            CheckBoundaryBounce();
        }
        else
        {
            // Sau khi đã dội: Uốn lượn quay trở về người chơi
            if (playerTransform != null)
            {
                Vector2 toPlayer = (playerTransform.position - transform.position).normalized;
                currentDir = Vector2.Lerp(currentDir, toPlayer, Time.deltaTime * 6f).normalized;

                // Nếu chạm lại người chơi thì hoàn thành vòng lặp ném
                if (Vector2.Distance(transform.position, playerTransform.position) < 0.8f && lifeTimer > 0.8f)
                {
                    Destroy(gameObject);
                    return;
                }
            }
        }

        transform.position += (Vector3)(currentDir * speed * Time.deltaTime);

        if (lifeTimer >= MaxLifetime)
        {
            Destroy(gameObject);
        }
    }

    private void CheckBoundaryBounce()
    {
        Vector3 pos = transform.position;

        // 1. Kiểm tra ranh giới đấu trường BrotatoArena
        if (BrotatoArena.Instance != null)
        {
            Rect arenaRect = BrotatoArena.Instance.PlayableRect;
            bool bounced = false;
            Vector2 normal = Vector2.zero;

            if (pos.x >= arenaRect.xMax)
            {
                normal = Vector2.left;
                bounced = true;
            }
            else if (pos.x <= arenaRect.xMin)
            {
                normal = Vector2.right;
                bounced = true;
            }
            else if (pos.y >= arenaRect.yMax)
            {
                normal = Vector2.down;
                bounced = true;
            }
            else if (pos.y <= arenaRect.yMin)
            {
                normal = Vector2.up;
                bounced = true;
            }

            if (bounced)
            {
                TriggerBounce(normal);
                return;
            }
        }

        // 2. Fallback: Nếu không có BrotatoArena, dùng ranh giới Camera màn hình
        Camera cam = Camera.main;
        if (cam != null && BrotatoArena.Instance == null)
        {
            Vector3 viewportPos = cam.WorldToViewportPoint(pos);
            if (viewportPos.x <= 0.05f || viewportPos.x >= 0.95f || viewportPos.y <= 0.05f || viewportPos.y >= 0.95f)
            {
                Vector2 normal = (Vector3.zero - pos).normalized;
                TriggerBounce(normal);
            }
        }
    }

    private void TriggerBounce(Vector2 wallNormal)
    {
        hasBounced = true;
        currentDir = Vector2.Reflect(currentDir, wallNormal).normalized;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        HandleHit(other);

        // Nếu va chạm với Tường vật lý trong game
        if (other.CompareTag("Wall") || other.name.Contains("Wall"))
        {
            if (!hasBounced)
            {
                Vector2 normal = (transform.position - other.bounds.center).normalized;
                TriggerBounce(normal);
            }
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        HandleHit(other);
    }

    private void HandleHit(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            float now = Time.time;
            if (hitCooldowns.TryGetValue(other, out float lastHit) && (now - lastHit) < 0.25f)
            {
                return;
            }
            hitCooldowns[other] = now;

            EnemyHealth enemy = other.GetComponent<EnemyHealth>();
            if (enemy != null)
            {
                enemy.TakeDamage(Mathf.RoundToInt(damage), false, currentDir);
            }

            LootChest chest = other.GetComponent<LootChest>();
            if (chest != null)
            {
                chest.TakeDamage(1);
            }
        }
    }
}
