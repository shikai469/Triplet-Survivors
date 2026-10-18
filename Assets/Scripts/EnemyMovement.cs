using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    [Header("Movement & Attack Stats")]
    public float moveSpeed = 2f;
    public int attackDamage = 10;           // Sát thương gây ra cho Player (thay đổi theo độ khó)
    public float knockbackResistance = 50f; // Kháng đẩy lùi
    public float damageCooldown = 0.5f;     // Cooldown giữa các lần gây sát thương (giảm tải physics)

    private static Transform cachedPlayerTransform;
    private Transform player;
    private Vector2 knockbackVelocity;
    private float knockbackTimer = 0f;
    private float damageTimer = 0f;
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
        // Nếu đang bị đẩy lùi thì ưu tiên văng ra xa
        if (knockbackTimer > 0f)
        {
            transform.position += (Vector3)(knockbackVelocity * Time.deltaTime);
            knockbackTimer -= Time.deltaTime;
        }
        else if (player != null)
        {
            transform.position = Vector2.MoveTowards(transform.position, player.position, moveSpeed * Time.deltaTime);

            if (spriteRenderer != null)
            {
                if (player.position.x < transform.position.x)
                    spriteRenderer.flipX = true;
                else if (player.position.x > transform.position.x)
                    spriteRenderer.flipX = false;
            }
        }

        // Đảm bảo quái không bị giật lùi xuyên qua tường bao đấu trường Brotato
        if (BrotatoArena.Instance != null)
        {
            transform.position = BrotatoArena.Instance.ClampPosition(transform.position, 0.4f);
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