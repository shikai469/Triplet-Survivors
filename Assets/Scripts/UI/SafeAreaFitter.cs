using System.Collections;
using UnityEngine;

/// <summary>
/// Tự động điều chỉnh RectTransform theo vùng an toàn (Screen.safeArea) của thiết bị di động.
/// Giúp bảo vệ giao diện HUD (ExpBar, nút Pause, bộ đếm quái/vàng, thanh máu) khỏi bị tai thỏ (notch),
/// nốt ruồi camera (punch hole) hoặc thanh điều hướng cử chỉ (home indicator) che khuất.
/// Tự động cập nhật ngay lập tức khi xoay màn hình hoặc đổi thiết bị trong Unity Device Simulator.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    [Header("Cạnh áp dụng SafeArea")]
    [Tooltip("Áp dụng thụt lề trên để né tai thỏ/nốt ruồi camera")]
    [SerializeField] private bool applyTop = true;

    [Tooltip("Áp dụng thụt lề dưới để né thanh điều hướng cử chỉ")]
    [SerializeField] private bool applyBottom = true;

    [Tooltip("Áp dụng thụt lề trái khi xoay ngang màn hình")]
    [SerializeField] private bool applyLeft = true;

    [Tooltip("Áp dụng thụt lề phải khi xoay ngang màn hình")]
    [SerializeField] private bool applyRight = true;

    [Header("Khoảng đệm bổ sung (Canvas Units)")]
    [Tooltip("Khoảng cách đệm thêm từ mép SafeArea phía trên (ví dụ 10-20px để UI thoáng hơn)")]
    [SerializeField] private float extraTopPadding = 0f;

    [Tooltip("Khoảng cách đệm thêm từ mép SafeArea phía dưới")]
    [SerializeField] private float extraBottomPadding = 0f;

    private RectTransform rectTransform;
    private Rect lastSafeArea = Rect.zero;
    private Vector2Int lastScreenSize = Vector2Int.zero;
    private ScreenOrientation lastOrientation = ScreenOrientation.AutoRotation;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoAttachToGameplayUI()
    {
        var gameplayUI = GameObject.Find("GameplayUIRoot");
        if (gameplayUI != null && gameplayUI.GetComponent<SafeAreaFitter>() == null)
        {
            var fitter = gameplayUI.AddComponent<SafeAreaFitter>();
            fitter.ApplySafeArea();
        }
    }

    void OnEnable()
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        ApplySafeArea();

        if (Application.isPlaying)
        {
            StartCoroutine(DelayedRefresh());
        }
    }

    void Start()
    {
        ApplySafeArea();

        if (Application.isPlaying)
        {
            StartCoroutine(DelayedRefresh());
        }
    }

    private IEnumerator DelayedRefresh()
    {
        yield return null; // Chờ Canvas Scaler áp dụng độ phân giải thiết bị
        ApplySafeArea();
    }

    void Update()
    {
        RefreshIfNeeded();
    }

    private void OnRectTransformDimensionsChange()
    {
        RefreshIfNeeded();
    }

    private void RefreshIfNeeded()
    {
        Rect safeArea = Screen.safeArea;
        Vector2Int screenSize = new Vector2Int(Screen.width, Screen.height);
        ScreenOrientation orientation = Screen.orientation;

        if (safeArea != lastSafeArea || screenSize != lastScreenSize || orientation != lastOrientation)
        {
            ApplySafeArea();
        }
    }

    /// <summary>
    /// Tính toán và áp dụng tỉ lệ vùng an toàn vào anchor của RectTransform
    /// </summary>
    [ContextMenu("Apply Safe Area Now")]
    public void ApplySafeArea()
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null) return;

        Rect safeArea = Screen.safeArea;
        int screenW = Screen.width;
        int screenH = Screen.height;

        if (screenW <= 0 || screenH <= 0) return;

        Vector2 minAnchor = safeArea.position;
        Vector2 maxAnchor = minAnchor + safeArea.size;

        float anchorMinX = applyLeft ? (minAnchor.x / screenW) : 0f;
        float anchorMinY = applyBottom ? (minAnchor.y / screenH) : 0f;
        float anchorMaxX = applyRight ? (maxAnchor.x / screenW) : 1f;
        float anchorMaxY = applyTop ? (maxAnchor.y / screenH) : 1f;

        rectTransform.anchorMin = new Vector2(anchorMinX, anchorMinY);
        rectTransform.anchorMax = new Vector2(anchorMaxX, anchorMaxY);

        // Áp dụng offset bổ sung nếu có cấu hình đệm thêm
        rectTransform.offsetMin = new Vector2(0f, extraBottomPadding);
        rectTransform.offsetMax = new Vector2(0f, -extraTopPadding);

        lastSafeArea = safeArea;
        lastScreenSize = new Vector2Int(screenW, screenH);
        lastOrientation = Screen.orientation;
    }
}
