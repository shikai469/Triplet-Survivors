using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Universal UI Rounded Corners & Border:
/// - Hỗ trợ tick chọn bo từ 1 đến 4 góc bất kỳ (Top-Left, Top-Right, Bottom-Left, Bottom-Right)
/// - Hỗ trợ chỉnh bán kính riêng biệt cho từng góc (nếu muốn mỗi góc cong khác nhau)
/// - Tự động cắt gọn các ảnh/object con bên trong bằng Mask (tùy chọn)
/// - Tự động vẽ đường viền (Border) khớp chính xác theo từng góc bo hoặc góc vuông
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform), typeof(Image))]
public class UIRoundedBorder : MonoBehaviour
{
    [Header("1. Chọn góc muốn bo (1 - 4 góc)")]
    public bool roundTopLeft = true;
    public bool roundTopRight = true;
    public bool roundBottomLeft = true;
    public bool roundBottomRight = true;

    [Header("2. Độ cong góc (Corner Radius)")]
    [Range(0, 150)]
    [Tooltip("Độ cong áp dụng cho các góc được tick ở trên")]
    public int cornerRadius = 40;

    [Tooltip("Bật nếu muốn chỉnh độ cong khác nhau cho từng góc")]
    public bool customPerCornerRadius = false;
    [Range(0, 150)] public int topLeftRadius = 40;
    [Range(0, 150)] public int topRightRadius = 40;
    [Range(0, 150)] public int bottomLeftRadius = 40;
    [Range(0, 150)] public int bottomRightRadius = 40;

    [Header("3. Cắt gọn Object con (Mask)")]
    [Tooltip("Bật khi dùng cho khung chứa ảnh con (như BackgroundLayer, Panel). Tắt nếu chỉ dùng cho Button/Slot đơn lẻ.")]
    public bool clipChildrenWithMask = false;

    [Header("4. Đường viền (Border)")]
    [Range(0, 30)]
    public int borderThickness = 0;
    public Color borderColor = new Color(0.85f, 0.70f, 0.35f, 1f);

    private Image targetImage;
    private Image borderOverlayImage;
    private Texture2D generatedMaskTex;
    private Texture2D generatedBorderTex;
    private Sprite generatedMaskSprite;
    private Sprite generatedBorderSprite;

    private const string BorderOverlayName = "_RoundedBorderOverlay";

    void OnEnable()
    {
        RebuildRoundedUI();
    }

    void OnValidate()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this != null && gameObject != null)
                RebuildRoundedUI();
        };
#endif
    }

    /// <summary>
    /// Lấy bán kính thực tế của 4 góc: (TopLeft, TopRight, BottomRight, BottomLeft)
    /// </summary>
    public Vector4 GetEffectiveRadii()
    {
        float tl = roundTopLeft ? (customPerCornerRadius ? topLeftRadius : cornerRadius) : 0f;
        float tr = roundTopRight ? (customPerCornerRadius ? topRightRadius : cornerRadius) : 0f;
        float br = roundBottomRight ? (customPerCornerRadius ? bottomRightRadius : cornerRadius) : 0f;
        float bl = roundBottomLeft ? (customPerCornerRadius ? bottomLeftRadius : cornerRadius) : 0f;
        return new Vector4(tl, tr, br, bl);
    }

    public void RebuildRoundedUI()
    {
        targetImage = GetComponent<Image>();
        if (targetImage == null) return;

        Vector4 radii = GetEffectiveRadii();
        float maxRadius = Mathf.Max(Mathf.Max(radii.x, radii.y), Mathf.Max(radii.z, radii.w));

        // Kích thước vùng cắt 9-slice đảm bảo bao trọn góc cong lớn nhất và độ dày viền
        int sliceMargin = Mathf.CeilToInt(Mathf.Max(maxRadius, borderThickness) + 4f);
        int texSize = Mathf.Max(sliceMargin * 2 + 16, 64);
        Vector4 borderSlice = new Vector4(sliceMargin, sliceMargin, sliceMargin, sliceMargin);

        // 1. Tạo Sprite 9-Slice cho khung chính
        CleanupAsset(ref generatedMaskSprite);
        CleanupAsset(ref generatedMaskTex);

        generatedMaskTex = Generate4CornerTexture(texSize, radii, 0f);
        generatedMaskSprite = Sprite.Create(
            generatedMaskTex,
            new Rect(0, 0, texSize, texSize),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            borderSlice
        );
        generatedMaskSprite.hideFlags = HideFlags.DontSave;

        targetImage.sprite = generatedMaskSprite;
        targetImage.type = Image.Type.Sliced;
        targetImage.pixelsPerUnitMultiplier = 1f;

        // 2. Quản lý Mask cắt ảnh con
        Mask mask = GetComponent<Mask>();
        if (clipChildrenWithMask)
        {
            if (mask == null) mask = gameObject.AddComponent<Mask>();
            mask.enabled = true;
            mask.showMaskGraphic = true;
            if (targetImage.color.a < 0.01f)
                targetImage.color = Color.white;
        }
        else if (mask != null)
        {
            mask.enabled = false;
        }

        // 3. Tạo hoặc cập nhật lớp viền (Border Overlay)
        UpdateBorderOverlay(texSize, radii, borderSlice);
    }

    private void UpdateBorderOverlay(int texSize, Vector4 radii, Vector4 borderSlice)
    {
        Transform existing = transform.Find(BorderOverlayName);

        if (borderThickness <= 0)
        {
            if (existing != null) existing.gameObject.SetActive(false);
            return;
        }

        GameObject overlayObj;
        if (existing != null)
        {
            overlayObj = existing.gameObject;
            overlayObj.SetActive(true);
        }
        else
        {
            overlayObj = new GameObject(BorderOverlayName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            overlayObj.transform.SetParent(transform, false);
        }

        // Luôn nằm dưới cùng của Hierarchy con để hiển thị đè lên trên các ảnh con
        overlayObj.transform.SetAsLastSibling();

        RectTransform rt = overlayObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        borderOverlayImage = overlayObj.GetComponent<Image>();
        borderOverlayImage.raycastTarget = false;

        CleanupAsset(ref generatedBorderSprite);
        CleanupAsset(ref generatedBorderTex);

        generatedBorderTex = Generate4CornerTexture(texSize, radii, borderThickness);
        generatedBorderSprite = Sprite.Create(
            generatedBorderTex,
            new Rect(0, 0, texSize, texSize),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            borderSlice
        );
        generatedBorderSprite.hideFlags = HideFlags.DontSave;

        borderOverlayImage.sprite = generatedBorderSprite;
        borderOverlayImage.type = Image.Type.Sliced;
        borderOverlayImage.pixelsPerUnitMultiplier = 1f;
        borderOverlayImage.color = borderColor;
    }

    /// <summary>
    /// Vẽ SDF hình chữ nhật với 4 bán kính góc độc lập:
    /// radii = (x: TopLeft, y: TopRight, z: BottomRight, w: BottomLeft)
    /// </summary>
    private Texture2D Generate4CornerTexture(int size, Vector4 radii, float thickness)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.hideFlags = HideFlags.DontSave;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[size * size];
        float halfSize = size * 0.5f;
        Vector2 center = new Vector2(halfSize, halfSize);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - center;

                // Chọn bán kính tương ứng với góc phần tư hiện tại
                float r;
                if (p.y >= 0f)
                    r = (p.x < 0f) ? radii.x : radii.y; // Top-Left (x) hoặc Top-Right (y)
                else
                    r = (p.x >= 0f) ? radii.z : radii.w; // Bottom-Right (z) hoặc Bottom-Left (w)

                Vector2 boxHalf = new Vector2(halfSize - r - 1f, halfSize - r - 1f);
                Vector2 q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - boxHalf;
                float dist = Vector2.Max(q, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - r;

                // Khử răng cưa mép ngoài
                float outerAlpha = Mathf.Clamp01(0.5f - dist);
                float finalAlpha = outerAlpha;

                if (thickness > 0f)
                {
                    // Khử răng cưa mép trong của đường viền
                    float innerDist = dist + thickness;
                    float innerAlpha = Mathf.Clamp01(0.5f + innerDist);
                    finalAlpha = outerAlpha * innerAlpha;
                }

                pixels[y * size + x] = new Color(1f, 1f, 1f, finalAlpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private void CleanupAsset<T>(ref T asset) where T : Object
    {
        if (asset != null)
        {
            DestroyImmediate(asset);
            asset = null;
        }
    }

    void OnDestroy()
    {
        CleanupAsset(ref generatedMaskSprite);
        CleanupAsset(ref generatedMaskTex);
        CleanupAsset(ref generatedBorderSprite);
        CleanupAsset(ref generatedBorderTex);
    }
}