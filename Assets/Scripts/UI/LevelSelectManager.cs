using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// LevelSelectManager — xử lý:
/// 1. Hiển thị ảnh Preview Map thực tế khi chọn level (crossfade mượt, Color.white, không phủ màu)
/// 2. Mảng mapPreviewSprites[4] gán ảnh từng màn chơi trực tiếp trên Inspector
/// 3. LevelDetailPanel kính mờ tối (semi-transparent dark), chữ trắng rõ
/// 4. Nút dạt sang trái khi chọn + tự khôi phục
/// 5. Đường nối Candy Crush (LevelPathRenderer) tự rebuild sau khi node di chuyển
/// </summary>
public class LevelSelectManager : MonoBehaviour
{
    [Header("UI Animations")]
    public Animator uiAnimator;

    [Header("Background Transition")]
    public Image bgMain;
    public Image bgTransition;
    public float fadeSpeed = 2f;

    [Header("Preview Map Sprites — theo thứ tự Easy / Normal / Hard / Endless")]
    [Tooltip("Gán ảnh map thực tế của từng màn tại đây. Nếu để trống, hệ thống lấy từ LevelNode.mapPreviewImage.")]
    public Sprite[] mapPreviewSprites = new Sprite[4]; // [0]=Easy [1]=Normal [2]=Hard [3]=Endless

    [Header("Detail Panel — Glassmorphism")]
    [Tooltip("Image nền của LevelDetailPanel — sẽ được set sang màu kính mờ tối")]
    public Image levelDetailPanelBg;
    [Tooltip("Màu nền kính mờ cho panel (semi-transparent dark)")]
    public Color detailPanelBgColor = new Color(0.04f, 0.06f, 0.12f, 0.78f);

    [Header("Detail Panel Texts")]
    public TextMeshProUGUI txtLevelName;
    public TextMeshProUGUI txtDetail;
    public TextMeshProUGUI txtBossTime;
    public TextMeshProUGUI txtClearCount;
    public TextMeshProUGUI txtDeathCount;
    public TextMeshProUGUI txtSurvivalTime;

    [Header("Navigation Buttons")]
    public Button btnBack;
    public Button btnPlay;

    [Header("Level Path (đường nối Candy Crush)")]
    [Tooltip("Script renderer đường nối — gắn vào LevelPathContainer")]
    public LevelPathRenderer pathRenderer;

    [Header("Node Slide Animation")]
    [Tooltip("Khoảng cách dạt sang trái khi node được chọn (pixels)")]
    public float selectedNodeOffsetX = -60f;
    [Tooltip("Thời gian animation dạt (giây)")]
    public float slideAnimDuration = 0.25f;

    [Header("Default Selection (Optional)")]
    public LevelNode defaultSelectedNode;

    // ---- Internal state ----
    private Coroutine fadeCoroutine;
    private string selectedLevelName;
    private DifficultyLevel selectedDifficulty = DifficultyLevel.Normal;
    private LevelNode selectedNode;
    private LevelNode previousNode;

    private readonly Dictionary<LevelNode, Vector2> nodeOriginalPositions = new Dictionary<LevelNode, Vector2>();

    // Ánh xạ DifficultyLevel -> chỉ số trong mapPreviewSprites[]
    private static readonly Dictionary<DifficultyLevel, int> DiffToIndex = new Dictionary<DifficultyLevel, int>
    {
        { DifficultyLevel.Easy,    0 },
        { DifficultyLevel.Normal,  1 },
        { DifficultyLevel.Hard,    2 },
        { DifficultyLevel.Endless, 3 },
    };

    // ---- Lifecycle ----
    void Start()
    {
        if (btnBack != null) btnBack.onClick.AddListener(BackToMainMenu);
        if (btnPlay != null) btnPlay.onClick.AddListener(StartGame);

        // Áp dụng glassmorphism cho detail panel ngay khi start
        ApplyDetailPanelStyle();

        // Làm mới dữ liệu kỷ lục & cache vị trí gốc
        LevelNode[] allNodes = FindObjectsByType<LevelNode>();
        foreach (var node in allNodes)
        {
            node.RefreshRecords();
            RectTransform rt = node.GetComponent<RectTransform>();
            if (rt != null && !nodeOriginalPositions.ContainsKey(node))
                nodeOriginalPositions[node] = rt.anchoredPosition;
        }

        // Chọn mặc định nếu có
        if (defaultSelectedNode != null)
            OnLevelSelected(defaultSelectedNode);
    }

    // Áp dụng style kính mờ cho LevelDetailPanel và đặt chữ về trắng
    private void ApplyDetailPanelStyle()
    {
        if (levelDetailPanelBg != null)
            levelDetailPanelBg.color = detailPanelBgColor;

        // Đặt tất cả text trong detail panel về màu trắng sáng
        SetTextWhite(txtLevelName);
        SetTextWhite(txtDetail);
        SetTextWhite(txtBossTime);
        SetTextWhite(txtClearCount);
        SetTextWhite(txtDeathCount);
        SetTextWhite(txtSurvivalTime);
    }

    private static void SetTextWhite(TextMeshProUGUI txt)
    {
        if (txt == null) return;
        txt.color = Color.white;
    }

    // ---- Khi người chơi chọn một level ----
    public void OnLevelSelected(LevelNode node)
    {
        if (node == null) return;

        node.RefreshRecords();
        selectedDifficulty = node.difficulty;
        selectedLevelName  = node.levelName;
        selectedNode       = node;

        // 1. Cập nhật UI Detail Panel
        if (txtLevelName  != null) txtLevelName.text  = node.levelName;
        if (txtDetail     != null) txtDetail.text     = node.levelDetail;
        if (txtBossTime   != null) txtBossTime.text   = $"Boss Time:\n<b>{node.bossTime}</b>";
        if (txtClearCount != null) txtClearCount.text = $"Clear: <b>{node.clearCount}</b>";
        if (txtDeathCount != null) txtDeathCount.text = $"Death: <b>{node.deathCount}</b>";
        if (txtSurvivalTime != null) txtSurvivalTime.text = $"Best: <b>{node.survivalTime}</b>";

        // 2. Kích hoạt Animation
        if (uiAnimator != null) uiAnimator.SetTrigger("Show");

        // 3. Đổi hình nền preview — ưu tiên theo thứ tự:
        //    a) mapPreviewSprites[index] được gán trên Inspector
        //    b) LevelNode.mapPreviewImage
        //    c) null (giữ nguyên nền cũ)
        Sprite bgSprite = ResolvePreviewSprite(node);

        if (bgSprite != null && bgTransition != null && bgMain != null)
        {
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(CrossfadeBackground(bgSprite));
        }

        // 4. Animate các nút dạt sang trái
        AnimateNodeSlide(previousNode, false);
        AnimateNodeSlide(node, true);
        previousNode = node;

        // 5. Rebuild đường nối sau 1 frame (chờ RectTransform cập nhật)
        if (pathRenderer != null)
            StartCoroutine(RebuildPathNextFrame());
    }

    /// <summary>
    /// Tìm sprite preview phù hợp cho node.
    /// Ưu tiên: mapPreviewSprites[index] > node.mapPreviewImage > null
    /// Ảnh được hiển thị nguyên bản Color.white, không phủ màu tối.
    /// </summary>
    private Sprite ResolvePreviewSprite(LevelNode node)
    {
        // Thử lấy từ mảng mapPreviewSprites theo độ khó
        if (mapPreviewSprites != null && DiffToIndex.TryGetValue(node.difficulty, out int idx))
        {
            if (idx < mapPreviewSprites.Length && mapPreviewSprites[idx] != null)
                return mapPreviewSprites[idx];
        }

        // Fallback: lấy từ LevelNode.mapPreviewImage
        if (node.mapPreviewImage != null)
            return node.mapPreviewImage;

        return null;
    }

    // ---- Crossfade nền (giữ Color.white, không phủ màu) ----
    private IEnumerator CrossfadeBackground(Sprite newBg)
    {
        if (bgTransition == null || bgMain == null) yield break;

        bgTransition.sprite = newBg;

        // Đảm bảo màu ảnh là trắng nguyên bản, không bị tint
        bgMain.color       = Color.white;
        bgTransition.color = new Color(1f, 1f, 1f, 0f);

        float alpha = 0f;
        while (alpha < 1f)
        {
            alpha = Mathf.MoveTowards(alpha, 1f, Time.deltaTime * fadeSpeed);
            bgTransition.color = new Color(1f, 1f, 1f, alpha);
            yield return null;
        }

        // Hoàn tất: gán ảnh vào main, ẩn transition
        bgMain.sprite      = newBg;
        bgMain.color       = Color.white;
        bgTransition.color = new Color(1f, 1f, 1f, 0f);
    }

    private IEnumerator RebuildPathNextFrame()
    {
        yield return null;
        pathRenderer.ForceRebuild();
    }

    // ---- Animate dạt node ----
    private void AnimateNodeSlide(LevelNode node, bool selected)
    {
        if (node == null) return;
        RectTransform rt = node.GetComponent<RectTransform>();
        if (rt == null) return;

        if (!nodeOriginalPositions.TryGetValue(node, out Vector2 origin))
        {
            origin = rt.anchoredPosition;
            nodeOriginalPositions[node] = origin;
        }

        Vector2 target = selected ? origin + new Vector2(selectedNodeOffsetX, 0f) : origin;
        StartCoroutine(SlideNode(rt, target));
    }

    private IEnumerator SlideNode(RectTransform rt, Vector2 targetPos)
    {
        if (rt == null) yield break;
        Vector2 startPos = rt.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < slideAnimDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / slideAnimDuration);
            rt.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            yield return null;
        }

        rt.anchoredPosition = targetPos;
    }

    // ---- Navigation ----
    public void BackToMainMenu() => SceneManager.LoadScene("MainMenu");

    public void StartGame()
    {
        GameDataManager.SetSelectedDifficulty(selectedDifficulty);

        if (selectedNode != null)
        {
            // Lấy sprite đang hiển thị làm nền in-game
            Sprite bgToUse = selectedNode.inGameBackgroundSprite != null
                ? selectedNode.inGameBackgroundSprite
                : ResolvePreviewSprite(selectedNode) ?? selectedNode.mapPreviewImage;

            GameDataManager.SelectedBackgroundSprite   = bgToUse;
            GameDataManager.SelectedBackgroundMaterial = selectedNode.inGameBackgroundMaterial;
        }

        Debug.Log($"[LEVEL SELECT] Bắt đầu màn: {selectedLevelName} | Độ khó: {selectedDifficulty}");
        SceneManager.LoadScene("GameplayScene");
    }
}