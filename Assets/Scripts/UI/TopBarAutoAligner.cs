using UnityEngine;

/// <summary>
/// Tự động căn chỉnh vị trí của thanh TopBar hoặc các nút điều hướng (Quay lại, Vàng, Hoàn trả)
/// sao cho luôn nằm CHÍNH GIỮA khoảng cách giữa CẠNH TRÊN MÀN HÌNH (hoặc mép dưới notch SafeArea)
/// và ĐỈNH CỦA BẢNG NỘI DUNG CHÍNH (SkillGrid, Bg_Main, Khung).
/// Đồng thời hỗ trợ tự tìm viền màn hình hai bên trái/phải.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class TopBarAutoAligner : MonoBehaviour
{
    [Header("Bảng nội dung tham chiếu")]
    [Tooltip("Kéo RectTransform của bảng nội dung chính vào đây (ví dụ SkillGrid, Bg_Main, Khung)")]
    public RectTransform targetBoard;

    [Header("Cấu hình căn chỉnh")]
    [Tooltip("Tự động co giãn chiều ngang ôm trọn 100% chiều rộng màn hình để các nút con tự tìm viền trái/phải")]
    public bool autoStretchWidth = true;

    [Tooltip("Khoảng bù trừ Y nếu muốn dịch nhẹ lên hoặc xuống")]
    public float extraOffsetY = 0f;

    private RectTransform rectTransform;
    private Canvas parentCanvas;
    private RectTransform canvasRectTransform;
    private Vector2 lastCanvasSize = Vector2.zero;
    private Rect lastSafeArea = Rect.zero;

    void Awake()
    {
        EnsureReferences();
    }

    void OnEnable()
    {
        EnsureReferences();
        AlignTopBar();
    }

    void Start()
    {
        EnsureReferences();
        AlignTopBar();
    }

    void Update()
    {
        CheckAndRefresh();
    }

    private void OnRectTransformDimensionsChange()
    {
        CheckAndRefresh();
    }

    private void EnsureReferences()
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        if (parentCanvas == null) parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null && canvasRectTransform == null)
        {
            canvasRectTransform = parentCanvas.transform as RectTransform;
        }

        // Tự động tìm bảng nội dung nếu chưa gán
        if (targetBoard == null && parentCanvas != null)
        {
            var found = parentCanvas.transform.Find("SkillGrid") ??
                        parentCanvas.transform.Find("Bg_Main") ??
                        parentCanvas.transform.Find("Khung") ??
                        parentCanvas.transform.Find("BackgroundLayer/Bg_Main");

            if (found != null) targetBoard = found as RectTransform;
        }
    }

    private void CheckAndRefresh()
    {
        EnsureReferences();
        if (canvasRectTransform == null) return;

        Vector2 currentCanvasSize = canvasRectTransform.rect.size;
        Rect currentSafeArea = Screen.safeArea;

        if (currentCanvasSize != lastCanvasSize || currentSafeArea != lastSafeArea)
        {
            AlignTopBar();
        }
    }

    [ContextMenu("Align TopBar Now")]
    public void AlignTopBar()
    {
        EnsureReferences();
        if (rectTransform == null || canvasRectTransform == null) return;

        // 1. Tự động ôm trọn chiều ngang màn hình để các nút con tự tìm viền
        if (autoStretchWidth)
        {
            rectTransform.anchorMin = new Vector2(0f, 0.5f);
            rectTransform.anchorMax = new Vector2(1f, 0.5f);
            rectTransform.offsetMin = new Vector2(0f, rectTransform.offsetMin.y);
            rectTransform.offsetMax = new Vector2(0f, rectTransform.offsetMax.y);
        }

        float canvasHeight = canvasRectTransform.rect.height;
        float canvasTopY = canvasHeight * 0.5f;

        // 2. Tính mép dưới của tai thỏ / notch camera (nếu có SafeArea)
        if (Screen.height > 0)
        {
            float topNotchPixels = Screen.height - (Screen.safeArea.y + Screen.safeArea.height);
            if (topNotchPixels > 0f)
            {
                float notchInsetCanvas = topNotchPixels * (canvasHeight / Screen.height);
                canvasTopY -= notchInsetCanvas;
            }
        }

        // 3. Tính đỉnh của bảng nội dung chính
        float boardTopY = 650f; // Mặc định an toàn cho bảng 1500 tại pos -100
        if (targetBoard != null)
        {
            float boardPivotY = targetBoard.pivot.y;
            float boardHeight = targetBoard.rect.height;
            boardTopY = targetBoard.anchoredPosition.y + (boardHeight * (1f - boardPivotY));
        }

        // 4. Vị trí Y chính giữa khoảng cách từ đỉnh bảng đến cạnh trên (hoặc mép dưới notch)
        float centerY = (boardTopY + canvasTopY) * 0.5f + extraOffsetY;

        // 5. Áp dụng vào anchoredPosition.y
        Vector2 pos = rectTransform.anchoredPosition;
        pos.y = centerY;
        rectTransform.anchoredPosition = pos;

        lastCanvasSize = canvasRectTransform.rect.size;
        lastSafeArea = Screen.safeArea;
    }
}

