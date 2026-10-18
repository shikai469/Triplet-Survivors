using UnityEngine;

/// <summary>
/// Quản lý Đấu Trường Khép Kín (Bounded Arena) phong cách game Brotato.
/// Định nghĩa ranh giới di chuyển, tường chặn va chạm và cung cấp API vị trí spawn an toàn.
/// </summary>
public class BrotatoArena : MonoBehaviour
{
    public static BrotatoArena Instance { get; private set; }

    [Header("Arena Dimensions (Kích thước sàn đấu)")]
    [Tooltip("Chiều rộng sàn đấu tính bằng Unit (chuẩn Brotato: 38)")]
    public float arenaWidth = 38f;

    [Tooltip("Chiều cao sàn đấu tính bằng Unit (chuẩn Brotato: 24)")]
    public float arenaHeight = 24f;

    [Tooltip("Độ dày của tường bao quanh để chặn va chạm")]
    public float wallThickness = 2f;

    [Header("Visual References")]
    public SpriteRenderer floorRenderer;
    public Transform wallsRoot;
    public Transform cornersRoot;
    public Transform decorationsRoot;

    // Ranh giới khu vực chơi bên trong đấu trường (trừ đi nửa bề dày)
    public Bounds ArenaBounds
    {
        get
        {
            Vector3 center = transform.position;
            Vector3 size = new Vector3(arenaWidth, arenaHeight, 10f);
            return new Bounds(center, size);
        }
    }

    // Ranh giới di chuyển an toàn của người chơi (trừ biên an toàn khỏi chạm vào tường)
    public Rect PlayableRect
    {
        get
        {
            float halfW = arenaWidth * 0.5f;
            float halfH = arenaHeight * 0.5f;
            Vector3 pos = transform.position;
            return new Rect(pos.x - halfW, pos.y - halfH, arenaWidth, arenaHeight);
        }
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        ApplyLevelSkin();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// Đồng bộ skin / texture sàn đấu nếu người chơi chọn độ khó hoặc map khác nhau
    /// </summary>
    public void ApplyLevelSkin()
    {
        if (floorRenderer == null) return;

        // Nếu có Material tùy chỉnh từ GameDataManager
        if (GameDataManager.SelectedBackgroundMaterial != null)
        {
            floorRenderer.material = GameDataManager.SelectedBackgroundMaterial;
            return;
        }

        // Hoặc Sprite tùy chỉnh
        if (GameDataManager.SelectedBackgroundSprite != null)
        {
            floorRenderer.sprite = GameDataManager.SelectedBackgroundSprite;
            return;
        }

        // Hoặc kiểm tra GameStageManager active config
        if (GameStageManager.Instance != null && GameStageManager.Instance.activeConfig != null)
        {
            var config = GameStageManager.Instance.activeConfig;
            if (config.backgroundMaterialOverride != null)
            {
                floorRenderer.material = config.backgroundMaterialOverride;
            }
            else if (config.backgroundSpriteOverride != null)
            {
                floorRenderer.sprite = config.backgroundSpriteOverride;
            }
        }
    }

    /// <summary>
    /// Kẹp chặt vị trí bất kỳ vào bên trong ranh giới đấu trường, cách tường 1 khoảng padding
    /// </summary>
    public Vector3 ClampPosition(Vector3 pos, float padding = 0.5f)
    {
        float halfW = Mathf.Max(0.1f, (arenaWidth * 0.5f) - padding);
        float halfH = Mathf.Max(0.1f, (arenaHeight * 0.5f) - padding);
        Vector3 center = transform.position;

        float clampedX = Mathf.Clamp(pos.x, center.x - halfW, center.x + halfW);
        float clampedY = Mathf.Clamp(pos.y, center.y - halfH, center.y + halfH);

        return new Vector3(clampedX, clampedY, pos.z);
    }

    /// <summary>
    /// Kiểm tra xem 1 vị trí có nằm trọn bên trong đấu trường không
    /// </summary>
    public bool IsInsideArena(Vector3 pos, float padding = 0f)
    {
        float halfW = (arenaWidth * 0.5f) - padding;
        float halfH = (arenaHeight * 0.5f) - padding;
        Vector3 center = transform.position;

        return pos.x >= center.x - halfW && pos.x <= center.x + halfW &&
               pos.y >= center.y - halfH && pos.y <= center.y + halfH;
    }

    /// <summary>
    /// Lấy 1 vị trí ngẫu nhiên bất kỳ bên trong đấu trường
    /// </summary>
    public Vector3 GetRandomPositionInside(float padding = 1.5f)
    {
        float halfW = Mathf.Max(0.1f, (arenaWidth * 0.5f) - padding);
        float halfH = Mathf.Max(0.1f, (arenaHeight * 0.5f) - padding);
        Vector3 center = transform.position;

        float rx = Random.Range(center.x - halfW, center.x + halfW);
        float ry = Random.Range(center.y - halfH, center.y + halfH);
        return new Vector3(rx, ry, 0f);
    }

    /// <summary>
    /// Lấy vị trí sinh quái ở mép trong của 4 bờ tường đấu trường (chuẩn phong cách Brotato)
    /// </summary>
    public Vector3 GetBorderSpawnPosition(float edgeMargin = 1.0f)
    {
        float halfW = (arenaWidth * 0.5f) - edgeMargin;
        float halfH = (arenaHeight * 0.5f) - edgeMargin;
        Vector3 center = transform.position;

        int edge = Random.Range(0, 4); // 0: Top, 1: Bottom, 2: Left, 3: Right
        float x = 0f;
        float y = 0f;

        switch (edge)
        {
            case 0: // Top
                x = Random.Range(center.x - halfW, center.x + halfW);
                y = center.y + halfH;
                break;
            case 1: // Bottom
                x = Random.Range(center.x - halfW, center.x + halfW);
                y = center.y - halfH;
                break;
            case 2: // Left
                x = center.x - halfW;
                y = Random.Range(center.y - halfH, center.y + halfH);
                break;
            case 3: // Right
                x = center.x + halfW;
                y = Random.Range(center.y - halfH, center.y + halfH);
                break;
        }

        return new Vector3(x, y, 0f);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.8f);
        Gizmos.DrawWireCube(transform.position, new Vector3(arenaWidth, arenaHeight, 0f));

        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.5f);
        Gizmos.DrawWireCube(transform.position, new Vector3(arenaWidth + wallThickness * 2f, arenaHeight + wallThickness * 2f, 0f));
    }
#endif
}
