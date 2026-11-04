using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public FloatingJoystick joystick;
    public float moveSpeed = 5f;
    private Rigidbody2D rb;

    // Các biến trạng thái bổ trợ hiệu ứng debuff (Trượt vỏ chuối, Làm chậm, Đẩy lùi)
    private float currentSlowMultiplier = 1f;
    private Coroutine slowCoroutine;
    private float slipTimer = 0f;
    private float maxSlipDuration = 0.8f;
    private Vector2 slipVelocity = Vector2.zero;
    private float knockbackTimer = 0f;
    private Vector2 knockbackVelocity = Vector2.zero;

    void Start()
    {
        // Lấy tham chiếu đến component vật lý đã gắn trên Player
        rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        // 1. Ưu tiên xử lý hiệu ứng Đẩy Lùi (Knockback từ sóng âm hoặc cú húc)
        if (knockbackTimer > 0f)
        {
            knockbackTimer -= Time.fixedDeltaTime;
            rb.linearVelocity = knockbackVelocity;
            return;
        }

        // 2. Xử lý hiệu ứng Trượt Vỏ Chuối (Mất đà trượt quán tính)
        if (slipTimer > 0f)
        {
            slipTimer -= Time.fixedDeltaTime;
            float progress = Mathf.Clamp01(1f - (slipTimer / maxSlipDuration));
            // Trượt chậm dần theo quán tính
            Vector2 currentSlip = Vector2.Lerp(slipVelocity, Vector2.zero, progress);

            // Cho phép người chơi hơi bẻ lái một chút khi đang trượt
            Vector2 steer = joystick != null ? new Vector2(joystick.Horizontal, joystick.Vertical) : Vector2.zero;
            rb.linearVelocity = currentSlip + (steer.normalized * (moveSpeed * 0.3f));
            return;
        }

        // 3. Di chuyển thông thường nhận từ Joystick kết hợp hệ số làm chậm
        Vector2 direction = joystick != null ? new Vector2(joystick.Horizontal, joystick.Vertical) : Vector2.zero;
        rb.linearVelocity = direction.normalized * (moveSpeed * currentSlowMultiplier);
    }

    /// <summary>
    /// Áp dụng hiệu ứng làm chậm tốc độ chạy trong khoảng thời gian nhất định
    /// </summary>
    public void ApplySlow(float multiplier, float duration)
    {
        if (slowCoroutine != null) StopCoroutine(slowCoroutine);
        slowCoroutine = StartCoroutine(SlowRoutine(multiplier, duration));
    }

    private IEnumerator SlowRoutine(float multiplier, float duration)
    {
        currentSlowMultiplier = Mathf.Clamp(multiplier, 0.2f, 1f);
        yield return new WaitForSeconds(duration);
        currentSlowMultiplier = 1f;
        slowCoroutine = null;
    }

    /// <summary>
    /// Áp dụng hiệu ứng trượt ngã (mất lái) theo hướng nhất định (khi giẫm vỏ chuối)
    /// </summary>
    public void ApplySlip(Vector2 direction, float force, float duration = 0.7f)
    {
        slipTimer = duration;
        maxSlipDuration = duration > 0f ? duration : 0.7f;
        slipVelocity = direction.sqrMagnitude > 0.01f ? direction.normalized * force : (Vector2)transform.up * force;
    }

    /// <summary>
    /// Áp dụng hiệu ứng đẩy lùi từ đòn đánh bão tố hoặc sóng âm của Boss
    /// </summary>
    public void ApplyKnockback(Vector2 direction, float force, float duration = 0.25f)
    {
        knockbackTimer = duration;
        knockbackVelocity = direction.normalized * force;
    }
}
