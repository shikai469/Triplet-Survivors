using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public FloatingJoystick joystick;
    public float moveSpeed = 5f;
    private Rigidbody2D rb;

    void Start()
    {
        // Lấy tham chiếu đến component vật lý đã gắn trên Player
        rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        // Nhận giá trị vector từ Joystick
        Vector2 direction = new Vector2(joystick.Horizontal, joystick.Vertical);

        // Di chuyển nhân vật (Dùng linearVelocity cho Unity 6)
        rb.linearVelocity = direction.normalized * moveSpeed;
    }
}