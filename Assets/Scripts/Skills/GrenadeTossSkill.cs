using UnityEngine;

/// <summary>
/// Kỹ năng 6: Ném Lựu Đạn (Grenade Toss) [Max Lv 3]
/// Cơ chế: Theo chu kỳ, tự động ném ra quả lựu đạn gây nổ diện rộng (AOE) tiêu diệt đám đông quái.
/// Tiến trình: Lên cấp giúp tăng tốc độ ném (rút ngắn thời gian chờ giữa 2 lần ném lựu đạn).
/// </summary>
public class GrenadeTossSkill : MonoBehaviour
{
    [Header("Skill Level")]
    public int currentLevel = 0;
    public const int MaxLevel = 3;

    [Header("Grenade Settings")]
    public float baseDamage = 50f;
    public float explosionRadius = 2.5f;

    private float tossTimer = 0f;

    public void SetLevel(int level)
    {
        currentLevel = Mathf.Clamp(level, 0, MaxLevel);
        enabled = (currentLevel > 0);
    }

    void Update()
    {
        if (currentLevel <= 0) return;

        float cooldown = GetCurrentCooldown();
        tossTimer += Time.deltaTime;

        if (tossTimer >= cooldown)
        {
            Transform target = FindTargetEnemy();
            if (target != null)
            {
                TossGrenade(target.position);
                tossTimer = 0f;
            }
        }
    }

    public float GetCurrentCooldown()
    {
        switch (currentLevel)
        {
            case 1: return 5.0f;
            case 2: return 3.5f;
            case 3: return 2.0f; // Rút ngắn thời gian ném tối đa
            default: return 5.0f;
        }
    }

    private Transform FindTargetEnemy()
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

    private void TossGrenade(Vector3 targetPos)
    {
        GameObject grenade = new GameObject("GrenadeProjectile");
        grenade.transform.position = transform.position;
        grenade.transform.localScale = new Vector3(0.7f, 0.7f, 1f);

        SpriteRenderer sr = grenade.AddComponent<SpriteRenderer>();
        sr.sprite = ProceduralSkillAssets.GetSprite("Grenade");
        sr.sortingOrder = 14;

        GrenadeProjectileLogic logic = grenade.AddComponent<GrenadeProjectileLogic>();
        float totalPlayerDmg = PlayerStats.Instance != null ? PlayerStats.Instance.GetTotalAttackDamage() : 12f;
        // Sát thương nổ AOE theo cấp: Lv 1: 130% + 5, Lv 2: 160% + 7, Lv 3: 190% + 10
        float ratio = 1.3f + (currentLevel - 1) * 0.3f;
        float flatBonus = 5f + (currentLevel - 1) * 2.5f;
        float finalDmg = (totalPlayerDmg * ratio) + flatBonus;

        logic.Launch(transform.position, targetPos, 0.65f, explosionRadius, finalDmg);
    }
}

/// <summary>
/// Quản lý quỹ đạo bay hình parabol và vụ nổ AOE của lựu đạn
/// </summary>
public class GrenadeProjectileLogic : MonoBehaviour
{
    private Vector3 startPos;
    private Vector3 targetPos;
    private float flightDuration;
    private float elapsedTime;
    private float radius;
    private float damage;
    private float arcHeight = 1.8f;

    public void Launch(Vector3 start, Vector3 target, float duration, float expRadius, float dmg)
    {
        startPos = start;
        targetPos = target;
        flightDuration = duration;
        radius = expRadius;
        damage = dmg;
        elapsedTime = 0f;
    }

    void Update()
    {
        elapsedTime += Time.deltaTime;
        float t = Mathf.Clamp01(elapsedTime / flightDuration);

        // Nội suy tuyến tính x, y
        Vector3 currentGroundPos = Vector3.Lerp(startPos, targetPos, t);
        // Đường cong Parabol theo chiều cao
        float height = Mathf.Sin(t * Mathf.PI) * arcHeight;
        transform.position = currentGroundPos + new Vector3(0, height, 0);

        // Xoay tròn lựu đạn trên không
        transform.Rotate(0, 0, 360f * Time.deltaTime * 2.5f);

        if (t >= 1.0f)
        {
            Explode();
        }
    }

    private void Explode()
    {
        Vector3 expCenter = targetPos;

        // Quét quái trong bán kính nổ
        Collider2D[] colliders = Physics2D.OverlapCircleAll(expCenter, radius);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D col = colliders[i];
            if (col.CompareTag("Enemy"))
            {
                EnemyHealth enemy = col.GetComponent<EnemyHealth>();
                if (enemy != null)
                {
                    Vector2 hitDir = (col.transform.position - expCenter).normalized;
                    enemy.TakeDamage(Mathf.RoundToInt(damage), true, hitDir);
                }

                LootChest chest = col.GetComponent<LootChest>();
                if (chest != null)
                {
                    chest.TakeDamage(1);
                }
            }
        }

        // Tạo hiệu ứng nổ nảy visual flash AOE
        CreateExplosionVFX(expCenter);
        Destroy(gameObject);
    }

    private void CreateExplosionVFX(Vector3 pos)
    {
        GameObject fx = new GameObject("ExplosionFX");
        fx.transform.position = pos;

        SpriteRenderer sr = fx.AddComponent<SpriteRenderer>();
        sr.sprite = ProceduralSkillAssets.GetSprite("EnergyShieldCircle");
        sr.color = new Color(1f, 0.45f, 0.1f, 0.9f);
        sr.sortingOrder = 16;
        fx.transform.localScale = Vector3.one * (radius * 0.8f);

        // Tự biến mất và phồng to sau 0.25s
        ExplosionFXLogic anim = fx.AddComponent<ExplosionFXLogic>();
        anim.targetScale = radius * 2.0f;
    }
}

public class ExplosionFXLogic : MonoBehaviour
{
    public float targetScale = 5f;
    private float timer = 0f;
    private SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        timer += Time.deltaTime;
        float progress = timer / 0.25f;

        transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * targetScale, progress);
        if (sr != null)
        {
            Color c = sr.color;
            c.a = Mathf.Lerp(0.9f, 0f, progress);
            sr.color = c;
        }

        if (progress >= 1f)
        {
            Destroy(gameObject);
        }
    }
}
