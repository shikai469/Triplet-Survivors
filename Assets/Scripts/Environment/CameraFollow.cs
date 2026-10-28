using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform player; // Kéo thả object Player vào đây
    public Vector3 offset = new Vector3(0, 0, -10f); // Giữ khoảng cách trục Z để camera không bị kẹt vào nhân vật

    [Header("Brotato Arena Bounds Clamping")]
    [Tooltip("Khóa góc nhìn camera không vượt ra ngoài mép đấu trường Brotato")]
    public bool clampToArena = true;

    [Tooltip("Làm mượt chuyển động camera (0 = đi theo tức thì)")]
    public float smoothSpeed = 0f;

    private Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void Start()
    {
        FindPlayerIfNull();
    }

    private void FindPlayerIfNull()
    {
        if (player == null)
        {
            if (PlayerStats.Instance != null)
            {
                player = PlayerStats.Instance.transform;
            }
            else
            {
                GameObject p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) player = p.transform;
            }
        }
    }

    // Sử dụng LateUpdate thay vì Update để đảm bảo Camera chỉ di chuyển SAU KHI nhân vật đã tính toán xong vật lý
    void LateUpdate()
    {
        if (player == null)
        {
            FindPlayerIfNull();
            if (player == null) return;
        }

        Vector3 targetPos = player.position + offset;

        // Giới hạn camera trong lòng đấu trường Brotato nếu có
        if (clampToArena && BrotatoArena.Instance != null)
        {
            if (cam == null) cam = GetComponent<Camera>();
            if (cam != null && cam.orthographic)
            {
                float camHalfHeight = cam.orthographicSize;
                float camHalfWidth = camHalfHeight * cam.aspect;
                Bounds bounds = BrotatoArena.Instance.ArenaBounds;

                float minX = bounds.min.x + camHalfWidth;
                float maxX = bounds.max.x - camHalfWidth;
                float minY = bounds.min.y + camHalfHeight;
                float maxY = bounds.max.y - camHalfHeight;

                targetPos.x = maxX >= minX ? Mathf.Clamp(targetPos.x, minX, maxX) : bounds.center.x;
                targetPos.y = maxY >= minY ? Mathf.Clamp(targetPos.y, minY, maxY) : bounds.center.y;
            }
        }

        if (smoothSpeed > 0f)
        {
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * smoothSpeed);
        }
        else
        {
            transform.position = targetPos;
        }
    }
}