using UnityEngine;

/// <summary>
/// Quản lý Đấu Trường Khép Kín (Bounded Arena) phong cách game Brotato.
/// Định nghĩa ranh giới di chuyển, tường chặn va chạm và cung cấp API vị trí spawn an toàn.
/// Tự động thay đổi bản đồ phù hợp theo từng chế độ chơi (Thường, Khó, Vô Hạn, Dễ).
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
    public SpriteRenderer outerVoidRenderer;
    public Transform wallsRoot;
    public Transform cornersRoot;
    public Transform decorationsRoot;

    [Header("Themed Map Skins (Chuẩn Phong Cách Brotato)")]
    [Tooltip("Thường (Bình Thường): Rừng nguyên sinh huyền bí")]
    public Sprite forestFloorSprite;

    [Tooltip("Khó: Hỏa sơn và dung nham")]
    public Sprite volcanoFloorSprite;

    [Tooltip("Vô hạn (Vô Tận): Vực sâu hư không")]
    public Sprite voidFloorSprite;

    [Tooltip("Dễ / Mặc định: Đất cát Brotato nguyên bản")]
    public Sprite defaultFloorSprite;

    [Tooltip("Nền không gian sao trời cho chế độ Vực Sâu Hư Không")]
    public Sprite cosmicVoidSprite;

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

        if (outerVoidRenderer == null)
        {
            Transform voidObj = transform.Find("OuterVoid");
            if (voidObj != null) outerVoidRenderer = voidObj.GetComponent<SpriteRenderer>();
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
    /// Đồng bộ skin / texture sàn đấu tương ứng với từng cấp độ chơi (Thường, Khó, Vô Hạn, Dễ)
    /// </summary>
    public void ApplyLevelSkin()
    {
        if (floorRenderer == null) return;

#if UNITY_EDITOR
        // Tự động nạp sprite nếu chưa kéo tay trên Inspector
        if (forestFloorSprite == null)
            forestFloorSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/BrotatoMap/BrotatoFloor_Forest.png");
        if (volcanoFloorSprite == null)
            volcanoFloorSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/BrotatoMap/BrotatoFloor_Volcano.png");
        if (voidFloorSprite == null)
            voidFloorSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/BrotatoMap/BrotatoFloor_Void.png");
        if (defaultFloorSprite == null)
            defaultFloorSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/BrotatoMap/BrotatoFloor.png");
        if (cosmicVoidSprite == null)
            cosmicVoidSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/BrotatoMap/BrotatoOuterVoid_Cosmic.png");
#endif

        // 1. Nếu có Material tùy chỉnh từ GameDataManager
        if (GameDataManager.SelectedBackgroundMaterial != null)
        {
            floorRenderer.material = GameDataManager.SelectedBackgroundMaterial;
            return;
        }

        // 2. Hoặc Sprite tùy chỉnh thủ công từ GameDataManager
        if (GameDataManager.SelectedBackgroundSprite != null)
        {
            floorRenderer.sprite = GameDataManager.SelectedBackgroundSprite;
            return;
        }

        // 3. Tự động áp dụng theme tương ứng theo Độ Khó đã chọn từ Menu
        DifficultyLevel diff = GameDataManager.GetSelectedDifficulty();

        Sprite targetFloor = null;
        switch (diff)
        {
            case DifficultyLevel.Easy:
                targetFloor = defaultFloorSprite;
                break;
            case DifficultyLevel.Normal:
                // Thường: Rừng nguyên sinh huyền bí
                targetFloor = forestFloorSprite ?? defaultFloorSprite;
                break;
            case DifficultyLevel.Hard:
                // Khó: Hỏa sơn và dung nham
                targetFloor = volcanoFloorSprite ?? defaultFloorSprite;
                break;
            case DifficultyLevel.Endless:
                // Vô Hạn: Vực sâu hư không
                targetFloor = voidFloorSprite ?? defaultFloorSprite;
                break;
        }

        if (targetFloor != null)
        {
            floorRenderer.sprite = targetFloor;
        }

        // 4. Tùy biến OuterVoid (Vô Hạn có nền vũ trụ sao trời)
        if (outerVoidRenderer != null)
        {
            if (diff == DifficultyLevel.Endless && cosmicVoidSprite != null)
            {
                outerVoidRenderer.sprite = cosmicVoidSprite;
                outerVoidRenderer.color = Color.white;
            }
            else
            {
                outerVoidRenderer.color = Color.black;
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
