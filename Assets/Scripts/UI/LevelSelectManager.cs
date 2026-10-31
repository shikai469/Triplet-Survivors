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
/// 6. Hiển thị Vàng (GoldDisplay) & Mở khóa tuần tự (0 - 200 - 500 - 700) + Rung nút khi thiếu vàng
/// </summary>
public class LevelSelectManager : MonoBehaviour
{
    [Header("Top Bar — Gold Display")]
    [Tooltip("Kéo GoldDisplay (TextMeshProUGUI) vào đây giống PowerUpShopManager")]
    public TextMeshProUGUI totalGoldText;

    [Header("Level Unlock Costs (Easy / Normal / Hard / Endless)")]
    [Tooltip("Giá mở khóa lần lượt cho: Dễ (0), Bình Thường (200), Khó (500), Vô Tận (700)")]
    public int[] unlockCosts = new int[4] { 0, 200, 500, 700 };

    [Header("UI Animations")]
    public Animator uiAnimator;

    [Header("Background Transition")]
    public Image bgMain;
    public Image bgTransition;
    public float fadeSpeed = 2f;
    [Tooltip("Ảnh nền bản đồ mặc định khi chưa chọn level (CandyCrush Map)")]
    public Sprite defaultMapSprite;

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
    [Tooltip("Text bên trong nút CHƠI (để trống sẽ tự động lấy từ btnPlay)")]
    public TextMeshProUGUI btnPlayText;

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
    private DifficultyLevel selectedDifficulty = DifficultyLevel.Easy;
    private LevelNode selectedNode;
    private LevelNode previousNode;
    private LevelNode[] allNodes;

    private readonly Dictionary<LevelNode, Vector2> nodeOriginalPositions = new Dictionary<LevelNode, Vector2>();

    // Quản lý rung nút khi bấm thiếu vàng (giống PowerUpShopManager)
    private Coroutine shakeCoroutine;
    private Vector3 originalBtnPos;
    private bool posRecorded = false;
    private Color originalBtnTextColor = Color.black;
    private bool textColorRecorded = false;

    // Ánh xạ DifficultyLevel -> chỉ số trong mapPreviewSprites[] & unlockCosts[]
    private static readonly Dictionary<DifficultyLevel, int> DiffToIndex = new Dictionary<DifficultyLevel, int>
    {
        { DifficultyLevel.Easy,    0 },
        { DifficultyLevel.Normal,  1 },
        { DifficultyLevel.Hard,    2 },
        { DifficultyLevel.Endless, 3 },
    };

    private static readonly DifficultyLevel[] IndexToDiff = new DifficultyLevel[]
    {
        DifficultyLevel.Easy,
        DifficultyLevel.Normal,
        DifficultyLevel.Hard,
        DifficultyLevel.Endless
    };

    // ---- Lifecycle ----
    void Awake()
    {
        // Đảm bảo mảng giá mở khóa luôn đúng 4 mốc 0 - 200 - 500 - 700
        if (unlockCosts == null || unlockCosts.Length < 4)
        {
            unlockCosts = new int[4] { 0, 200, 500, 700 };
        }

        // Tự động tìm GoldDisplay nếu chưa kéo vào Inspector
        if (totalGoldText == null)
        {
            GameObject goldObj = GameObject.Find("GoldDisplay");
            if (goldObj != null)
                totalGoldText = goldObj.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        // Tự động lấy Text của nút CHƠI
        if (btnPlay != null && btnPlayText == null)
        {
            btnPlayText = btnPlay.GetComponentInChildren<TextMeshProUGUI>(true);
        }
    }

    void Start()
    {
        // Luôn mở khóa sẵn màn Dễ (Easy)
        SetDifficultyUnlocked(DifficultyLevel.Easy, true);

        if (btnPlay != null && !posRecorded)
        {
            originalBtnPos = btnPlay.transform.localPosition;
            posRecorded = true;
        }

        if (btnPlayText != null && !textColorRecorded)
        {
            originalBtnTextColor = btnPlayText.color;
            textColorRecorded = true;
        }

        if (btnBack != null) btnBack.onClick.AddListener(BackToMainMenu);
        if (btnPlay != null) btnPlay.onClick.AddListener(OnPlayOrUnlockClicked);

        // Áp dụng glassmorphism cho detail panel ngay khi start
        ApplyDetailPanelStyle();

        // Làm mới dữ liệu kỷ lục & cache vị trí gốc
        allNodes = FindObjectsByType<LevelNode>();
        foreach (var node in allNodes)
        {
            node.RefreshRecords();
            RectTransform rt = node.GetComponent<RectTransform>();
            if (rt != null && !nodeOriginalPositions.ContainsKey(node))
                nodeOriginalPositions[node] = rt.anchoredPosition;
        }

        // Gán ảnh nền mặc định nếu có và chưa có sprite
        if (bgMain != null && defaultMapSprite != null && (bgMain.sprite == null || defaultSelectedNode == null))
        {
            bgMain.sprite = defaultMapSprite;
            bgMain.color = Color.white;
        }

        // Cập nhật số vàng & trạng thái sáng/tối của các nút level
        UpdateGoldUI();
        RefreshNodesLockVisual();

        // Chọn mặc định nếu có
        if (defaultSelectedNode != null)
            OnLevelSelected(defaultSelectedNode);
    }

    // Áp dụng style kính mờ cho LevelDetailPanel và đặt chữ về trắng
    private void ApplyDetailPanelStyle()
    {
        if (levelDetailPanelBg != null)
            levelDetailPanelBg.color = detailPanelBgColor;

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
        selectedLevelName = node.levelName;
        selectedNode = node;

        // 1. Cập nhật UI Detail Panel
        if (txtLevelName != null) txtLevelName.text = node.levelName;
        if (txtDetail != null) txtDetail.text = node.levelDetail;
        if (txtBossTime != null) txtBossTime.text = $"Boss Time:\n<b>{node.bossTime}</b>";
        if (txtClearCount != null) txtClearCount.text = $"Clear: <b>{node.clearCount}</b>";
        if (txtDeathCount != null) txtDeathCount.text = $"Death: <b>{node.deathCount}</b>";
        if (txtSurvivalTime != null) txtSurvivalTime.text = $"Best: <b>{node.survivalTime}</b>";

        // 2. Cập nhật trạng thái nút CHƠI / Mở Khóa / Thiếu Vàng
        RefreshPlayButtonState();

        // 3. Kích hoạt Animation
        if (uiAnimator != null) uiAnimator.SetTrigger("Show");

        // 4. Đổi hình nền preview
        Sprite bgSprite = ResolvePreviewSprite(node);

        if (bgSprite != null && bgTransition != null && bgMain != null)
        {
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
                if (bgTransition.sprite != null && bgTransition.color.a > 0.4f)
                    bgMain.sprite = bgTransition.sprite;
            }
            fadeCoroutine = StartCoroutine(CrossfadeBackground(bgSprite));
        }

        // 5. Animate các nút dạt sang trái
        AnimateNodeSlide(previousNode, false);
        AnimateNodeSlide(node, true);
        previousNode = node;

        // 6. Hiển thị và rebuild đường nối thẳng Candy Crush
        if (pathRenderer != null)
        {
            pathRenderer.SetStraightRoadVisible(true);
            StartCoroutine(RebuildPathNextFrame());
        }
    }

    // =====================================================================
    // HỆ THỐNG VÀNG & MỞ KHÓA LEVEL (GIỐNG POWERUPSHOPMANAGER)
    // =====================================================================

    public void UpdateGoldUI()
    {
        if (totalGoldText != null)
        {
            totalGoldText.text = GameDataManager.GetTotalGold().ToString();
        }
    }

    private string GetUnlockKey(DifficultyLevel diff) => $"LevelUnlock_{diff}";

    public bool IsDifficultyUnlocked(DifficultyLevel diff)
    {
        if (diff == DifficultyLevel.Easy) return true; // Màn Dễ luôn mở
        return GameDataManager.GetUpgradeLevel(GetUnlockKey(diff)) >= 1;
    }

    public void SetDifficultyUnlocked(DifficultyLevel diff, bool unlocked)
    {
        GameDataManager.SetUpgradeLevel(GetUnlockKey(diff), unlocked ? 1 : 0);
    }

    private bool IsPreviousDifficultyUnlocked(DifficultyLevel diff)
    {
        if (!DiffToIndex.TryGetValue(diff, out int idx) || idx <= 0) return true;
        DifficultyLevel prevDiff = IndexToDiff[idx - 1];
        return IsDifficultyUnlocked(prevDiff);
    }

    private int GetUnlockCost(DifficultyLevel diff)
    {
        if (DiffToIndex.TryGetValue(diff, out int idx) && unlockCosts != null && idx < unlockCosts.Length)
        {
            return unlockCosts[idx];
        }
        return 0;
    }

    /// <summary>
    /// Cập nhật chữ và màu sắc trên nút btnPlay giống PowerUpShopManager
    /// </summary>
    private void RefreshPlayButtonState()
    {
        StopShake();

        if (btnPlay != null)
        {
            if (!posRecorded)
            {
                originalBtnPos = btnPlay.transform.localPosition;
                posRecorded = true;
            }
            btnPlay.interactable = true; // Luôn để mở để bắt sự kiện bấm rung nút
        }

        if (btnPlayText == null) return;

        if (IsDifficultyUnlocked(selectedDifficulty))
        {
            btnPlayText.text = "CHƠI";
            btnPlayText.color = textColorRecorded ? originalBtnTextColor : Color.black;
        }
        else
        {
            int cost = GetUnlockCost(selectedDifficulty);
            int totalGold = GameDataManager.GetTotalGold();
            bool prevUnlocked = IsPreviousDifficultyUnlocked(selectedDifficulty);

            if (!prevUnlocked)
            {
                // Chưa mở màn trước đó
                btnPlayText.text = $"Khóa ({cost})";
                btnPlayText.color = new Color(1f, 0.35f, 0.35f, 1f);
            }
            else if (totalGold >= cost)
            {
                // Đủ vàng để mở khóa
                btnPlayText.text = $"Mở Khóa ({cost})";
                btnPlayText.color = new Color(0.1f, 0.55f, 0.15f, 1f);
            }
            else
            {
                // Thiếu vàng -> Chữ đỏ cam tĩnh giống PowerUpShopManager
                btnPlayText.text = $"Thiếu Vàng ({cost})";
                btnPlayText.color = new Color(1f, 0.35f, 0.35f, 1f);
            }
        }
    }

    /// <summary>
    /// Xử lý khi ấn vào nút dưới cùng của LevelDetailPanel (Mở khóa hoặc Vào chơi)
    /// </summary>
    private void OnPlayOrUnlockClicked()
    {
        // 1. Nếu màn chơi đã mở khóa -> Vào game ngay
        if (IsDifficultyUnlocked(selectedDifficulty))
        {
            StartGame();
            return;
        }

        // 2. Nếu màn chơi đang khóa -> Kiểm tra điều kiện mở tuần tự & số vàng
        bool prevUnlocked = IsPreviousDifficultyUnlocked(selectedDifficulty);
        int cost = GetUnlockCost(selectedDifficulty);
        int totalGold = GameDataManager.GetTotalGold();

        if (prevUnlocked && totalGold >= cost)
        {
            // Mua mở khóa thành công
            GameDataManager.AddGold(-cost);
            SetDifficultyUnlocked(selectedDifficulty, true);

            UpdateGoldUI();
            RefreshNodesLockVisual();
            RefreshPlayButtonState();
            Debug.Log($"[LEVEL UNLOCK] Đã mở khóa màn {selectedDifficulty} với {cost} Vàng!");
        }
        else
        {
            // Thiếu vàng hoặc chưa mở màn trước -> Rung ngang nút giống PowerUpShopManager
            if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
            shakeCoroutine = StartCoroutine(ShakeButtonRoutine());
        }
    }

    private IEnumerator ShakeButtonRoutine()
    {
        if (btnPlay == null) yield break;

        float duration = 0.3f;
        float elapsed = 0f;
        float magnitude = 9f; // Biên độ rung ngang giống PowerUpShopManager

        while (elapsed < duration)
        {
            float offsetX = Random.Range(-1f, 1f) * magnitude;
            btnPlay.transform.localPosition = originalBtnPos + new Vector3(offsetX, 0f, 0f);

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        StopShake();
    }

    private void StopShake()
    {
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            shakeCoroutine = null;
        }
        if (btnPlay != null && posRecorded)
        {
            btnPlay.transform.localPosition = originalBtnPos;
        }
    }

    /// <summary>
    /// Làm tối nhẹ các nút level chưa mở khóa trên bản đồ để dễ phân biệt
    /// </summary>
    private void RefreshNodesLockVisual()
    {
        if (allNodes == null || allNodes.Length == 0)
            allNodes = FindObjectsByType<LevelNode>();

        foreach (var node in allNodes)
        {
            if (node == null) continue;
            Image img = node.GetComponent<Image>();
            if (img != null)
            {
                bool unlocked = IsDifficultyUnlocked(node.difficulty);
                img.color = unlocked ? Color.white : new Color(0.55f, 0.55f, 0.55f, 0.9f);
            }
        }
    }

    // =====================================================================
    // XỬ LÝ PREVIEW MAP, CROSSFADE & SLIDE NODE
    // =====================================================================

    private Sprite ResolvePreviewSprite(LevelNode node)
    {
        if (mapPreviewSprites != null && DiffToIndex.TryGetValue(node.difficulty, out int idx))
        {
            if (idx < mapPreviewSprites.Length && mapPreviewSprites[idx] != null)
                return mapPreviewSprites[idx];
        }

        if (node.mapPreviewImage != null)
            return node.mapPreviewImage;

        return GetFallbackPreview(node.difficulty);
    }

    private static readonly Dictionary<DifficultyLevel, Sprite> CachedFallbackPreviews = new Dictionary<DifficultyLevel, Sprite>();

    private Sprite GetFallbackPreview(DifficultyLevel diff)
    {
        if (CachedFallbackPreviews.TryGetValue(diff, out Sprite cached) && cached != null)
            return cached;

        string fileName = diff switch
        {
            DifficultyLevel.Easy => "MapPreview_Easy.png",
            DifficultyLevel.Normal => "MapPreview_Normal.png",
            DifficultyLevel.Hard => "MapPreview_Hard.png",
            DifficultyLevel.Endless => "MapPreview_Endless.png",
            _ => "MapPreview_Easy.png"
        };

        string fullPath = System.IO.Path.Combine(Application.dataPath, "Sprites", "UI", "MapPreviews", fileName);
        if (System.IO.File.Exists(fullPath))
        {
            try
            {
                byte[] bytes = System.IO.File.ReadAllBytes(fullPath);
                Texture2D tex = new Texture2D(900, 1500, TextureFormat.RGBA32, false);
                if (tex.LoadImage(bytes))
                {
                    Sprite spr = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                    CachedFallbackPreviews[diff] = spr;
                    return spr;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[LevelSelectManager] Không thể nạp preview {fileName}: {ex.Message}");
            }
        }

        return defaultMapSprite;
    }

    private IEnumerator CrossfadeBackground(Sprite newBg)
    {
        if (bgTransition == null || bgMain == null || newBg == null) yield break;

        if (bgMain.sprite == newBg && bgTransition.color.a <= 0.01f) yield break;

        bgTransition.sprite = newBg;
        bgTransition.gameObject.SetActive(true);
        bgTransition.raycastTarget = false;

        bgMain.color = Color.white;
        bgTransition.color = new Color(1f, 1f, 1f, 0f);

        float alpha = 0f;
        while (alpha < 1f)
        {
            alpha = Mathf.MoveTowards(alpha, 1f, Time.deltaTime * fadeSpeed);
            bgTransition.color = new Color(1f, 1f, 1f, alpha);
            yield return null;
        }

        bgMain.sprite = newBg;
        bgMain.color = Color.white;
        bgTransition.color = new Color(1f, 1f, 1f, 0f);
    }

    private IEnumerator RebuildPathNextFrame()
    {
        yield return null;
        pathRenderer.ForceRebuild();
    }

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
        // Chặn vào game nếu gọi trực tiếp mà màn chơi chưa mở khóa
        if (!IsDifficultyUnlocked(selectedDifficulty))
        {
            OnPlayOrUnlockClicked();
            return;
        }

        GameDataManager.SetSelectedDifficulty(selectedDifficulty);

        if (selectedNode != null)
        {
            GameDataManager.SelectedBackgroundSprite = selectedNode.inGameBackgroundSprite;
            GameDataManager.SelectedBackgroundMaterial = selectedNode.inGameBackgroundMaterial;
        }

        Debug.Log($"[LEVEL SELECT] Bắt đầu màn: {selectedLevelName} | Độ khó: {selectedDifficulty}");
        SceneManager.LoadScene("GameplayScene");
    }

    // =====================================================================
    // MENU TEST NHANH TRONG INSPECTOR (Chuột phải vào LevelSelectManager)
    // =====================================================================
    [ContextMenu("Test: Cộng 500 Vàng")]
    public void DebugAdd500Gold()
    {
        GameDataManager.AddGold(500);
        UpdateGoldUI();
        RefreshPlayButtonState();
    }

    [ContextMenu("Test: Khóa lại Normal / Hard / Endless")]
    public void DebugResetLocks()
    {
        SetDifficultyUnlocked(DifficultyLevel.Normal, false);
        SetDifficultyUnlocked(DifficultyLevel.Hard, false);
        SetDifficultyUnlocked(DifficultyLevel.Endless, false);
        RefreshNodesLockVisual();
        RefreshPlayButtonState();
    }
}