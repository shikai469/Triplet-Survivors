using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    [Header("Movement & Attack Stats")]
    public float moveSpeed = 2f;
    public int attackDamage = 10;           // Sát thương gây ra cho Player (thay đổi theo độ khó)
    public float knockbackResistance = 50f; // Kháng đẩy lùi
    public float damageCooldown = 0.5f;     // Cooldown giữa các lần gây sát thương (giảm tải physics)

    [Header("Spawn Entrance Effect (Nhìn thấy quái chạy từ xa đến gần)")]
    public bool enableEntranceEffect = true;
    public float rushDuration = 0.8f;       // Thời gian quái tăng tốc lao vào trong lúc xuất hiện ở xa
    public float rushSpeedMultiplier = 1.4f; // Hệ số tăng tốc khi xuất hiện
    public float fadeDuration = 0.35f;      // Thời gian hiện hình mượt mà từ xa (Fade-In)

    private static Transform cachedPlayerTransform;
    private Transform player;
    private Vector2 knockbackVelocity;
    private float knockbackTimer = 0f;
    private float damageTimer = 0f;
    private float rushTimer = 0f;
    private float fadeTimer = 0f;
    private float arenaCheckTimer = 0f;
    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void OnEnable()
    {
        // Reset trạng thái khi lấy từ pool tái sử dụng
        knockbackVelocity = Vector2.zero;
        knockbackTimer = 0f;
        damageTimer = 0f;

        // Kích hoạt hiệu ứng quái lao từ xa vào và hiện hình mượt mà
        if (enableEntranceEffect)
        {
            rushTimer = rushDuration;
            fadeTimer = fadeDuration;
            if (spriteRenderer != null)
            {
                Color c = spriteRenderer.color;
                c.a = 0.2f;
                spriteRenderer.color = c;
            }
        }
        else if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = 1.0f;
            spriteRenderer.color = c;
        }

        // Tối ưu: Dùng static cache để hàng trăm quái không phải scan tìm player
        if (cachedPlayerTransform == null)
        {
            if (PlayerStats.Instance != null)
                cachedPlayerTransform = PlayerStats.Instance.transform;
            else
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null) cachedPlayerTransform = playerObj.transform;
            }
        }
        player = cachedPlayerTransform;
    }

    void OnDisable()
    {
        // Khôi phục lại alpha ban đầu khi trả về pool
        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = 1.0f;
            spriteRenderer.color = c;
        }
    }

    void Start()
    {
        if (player == null)
        {
            if (PlayerStats.Instance != null)
                player = PlayerStats.Instance.transform;
        }
    }

    public void InitMovement(float speed, int damage, float knockback)
    {
        moveSpeed = speed;
        attackDamage = damage;
        knockbackResistance = knockback;
    }

    // Hiệu ứng giật lùi khi trúng đạn (Section 3.4)
    public void ApplyKnockback(Vector2 hitDirection, float resistance)
    {
        if (resistance <= 0f) return; // Quái miễn nhiễm đẩy lùi hoàn toàn
        float force = (resistance / 50f) * 2.5f;
        knockbackVelocity = hitDirection.normalized * force;
        knockbackTimer = 0.12f;
    }

    void Update()
    {
        // 1. Hiệu ứng Fade-In mượt mà khi xuất hiện từ ngoài tầm nhìn
        if (fadeTimer > 0f && spriteRenderer != null)
        {
            fadeTimer -= Time.deltaTime;
            float progress = Mathf.Clamp01(1f - (fadeTimer / fadeDuration));
            Color c = spriteRenderer.color;
            c.a = Mathf.Lerp(0.2f, 1.0f, progress);
            spriteRenderer.color = c;
        }

        // 2. Nếu đang bị đẩy lùi thì ưu tiên văng ra xa
        if (knockbackTimer > 0f)
        {
            transform.position += (Vector3)(knockbackVelocity * Time.deltaTime);
            knockbackTimer -= Time.deltaTime;

            if (BrotatoArena.Instance != null)
            {
                transform.position = BrotatoArena.Instance.ClampPosition(transform.position, 0.4f);
            }
        }
        else if (player != null)
        {
            // Tính toán tốc độ: nếu đang trong thời gian xuất hiện (rushTimer) thì quái phi nhanh hơn từ xa vào
            float currentSpeed = moveSpeed;
            if (rushTimer > 0f)
            {
                rushTimer -= Time.deltaTime;
                float rushFactor = Mathf.Clamp01(rushTimer / rushDuration);
                currentSpeed = moveSpeed * Mathf.Lerp(1.0f, rushSpeedMultiplier, rushFactor);
            }

            transform.position = Vector2.MoveTowards(transform.position, player.position, currentSpeed * Time.deltaTime);

            // Tối ưu: Chỉ đổi flipX khi hướng thay đổi để tránh ghi đè liên tục mỗi frame
            if (spriteRenderer != null)
            {
                bool shouldFlip = player.position.x < transform.position.x;
                if (spriteRenderer.flipX != shouldFlip)
                {
                    spriteRenderer.flipX = shouldFlip;
                }
            }

            // Tối ưu hiệu năng: Giãn cách kiểm tra tường Brotato mỗi 0.2s thay vì mỗi frame cho hàng trăm con quái
            arenaCheckTimer -= Time.deltaTime;
            if (arenaCheckTimer <= 0f)
            {
                arenaCheckTimer = 0.2f;
                if (BrotatoArena.Instance != null)
                {
                    transform.position = BrotatoArena.Instance.ClampPosition(transform.position, 0.4f);
                }
            }
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // Cooldown chống spam damage mỗi physics frame (giảm tải CPU mobile)
            damageTimer += Time.deltaTime;
            if (damageTimer >= damageCooldown)
            {
                damageTimer = 0f;
                if (PlayerHealth.Instance != null)
                {
                    PlayerHealth.Instance.TakeDamage(attackDamage);
                }
                else
                {
                    PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
                    if (playerHealth != null)
                    {
                        playerHealth.TakeDamage(attackDamage);
                    }
                }
            }
        }
    }
}
