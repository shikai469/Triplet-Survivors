using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Vẽ đường nối dạng chấm/nét đứt kiểu Candy Crush giữa các nút Level trong LevelPathContainer.
/// • Không yêu cầu Canvas riêng — tự tìm Canvas cha gần nhất.
/// • Tự động tìm 4 nút con (Easy/Normal/Hard/Endless) trong transform nếu levelNodes chưa gán.
/// • Tự động rebuild khi vị trí node thay đổi (ví dụ: slide animation).
/// Gắn script này vào GameObject LevelPathContainer.
/// </summary>
public class LevelPathRenderer : MonoBehaviour
{
    [Header("Các nút Level theo thứ tự: Easy → Normal → Hard → Endless")]
    [Tooltip("Nếu để trống, script tự tìm tất cả Button con theo thứ tự sibling trong LevelPathContainer.")]
    public List<RectTransform> levelNodes = new List<RectTransform>();

    [Header("Cài đặt đường nối")]
    [Tooltip("Kích thước mỗi chấm (pixels)")]
    public float dotSize = 12f;

    [Tooltip("Khoảng cách giữa tâm 2 chấm liên tiếp (pixels)")]
    public float dotSpacing = 24f;

    [Tooltip("Màu chấm chính")]
    public Color dotColor = new Color(1f, 0.90f, 0.40f, 0.90f);

    [Tooltip("Màu viền ngoài chấm")]
    public Color dotBorderColor = new Color(0.55f, 0.40f, 0.05f, 0.65f);

    [Header("Animation nhấp nháy")]
    public bool animateDots = true;
    public float animSpeed  = 1.4f;

    // ── Internal ──────────────────────────────────────────────────────────────
    private readonly List<Image>   dotImages      = new List<Image>();
    private readonly List<Vector3> cachedPositions = new List<Vector3>();

    private Canvas        parentCanvas;
    private RectTransform containerRect;
    private float         animOffset = 0f;

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    void Awake()
    {
        containerRect = GetComponent<RectTransform>();
        // Tìm Canvas cha gần nhất (không cần Canvas trực tiếp trên object này)
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

        if (animateDots) AnimateDots();
    }

#if UNITY_EDITOR
    // Trong Editor: tự nạp node ngay khi giá trị thay đổi để xem trước
    void OnValidate()
    {
        if (!Application.isPlaying) return;
        AutoFindNodes();
    }
#endif

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Gọi từ LevelSelectManager sau khi slide animation kết thúc.</summary>
    public void ForceRebuild()
    {
        AutoFindNodes();
        RebuildPath();
        CacheNodePositions();
    }

    // ── Auto-find nodes ───────────────────────────────────────────────────────

    /// <summary>
    /// Nếu levelNodes chưa đủ 4 phần tử hợp lệ, tự tìm tất cả Button con
    /// (hoặc toàn bộ con có RectTransform) sắp xếp theo thứ tự sibling.
    /// </summary>
    private void AutoFindNodes()
    {
        // Đếm node hợp lệ hiện tại
        int validCount = 0;
        foreach (var n in levelNodes)
            if (n != null) validCount++;

        if (validCount >= 2) return; // Đã đủ, không tự fill

        levelNodes.Clear();

        // Duyệt các con trực tiếp theo thứ tự sibling
        foreach (Transform child in transform)
        {
            // Bỏ qua các chấm dot đã sinh (tên bắt đầu bằng "Dot_")
            if (child.name.StartsWith("Dot_")) continue;

            RectTransform rt = child.GetComponent<RectTransform>();
            if (rt != null)
                levelNodes.Add(rt);
        }

        if (levelNodes.Count < 2)
            Debug.LogWarning("[LevelPathRenderer] Tìm được ít hơn 2 node con. Hãy gán thủ công vào levelNodes.", this);
        else
            Debug.Log($"[LevelPathRenderer] Tự động tìm được {levelNodes.Count} node(s): " +
                      string.Join(", ", levelNodes.ConvertAll(n => n.name)));
    }

    // ── Path rebuild ──────────────────────────────────────────────────────────

    private void RebuildPath()
    {
        // Xoá chấm cũ
        foreach (var dot in dotImages)
            if (dot != null) Destroy(dot.gameObject);
        dotImages.Clear();

        if (levelNodes == null || levelNodes.Count < 2) return;
        if (parentCanvas == null)
        {
            parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas == null) return;
        }

        for (int i = 0; i < levelNodes.Count - 1; i++)
        {
            if (levelNodes[i] == null || levelNodes[i + 1] == null) continue;
            DrawDottedLine(GetLocalCenter(levelNodes[i]), GetLocalCenter(levelNodes[i + 1]));
        }
    }

    // Chuyển vị trí world-center của node về local space của container
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

            // Viền ngoài
            SpawnDot(pos, dotSize + 5f, dotBorderColor);
            // Chấm chính
            SpawnDot(pos, dotSize, dotColor);
        }
    }

    private void SpawnDot(Vector2 localPos, float size, Color color)
    {
        var go = new GameObject($"Dot_{dotImages.Count}", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(transform, false);
        go.transform.SetSiblingIndex(0); // luôn nằm dưới cùng, không che nút

        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = localPos;
        rt.sizeDelta        = new Vector2(size, size);

        var img = go.GetComponent<Image>();
        img.sprite          = GetCircleSprite();
        img.color           = color;
        img.raycastTarget   = false; // không chặn click

        dotImages.Add(img);
    }

    // ── Animation ─────────────────────────────────────────────────────────────

    private void AnimateDots()
    {
        // Mỗi vị trí có 2 ảnh: [border, main] → chỉ animate chấm chính (index lẻ)
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
