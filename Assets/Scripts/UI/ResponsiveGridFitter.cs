using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tự động co giãn và dàn đều 12 ô kỹ năng trong GridLayoutGroup của UpgradeScene.
/// Đảm bảo tất cả các ô luôn nằm gọn 100% bên trong khung nền trắng trên mọi tỉ lệ màn hình
/// (từ 16:9, 18:9, 21:9 đến Tablet 4:3, 3:4), đồng thời giữ nguyên 100% kích thước chuẩn 280x300 ở màn 16:9.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(GridLayoutGroup))]
public class ResponsiveGridFitter : MonoBehaviour
{
    [Header("Base 16:9 Reference Settings")]
    [Tooltip("Kích thước chiều rộng chuẩn của 1 ô trên màn hình 16:9 (1080x1920)")]
    public float baseCellWidth = 280f;

    [Tooltip("Kích thước chiều cao chuẩn của 1 ô trên màn hình 16:9 (1080x1920)")]
    public float baseCellHeight = 300f;

    [Tooltip("Khoảng cách ngang chuẩn giữa các ô")]
    public float baseSpacingX = 40f;

    [Tooltip("Khoảng cách dọc chuẩn giữa các ô")]
    public float baseSpacingY = 40f;

    [Tooltip("Số cột cố định (mặc định 3 cột)")]
    public int columns = 3;

    [Tooltip("Số hàng cố định (mặc định 4 hàng cho 12 kỹ năng)")]
    public int rows = 4;

    [Header("Margin Constraints")]
    [Tooltip("Khoảng cách lề ngang an toàn tối thiểu (tránh đè viền bo góc)")]
    public float minMarginX = 30f;

    [Tooltip("Khoảng cách lề dọc an toàn tối thiểu (tránh đè viền trên dưới)")]
    public float minMarginY = 80f;

    [Header("Auto Setup Options")]
    [Tooltip("Tự động vô hiệu hóa ContentSizeFitter để tránh xung đột chiều cao")]
    public bool disableContentSizeFitter = true;

    [Tooltip("Tự động chuyển RectTransform của Content sang Stretch-Stretch")]
    public bool stretchToParent = true;

    private RectTransform rectTransform;
    private GridLayoutGroup gridLayoutGroup;
    private ContentSizeFitter contentSizeFitter;
    private Vector2 lastSize = Vector2.zero;

    void Awake()
    {
        EnsureComponents();
    }

    void OnEnable()
    {
        EnsureComponents();
        RefreshLayout();

        if (Application.isPlaying)
        {
            StartCoroutine(DelayedRefresh());
        }
    }

    void Start()
    {
        RefreshLayout();

        if (Application.isPlaying)
        {
            StartCoroutine(DelayedRefresh());
        }
    }

    private void EnsureComponents()
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        if (gridLayoutGroup == null) gridLayoutGroup = GetComponent<GridLayoutGroup>();
        if (contentSizeFitter == null) contentSizeFitter = GetComponent<ContentSizeFitter>();
    }

    private void OnRectTransformDimensionsChange()
    {
        if (rectTransform != null)
        {
            Vector2 currentSize = rectTransform.rect.size;
            if (currentSize != lastSize)
            {
                RefreshLayout();
            }
        }
        else
        {
            RefreshLayout();
        }
    }

    private IEnumerator DelayedRefresh()
    {
        yield return null; // Chờ 1 frame để Canvas Scaler áp dụng độ phân giải thiết bị
        RefreshLayout();
    }

    /// <summary>
    /// Tính toán lại cellSize, spacing và padding dựa theo kích thước khung SkillGrid thực tế
    /// </summary>
    [ContextMenu("Refresh Layout Now")]
    public void RefreshLayout()
    {
        EnsureComponents();

        // 1. Tắt ContentSizeFitter nếu đang bật
        if (disableContentSizeFitter && contentSizeFitter != null && contentSizeFitter.enabled)
        {
            contentSizeFitter.enabled = false;
        }

        // 2. Chuyển sang chế độ Stretch-Stretch ôm trọn khung trắng SkillGrid
        if (stretchToParent && rectTransform != null)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        if (rectTransform == null || gridLayoutGroup == null) return;

        float availableWidth = rectTransform.rect.width;
        float availableHeight = rectTransform.rect.height;

        // Fallback: nếu Content vừa khởi tạo chưa có size, lấy size từ SkillGrid cha
        if ((availableWidth <= 10f || availableHeight <= 10f) && transform.parent is RectTransform parentRt)
        {
            availableWidth = parentRt.rect.width;
            availableHeight = parentRt.rect.height;
        }

        if (availableWidth <= 10f || availableHeight <= 10f)
        {
            return;
        }

        // 3. Tính toán không gian chuẩn cần thiết trên màn hình 16:9
        float neededWidth = (columns * baseCellWidth) + ((columns - 1) * baseSpacingX) + minMarginX;
        float neededHeight = (rows * baseCellHeight) + ((rows - 1) * baseSpacingY) + minMarginY;

        // 4. Tính toán hệ số co giãn (Scale Factor)
        float scaleX = availableWidth / neededWidth;
        float scaleY = availableHeight / neededHeight;

        // Giữ nguyên 100% kích thước chuẩn trên màn 16:9 (không phóng to vượt quá 1.0)
        float scale = Mathf.Min(scaleX, scaleY, 1.0f);
        scale = Mathf.Max(0.1f, scale); // Đảm bảo không âm

        // 5. Tính kích thước ô và khoảng cách (giữ đúng tỉ lệ 280:300)
        float finalCellWidth = baseCellWidth * scale;
        float finalCellHeight = baseCellHeight * scale;
        float finalSpacingX = baseSpacingX * scale;
        float finalSpacingY = baseSpacingY * scale;

        // 6. Tính toán Padding để căn giữa toàn bộ 12 ô trong khung nền trắng
        float totalGridWidth = (columns * finalCellWidth) + ((columns - 1) * finalSpacingX);
        float totalGridHeight = (rows * finalCellHeight) + ((rows - 1) * finalSpacingY);

        float remainW = Mathf.Max(0f, availableWidth - totalGridWidth);
        float remainH = Mathf.Max(0f, availableHeight - totalGridHeight);

        int padX = Mathf.RoundToInt(remainW * 0.5f);
        int padY = Mathf.RoundToInt(remainH * 0.5f);

        // 7. Áp dụng vào GridLayoutGroup
        gridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayoutGroup.constraintCount = columns;
        gridLayoutGroup.cellSize = new Vector2(finalCellWidth, finalCellHeight);
        gridLayoutGroup.spacing = new Vector2(finalSpacingX, finalSpacingY);
        gridLayoutGroup.childAlignment = TextAnchor.MiddleCenter;
        gridLayoutGroup.padding = new RectOffset(padX, padX, padY, padY);

        lastSize = new Vector2(availableWidth, availableHeight);
    }
}

