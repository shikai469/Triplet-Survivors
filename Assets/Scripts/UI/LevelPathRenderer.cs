using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Vẽ đường nối dạng Candy Crush thẳng giữa các nút Level trong LevelPathContainer khi thu gọn (Ảnh 2).
/// • Không yêu cầu Canvas riêng — tự tìm Canvas cha gần nhất.
/// • Tự động tìm 4 nút con (Easy/Normal/Hard/Endless) trong transform nếu levelNodes chưa gán.
/// • Tự động rebuild khi vị trí node thay đổi (ví dụ: slide animation).
/// • Vẽ dải đường gạch kem viền đá + chấm sáng nhấp nháy đặc trưng Candy Crush nối thẳng các nút.
/// Gắn script này vào GameObject LevelPathContainer.
/// </summary>
public class LevelPathRenderer : MonoBehaviour
{
    [Header("Các nút Level theo thứ tự: Easy → Normal → Hard → Endless")]
    [Tooltip("Nếu để trống, script tự tìm tất cả Button con theo thứ tự sibling trong LevelPathContainer.")]
    public List<RectTransform> levelNodes = new List<RectTransform>();

    [Header("Hiển thị đường nối thẳng Candy Crush khi thu gọn (Ảnh 2)")]
    [Tooltip("Chỉ vẽ đường nối khi các nút đã thu gọn sang bên trái (x <= -200) hoặc khi LevelDetailPanel mở")]
    public bool showOnlyWhenRetracted = true;

    [Tooltip("Ngưỡng tọa độ X được coi là đã thu gọn")]
    public float retractedThresholdX = -200f;

    [Header("Cài đặt dải đường Candy Crush")]
    [Tooltip("Vẽ mặt đường gạch màu kem Candy Crush bên dưới các chấm")]
    public bool renderRoadSurface = true;
    public float roadBorderWidth = 36f;
    public float roadSurfaceWidth = 26f;
    public Color roadBorderColor = new Color(0.68f, 0.48f, 0.25f, 1f);
    public Color roadSurfaceColor = new Color(0.98f, 0.92f, 0.74f, 1f);
    public Color roadHighlightColor = new Color(1f, 0.98f, 0.88f, 0.75f);

    [Header("Cài đặt chấm tròn Candy Crush")]
    [Tooltip("Kích thước mỗi chấm (pixels)")]
    public float dotSize = 14f;

    [Tooltip("Khoảng cách giữa tâm 2 chấm liên tiếp (pixels)")]
    public float dotSpacing = 28f;

    [Tooltip("Màu chấm chính")]
    public Color dotColor = new Color(1f, 0.92f, 0.45f, 0.95f);

    [Tooltip("Màu viền ngoài chấm")]
    public Color dotBorderColor = new Color(0.55f, 0.38f, 0.12f, 0.75f);

    [Header("Animation nhấp nháy")]
    public bool animateDots = true;
    public float animSpeed  = 1.5f;

    // ── Internal ──────────────────────────────────────────────────────────────
    private readonly List<GameObject> spawnedRoadObjects = new List<GameObject>();
    private readonly List<Image>      dotImages          = new List<Image>();
    private readonly List<Vector3>    cachedPositions    = new List<Vector3>();

    private Canvas        parentCanvas;
    private RectTransform containerRect;
    private float         animOffset = 0f;
    private bool          forceShowStraightRoad = false;

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    void Awake()
    {
        containerRect = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();
    }

    void Start()
    {
        AutoFindNodes();
        RebuildPath();
        CacheNodePositions();
    }

    void Update()
    {
        if (animateDots)
            animOffset = (animOffset + Time.deltaTime * animSpeed) % 1f;

        if (NodePositionsChanged())
        {
            RebuildPath();
            CacheNodePositions();
        }

        if (animateDots && dotImages.Count > 0) AnimateDots();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!Application.isPlaying) return;
        AutoFindNodes();
    }
#endif

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Bật/tắt chế độ hiển thị đường nối thẳng từ LevelSelectManager.</summary>
    public void SetStraightRoadVisible(bool visible)
    {
        forceShowStraightRoad = visible;
        RebuildPath();
        CacheNodePositions();
    }

    /// <summary>Gọi từ LevelSelectManager sau khi slide animation kết thúc.</summary>
    public void ForceRebuild()
    {
        AutoFindNodes();
        RebuildPath();
        CacheNodePositions();
    }

    // ── Auto-find nodes ───────────────────────────────────────────────────────

    private void AutoFindNodes()
    {
        int validCount = 0;
        foreach (var n in levelNodes)
            if (n != null) validCount++;

        if (validCount >= 2) return;

        levelNodes.Clear();
        foreach (Transform child in transform)
        {
            if (child.name.StartsWith("Road_") || child.name.StartsWith("Dot_")) continue;

            RectTransform rt = child.GetComponent<RectTransform>();
            if (rt != null)
                levelNodes.Add(rt);
        }
    }

    // ── Path rebuild ──────────────────────────────────────────────────────────

    private void RebuildPath()
    {
        ClearSpawnedElements();

        if (levelNodes == null || levelNodes.Count < 2) return;
        if (parentCanvas == null)
        {
            parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas == null) return;
        }

        // Kiểm tra xem các nút đã thu gọn chưa (nếu bật showOnlyWhenRetracted)
        if (showOnlyWhenRetracted && !forceShowStraightRoad && !AreNodesRetracted())
        {
            return; // Đang ở màn hình chính (Idle S-curve của background), không vẽ đường đè lên
        }

        for (int i = 0; i < levelNodes.Count - 1; i++)
        {
            if (levelNodes[i] == null || levelNodes[i + 1] == null) continue;
            Vector2 from = GetLocalCenter(levelNodes[i]);
            Vector2 to   = GetLocalCenter(levelNodes[i + 1]);

            // 1. Vẽ dải đường Candy Crush (Bóng đổ + Viền gạch + Mặt đường + Vệt sáng)
            if (renderRoadSurface)
            {
                SpawnRoadSegment(from, to, roadBorderWidth + 10f, new Color(0f, 0f, 0f, 0.28f), "Road_Shadow");
                SpawnRoadSegment(from, to, roadBorderWidth, roadBorderColor, "Road_Border");
                SpawnRoadSegment(from, to, roadSurfaceWidth, roadSurfaceColor, "Road_Surface");
                SpawnRoadSegment(from, to, roadSurfaceWidth * 0.5f, roadHighlightColor, "Road_Highlight");
            }

            // 2. Vẽ chuỗi chấm tròn nhấp nháy Candy Crush dọc theo đường
            DrawDottedLine(from, to);
        }
    }

    private bool AreNodesRetracted()
    {
        if (levelNodes == null || levelNodes.Count == 0) return false;
        foreach (var node in levelNodes)
        {
            if (node != null && node.anchoredPosition.x <= retractedThresholdX)
                return true;
        }
        return false;
    }

    private void ClearSpawnedElements()
    {
        foreach (var obj in spawnedRoadObjects)
            if (obj != null) Destroy(obj);
        spawnedRoadObjects.Clear();

        foreach (var dot in dotImages)
            if (dot != null) Destroy(dot.gameObject);
        dotImages.Clear();
    }

    private void SpawnRoadSegment(Vector2 from, Vector2 to, float width, Color color, string name)
    {
        Vector2 dir = to - from;
        float dist = dir.magnitude;
        if (dist < 1f) return;

        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(transform, false);
        go.transform.SetSiblingIndex(0); // Luôn nằm dưới cùng, không che nút

        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta        = new Vector2(width, dist);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = (from + to) * 0.5f;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        rt.localRotation = Quaternion.Euler(0f, 0f, angle - 90f);

        var img = go.GetComponent<Image>();
        img.color         = color;
        img.raycastTarget = false;

        spawnedRoadObjects.Add(go);
    }

    private Vector2 GetLocalCenter(RectTransform node)
    {
        Vector3[] corners = new Vector3[4];
        node.GetWorldCorners(corners);
        Vector3 worldCenter = (corners[0] + corners[2]) * 0.5f;

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(
            parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera,
            worldCenter);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            containerRect, screenPoint,
            parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera,
            out Vector2 local);

        return local;
    }

    private void DrawDottedLine(Vector2 from, Vector2 to)
    {
        float dist  = Vector2.Distance(from, to);
        int   count = Mathf.Max(1, Mathf.FloorToInt(dist / dotSpacing));

        for (int i = 0; i <= count; i++)
        {
            float   t   = i / (float)count;
            Vector2 pos = Vector2.Lerp(from, to, t);

            // Viền ngoài chấm
            SpawnDot(pos, dotSize + 5f, dotBorderColor);
            // Chấm chính
            SpawnDot(pos, dotSize, dotColor);
        }
    }

    private void SpawnDot(Vector2 localPos, float size, Color color)
    {
        var go = new GameObject($"Dot_{dotImages.Count}", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(transform, false);
        go.transform.SetSiblingIndex(0);

        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = localPos;
        rt.sizeDelta        = new Vector2(size, size);

        var img = go.GetComponent<Image>();
        img.sprite          = GetCircleSprite();
        img.color           = color;
        img.raycastTarget   = false;

        dotImages.Add(img);
    }

    // ── Animation ─────────────────────────────────────────────────────────────

    private void AnimateDots()
    {
        for (int i = 1; i < dotImages.Count; i += 2)
        {
            if (dotImages[i] == null) continue;
            float phase = (i / (float)dotImages.Count) - animOffset;
            float alpha = 0.40f + 0.60f * Mathf.Abs(Mathf.Sin(phase * Mathf.PI * 2.5f));
            Color c = dotImages[i].color;
            c.a = alpha * dotColor.a;
            dotImages[i].color = c;
        }
    }

    // ── Change detection ──────────────────────────────────────────────────────

    private bool NodePositionsChanged()
    {
        if (cachedPositions.Count != levelNodes.Count) return true;
        for (int i = 0; i < levelNodes.Count; i++)
        {
            if (levelNodes[i] == null) continue;
            if (Vector3.SqrMagnitude(cachedPositions[i] - levelNodes[i].position) > 0.25f)
                return true;
        }
        return false;
    }

    private void CacheNodePositions()
    {
        cachedPositions.Clear();
        foreach (var n in levelNodes)
            cachedPositions.Add(n != null ? n.position : Vector3.zero);
    }

    // ── Circle sprite (shared, cached) ────────────────────────────────────────

    private static Sprite _circleSprite;

    private static Sprite GetCircleSprite()
    {
        if (_circleSprite != null) return _circleSprite;

        const int size = 32;
        var tex    = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;

        var    pixels = new Color[size * size];
        float  cx     = size * 0.5f, cy = size * 0.5f;
        float  r      = size * 0.44f;

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float edge  = r - Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
            float alpha = Mathf.Clamp01(edge * 2f);
            pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
        }

        tex.SetPixels(pixels);
        tex.Apply();
        _circleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return _circleSprite;
    }
}
