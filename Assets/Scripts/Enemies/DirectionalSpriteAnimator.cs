using UnityEngine;

public class DirectionalSpriteAnimator : MonoBehaviour
{
    public enum AnimMode
    {
        FourDirections_3x4, // Chuẩn 12 frames (3 cột x 4 hàng: Xuống, Trái, Phải, Lên)
        SideWalk_AutoFlip,  // Quái chỉ có 1 dãy frame đi ngang (tự động lật mặt Trái/Phải)
        Static_AutoFlip     // Quái 1 ảnh tĩnh duy nhất (như Enemy-ChoDo)
    }

    [Header("Chế độ hoạt ảnh (Universal)")]
    public AnimMode mode = AnimMode.FourDirections_3x4;

    [Header("Danh sách Sprites")]
    [Tooltip("Kéo các sprite đã cắt vào đây (12 frames cho 4 hướng, hoặc N frames cho 2 hướng)")]
    public Sprite[] sprites;
    public float frameRate = 0.15f;

    [Header("Cấu hình riêng cho 4 Hướng (Chuẩn RPG Maker)")]
    public int downRow = 0;  // Hàng 1 (0, 1, 2)
    public int leftRow = 1;  // Hàng 2 (3, 4, 5)
    public int rightRow = 2; // Hàng 3 (6, 7, 8)
    public int upRow = 3;    // Hàng 4 (9, 10, 11)

    [Header("Cấu hình cho Chế độ AutoFlip (2 hướng / Tĩnh)")]
    [Tooltip("Tích chọn nếu ảnh gốc của quái đang nhìn sang Trái, bỏ tích nếu nhìn sang Phải")]
    public bool defaultSpriteFacesLeft = false;

    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private Transform playerTransform;
    private Vector3 lastPosition;
    private Vector3 initialScale;

    private float timer;
    private int currentStep;
    private int currentRow = 0;

    // Chu kỳ bước chân 3 cột mượt mà: Trái (0) -> Giữa (1) -> Phải (2) -> Giữa (1)
    private readonly int[] walkCycle3 = { 0, 1, 2, 1 };

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        lastPosition = transform.position;
        initialScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);

        // Tìm Player để lấy hướng chính xác nhất ngay cả khi quái bị kẹt tường
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
    }

    // Dùng LateUpdate để ghi đè mọi lệnh flipX hoặc đổi Scale từ các script di chuyển cũ
    void LateUpdate()
    {
        if (spriteRenderer == null) return;

        // 1. Tính toán hướng di chuyển thực tế hoặc hướng nhìn về phía Player
        Vector2 moveDir = GetMovementDirection();
        bool isMoving = moveDir.sqrMagnitude > 0.0001f;

        // 2. Xử lý theo từng chế độ
        switch (mode)
        {
            case AnimMode.FourDirections_3x4:
                HandleFourDirections(moveDir, isMoving);
                break;

            case AnimMode.SideWalk_AutoFlip:
                HandleSideWalk(moveDir, isMoving);
                break;

            case AnimMode.Static_AutoFlip:
                HandleStaticFlip(moveDir);
                break;
        }
    }

    private Vector2 GetMovementDirection()
    {
        // Ưu tiên 1: Vận tốc vật lý từ Rigidbody2D
        if (rb != null && rb.linearVelocity.sqrMagnitude > 0.01f)
        {
            lastPosition = transform.position;
            return rb.linearVelocity;
        }

        // Ưu tiên 2: Độ lệch vị trí giữa 2 frame (nếu di chuyển bằng transform.position)
        Vector2 posDelta = (Vector2)(transform.position - lastPosition);
        lastPosition = transform.position;
        if (posDelta.sqrMagnitude > 0.00001f)
        {
            return posDelta;
        }

        // Ưu tiên 3: Nếu đứng sát hoặc bị đẩy, luôn hướng mặt về phía Player
        if (playerTransform != null)
        {
            return (Vector2)(playerTransform.position - transform.position);
        }

        return Vector2.zero;
    }

    private void HandleFourDirections(Vector2 moveDir, bool isMoving)
    {
        if (sprites == null || sprites.Length < 12) return;

        // Khóa hoàn toàn lệnh lật hình (flipX và Scale âm) của script di chuyển cũ
        spriteRenderer.flipX = false;
        if (transform.localScale.x < 0)
        {
            transform.localScale = initialScale;
        }

        if (isMoving)
        {
            // So sánh độ lớn trục X và Y để chọn hướng ưu tiên
            if (Mathf.Abs(moveDir.x) >= Mathf.Abs(moveDir.y))
            {
                currentRow = (moveDir.x > 0) ? rightRow : leftRow;
            }
            else
            {
                currentRow = (moveDir.y > 0) ? upRow : downRow;
            }

            // Chạy bộ đếm chuyển frame
            timer += Time.deltaTime;
            if (timer >= frameRate)
            {
                timer -= frameRate;
                currentStep = (currentStep + 1) % walkCycle3.Length;
            }
        }
        else
        {
            currentStep = 1; // Frame đứng yên ở giữa
        }

        int spriteIndex = (currentRow * 3) + walkCycle3[currentStep];
        if (spriteIndex >= 0 && spriteIndex < sprites.Length)
        {
            spriteRenderer.sprite = sprites[spriteIndex];
        }
    }

    private void HandleSideWalk(Vector2 moveDir, bool isMoving)
    {
        if (sprites == null || sprites.Length == 0) return;

        HandleStaticFlip(moveDir);

        if (isMoving)
        {
            timer += Time.deltaTime;
            if (timer >= frameRate)
            {
                timer -= frameRate;
                currentStep = (currentStep + 1) % sprites.Length;
            }
            spriteRenderer.sprite = sprites[currentStep];
        }
        else
        {
            spriteRenderer.sprite = sprites[0];
        }
    }

    private void HandleStaticFlip(Vector2 moveDir)
    {
        // Đảm bảo Scale dương, chỉ dùng flipX để lật ảnh gọn gàng
        if (transform.localScale.x < 0)
        {
            transform.localScale = initialScale;
        }

        if (Mathf.Abs(moveDir.x) > 0.01f)
        {
            bool movingLeft = moveDir.x < 0;
            spriteRenderer.flipX = defaultSpriteFacesLeft ? !movingLeft : movingLeft;
        }
    }
}