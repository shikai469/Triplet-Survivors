using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    [Header("Movement & Attack Stats")]
    public float moveSpeed = 2f;
    public int attackDamage = 10;           // Sát thương gây ra cho Player (thay đổi theo độ khó)
    public float knockbackResistance = 50f; // Kháng đẩy lùi

    private Transform player;
    private Vector2 knockbackVelocity;
    private float knockbackTimer = 0f;

    void Start()
    {
        if (PlayerStats.Instance != null)
        {
            player = PlayerStats.Instance.transform;
        }
        else
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
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
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
            }
        }
    }
}