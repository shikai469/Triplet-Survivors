using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Thanh Máu Boss (Boss Health Bar UI) hiển thị nổi bật trên đỉnh màn hình khi chiến đấu với Boss.
/// - Tự động liên kết và theo dõi máu của Boss theo thời gian thực (Zero Lag, Smooth Fill).
/// - Hiệu ứng Damage Ghost (vệt máu vàng tuột chậm theo sau khi Boss nhận sát thương).
/// - Hiển thị Tên Boss, Huy hiệu Phase (Giai đoạn 1, 2, Cuồng Nộ), Số máu & Tỉ lệ %.
/// - Tự động khởi tạo giao diện UI Procedural đẹp mắt, tương thích hoàn toàn với SafeArea và Canvas.
/// </summary>
public class BossHealthBarUI : MonoBehaviour
{
    public static BossHealthBarUI Instance { get; private set; }

    [Header("UI Hierarchy References")]
    public CanvasGroup canvasGroup;
    public RectTransform containerRect;
    public Image bgImage;
    public Image bossIconImage;
    public TextMeshProUGUI bossNameText;
    public TextMeshProUGUI phaseBadgeText;
    public Slider healthSlider;
    public Image healthFillImage;
    public Slider ghostSlider;
    public Image ghostFillImage;
    public TextMeshProUGUI healthNumbersText;

    private EnemyHealth currentBossHealth;
    private float maxHp = 1f;
    private float displayedHp = 1f;
    private float ghostHp = 1f;
    private float ghostWaitTimer = 0f;
    private int currentPhase = 1;
    private bool isDefeated = false;
    private Coroutine animCoroutine;

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

        // Tự động xây dựng UI nếu chưa có sẵn
        if (containerRect == null)
        {
            BuildProceduralUI();
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    /// <summary>
    /// Hiển thị thanh máu Boss và bắt đầu theo dõi máu của thực thể Boss
    /// </summary>
    public static void Show(EnemyHealth bossHealth, string bossName = "MÈO CHUỐI")
    {
        if (bossHealth == null) return;

        EnsureInstance();
        if (Instance != null)
        {
            Instance.SetBoss(bossHealth, bossName);
        }
    }

    /// <summary>
    /// Tắt thanh máu Boss khi Boss bị tiêu diệt
    /// </summary>
    public static void Hide()
    {
        if (Instance != null)
        {
            Instance.StartDefeatSequence();
        }
    }

    private static void EnsureInstance()
    {
        if (Instance != null) return;

        BossHealthBarUI found = Object.FindAnyObjectByType<BossHealthBarUI>();
        if (found != null)
        {
            Instance = found;
            return;
        }

        // Tìm Canvas phù hợp để đính UI vào
        Canvas canvas = null;
        GameObject uiRoot = GameObject.Find("gameplayUIRoot");
        if (uiRoot != null)
        {
            canvas = uiRoot.GetComponentInParent<Canvas>();
        }
        if (canvas == null)
        {
            canvas = Object.FindAnyObjectByType<Canvas>();
        }

        if (canvas != null)
        {
            Transform parentTransform = uiRoot != null ? uiRoot.transform : canvas.transform;
            GameObject bossBarObj = new GameObject("[BossHealthBarUI]");
            bossBarObj.transform.SetParent(parentTransform, false);
            Instance = bossBarObj.AddComponent<BossHealthBarUI>();
        }
    }

    public void SetBoss(EnemyHealth bossHealth, string bossName)
    {
        currentBossHealth = bossHealth;
        isDefeated = false;

        maxHp = bossHealth.maxHealth > 0 ? bossHealth.maxHealth : (bossHealth.baseMaxHealth > 0 ? bossHealth.baseMaxHealth : 6000f);
        displayedHp = bossHealth.currentHealth;
        ghostHp = displayedHp;
        ghostWaitTimer = 0f;
        currentPhase = 1;

        if (bossNameText != null)
        {
            bossNameText.text = $"👑 BOSS: {bossName.ToUpper()}";
        }

        UpdateHealthDisplays(displayedHp, maxHp);
        UpdatePhaseBadge(1f);

        if (animCoroutine != null) StopCoroutine(animCoroutine);
        animCoroutine = StartCoroutine(EntranceAnimation());

        gameObject.SetActive(true);
    }

    private IEnumerator EntranceAnimation()
    {
        if (canvasGroup == null || containerRect == null) yield break;

        Vector2 targetPos = new Vector2(0f, -175f);
        Vector2 startPos = new Vector2(0f, -120f);
        containerRect.anchoredPosition = startPos;
        canvasGroup.alpha = 0f;

        float duration = 0.45f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float easeOut = Mathf.Sin(t * Mathf.PI * 0.5f);

            containerRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, easeOut);
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
            yield return null;
        }

        containerRect.anchoredPosition = targetPos;
        canvasGroup.alpha = 1f;
    }

    private void StartDefeatSequence()
    {
        if (isDefeated) return;
        isDefeated = true;

        if (animCoroutine != null) StopCoroutine(animCoroutine);
        animCoroutine = StartCoroutine(DefeatAnimationRoutine());
    }

    private IEnumerator DefeatAnimationRoutine()
    {
        if (phaseBadgeText != null)
        {
            phaseBadgeText.text = "💀 ĐÃ BỊ HẠ GỤC! 💀";
            phaseBadgeText.color = new Color(1f, 0.3f, 0.3f, 1f);
        }

        if (healthNumbersText != null)
        {
            healthNumbersText.text = "CHIẾN THẮNG! (STAGE CLEAR)";
            healthNumbersText.color = new Color(1f, 0.9f, 0.2f, 1f);
        }

        if (healthSlider != null) healthSlider.value = 0f;

        // Rút cạn thanh ghost nhanh chóng
        float elapsed = 0f;
        float startGhost = ghostSlider != null ? ghostSlider.value : 0f;
        while (elapsed < 0.6f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / 0.6f;
            if (ghostSlider != null) ghostSlider.value = Mathf.Lerp(startGhost, 0f, t);
            yield return null;
        }
        if (ghostSlider != null) ghostSlider.value = 0f;

        yield return new WaitForSeconds(1.2f);

        // Mờ dần thanh máu
        elapsed = 0f;
        while (elapsed < 0.5f)
        {
            elapsed += Time.deltaTime;
            if (canvasGroup != null) canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / 0.5f);
            yield return null;
        }

        if (canvasGroup != null) canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (isDefeated || currentBossHealth == null) return;

        // Cập nhật giá trị máu thời gian thực
        float actualHp = Mathf.Max(0f, currentBossHealth.currentHealth);
        if (currentBossHealth.maxHealth > maxHp) maxHp = currentBossHealth.maxHealth;

        // Nếu Boss mất máu: Thanh chính tụt ngay, thanh ghost delay 0.2s rồi tuột theo
        if (actualHp < displayedHp)
        {
            displayedHp = actualHp;
            ghostWaitTimer = 0.25f;
        }
        else if (actualHp > displayedHp)
        {
            displayedHp = actualHp;
            ghostHp = actualHp;
        }

        // Xử lý thanh Ghost tuột từ từ mượt mà
        if (ghostWaitTimer > 0f)
        {
            ghostWaitTimer -= Time.deltaTime;
        }
        else
        {
            ghostHp = Mathf.MoveTowards(ghostHp, displayedHp, maxHp * 0.45f * Time.deltaTime);
        }

        UpdateHealthDisplays(displayedHp, maxHp);

        // Kiểm tra Phase dựa theo % máu
        float hpRatio = maxHp > 0 ? displayedHp / maxHp : 0f;
        UpdatePhaseBadge(hpRatio);

        // Nếu Boss chết
        if (actualHp <= 0f)
        {
            StartDefeatSequence();
        }
    }

    private void UpdateHealthDisplays(float current, float max)
    {
        float ratio = max > 0 ? Mathf.Clamp01(current / max) : 0f;
        float ghostRatio = max > 0 ? Mathf.Clamp01(ghostHp / max) : 0f;

        if (healthSlider != null) healthSlider.value = ratio;
        if (ghostSlider != null) ghostSlider.value = ghostRatio;

        if (healthNumbersText != null && !isDefeated)
        {
            int currentInt = Mathf.CeilToInt(current);
            int maxInt = Mathf.CeilToInt(max);
            int percent = Mathf.RoundToInt(ratio * 100f);
            healthNumbersText.text = $"{currentInt:N0} / {maxInt:N0} ({percent}%)";
        }
    }

    private void UpdatePhaseBadge(float hpRatio)
    {
        if (phaseBadgeText == null || isDefeated) return;

        if (hpRatio > 0.70f)
        {
            if (currentPhase != 1)
            {
                currentPhase = 1;
                SetHealthBarColor(new Color(0.95f, 0.35f, 0.15f, 1f)); // Cam đỏ
            }
            phaseBadgeText.text = "[ GIAI ĐOẠN 1: BÌNH THƯỜNG ]";
            phaseBadgeText.color = new Color(0.4f, 0.95f, 0.4f, 1f); // Xanh lá
        }
        else if (hpRatio > 0.30f)
        {
            if (currentPhase != 2)
            {
                currentPhase = 2;
                SetHealthBarColor(new Color(1f, 0.5f, 0.1f, 1f)); // Vàng cam
            }
            phaseBadgeText.text = "[ GIAI ĐOẠN 2: NƯỚC MẮT TUÔN RƠI ]";
            phaseBadgeText.color = new Color(1f, 0.85f, 0.2f, 1f); // Vàng rực
        }
        else
        {
            if (currentPhase != 3)
            {
                currentPhase = 3;
                SetHealthBarColor(new Color(1f, 0.15f, 0.15f, 1f)); // Đỏ rực nộ
            }
            // Nhấp nháy màu đỏ cảnh báo nộ
            float pulse = Mathf.Sin(Time.time * 8f) * 0.3f + 0.7f;
            phaseBadgeText.text = "[ 🔥 GIAI ĐOẠN 3: CUỒNG NỘ! 🔥 ]";
            phaseBadgeText.color = new Color(1f, 0.2f * pulse, 0.2f * pulse, 1f);
        }
    }

    private void SetHealthBarColor(Color c)
    {
        if (healthFillImage != null)
        {
            healthFillImage.color = c;
        }
    }

    // =========================================================================
    // XÂY DỰNG GIAO DIỆN PROCEDURAL UI CHO THANH MÁU BOSS (ZERO MISSING ASSET)
    // =========================================================================
    private void BuildProceduralUI()
    {
        containerRect = GetComponent<RectTransform>();
        if (containerRect == null) containerRect = gameObject.AddComponent<RectTransform>();

        // Căn chỉnh Neo ở Top-Center màn hình, rộng 880px, cao 76px, cách đỉnh -175px
        containerRect.anchorMin = new Vector2(0.5f, 1f);
        containerRect.anchorMax = new Vector2(0.5f, 1f);
        containerRect.pivot = new Vector2(0.5f, 1f);
        containerRect.anchoredPosition = new Vector2(0f, -175f);
        containerRect.sizeDelta = new Vector2(880f, 76f);

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // 1. Khung nền tối mờ sang trọng bo viền
        GameObject bgObj = new GameObject("FrameBackground");
        bgObj.transform.SetParent(containerRect, false);
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.08f, 0.08f, 0.12f, 0.94f); // Tím đen vũ trụ huyền bí

        // 2. Dòng Tiêu Đề Trên (Icon, Tên Boss, Huy Hiệu Phase)
        GameObject headerObj = new GameObject("HeaderRow");
        headerObj.transform.SetParent(containerRect, false);
        RectTransform headerRect = headerObj.AddComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 0.52f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.offsetMin = new Vector2(16f, 0f);
        headerRect.offsetMax = new Vector2(-16f, -4f);

        // Icon Mèo Chuối / Vương miện
        GameObject iconObj = new GameObject("BossIcon");
        iconObj.transform.SetParent(headerRect, false);
        RectTransform iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.sizeDelta = new Vector2(34f, 34f);
        iconRect.anchoredPosition = new Vector2(0f, 0f);
        bossIconImage = iconObj.AddComponent<Image>();
        bossIconImage.sprite = BossMeoChuoiAssets.GetBananaPeelSprite();

        // Tên Boss
        GameObject nameObj = new GameObject("BossNameText");
        nameObj.transform.SetParent(headerRect, false);
        RectTransform nameRect = nameObj.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0f, 0f);
        nameRect.anchorMax = new Vector2(0.6f, 1f);
        nameRect.pivot = new Vector2(0f, 0.5f);
        nameRect.offsetMin = new Vector2(40f, 0f);
        nameRect.offsetMax = Vector2.zero;
        bossNameText = nameObj.AddComponent<TextMeshProUGUI>();
        bossNameText.text = "👑 BOSS: MÈO CHUỐI";
        bossNameText.fontSize = 22;
        bossNameText.fontStyle = FontStyles.Bold;
        bossNameText.alignment = TextAlignmentOptions.MidlineLeft;
        bossNameText.color = new Color(1f, 0.9f, 0.25f, 1f); // Vàng hoàng gia

        // Huy hiệu Phase
        GameObject phaseObj = new GameObject("PhaseBadgeText");
        phaseObj.transform.SetParent(headerRect, false);
        RectTransform phaseRect = phaseObj.AddComponent<RectTransform>();
        phaseRect.anchorMin = new Vector2(0.55f, 0f);
        phaseRect.anchorMax = new Vector2(1f, 1f);
        phaseRect.pivot = new Vector2(1f, 0.5f);
        phaseRect.offsetMin = Vector2.zero;
        phaseRect.offsetMax = Vector2.zero;
        phaseBadgeText = phaseObj.AddComponent<TextMeshProUGUI>();
        phaseBadgeText.text = "[ GIAI ĐOẠN 1: BÌNH THƯỜNG ]";
        phaseBadgeText.fontSize = 18;
        phaseBadgeText.fontStyle = FontStyles.Bold;
        phaseBadgeText.alignment = TextAlignmentOptions.MidlineRight;
        phaseBadgeText.color = new Color(0.4f, 0.95f, 0.4f, 1f);

        // 3. Khung Thanh Máu Dưới (Track, Ghost Bar, Main Bar, Numbers Text)
        GameObject barContainerObj = new GameObject("HealthBarContainer");
        barContainerObj.transform.SetParent(containerRect, false);
        RectTransform barRect = barContainerObj.AddComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0f, 0f);
        barRect.anchorMax = new Vector2(1f, 0.48f);
        barRect.offsetMin = new Vector2(14f, 8f);
        barRect.offsetMax = new Vector2(-14f, -4f);

        // Khung nền đen thanh máu
        Image trackBg = barContainerObj.AddComponent<Image>();
        trackBg.color = new Color(0.12f, 0.05f, 0.05f, 1f);

        // Thanh Ghost Lag (Màu vàng nhạt/trắng tụt chậm)
        GameObject ghostObj = CreateSliderObject("GhostSlider", barRect, new Color(1f, 0.95f, 0.6f, 0.85f), out ghostSlider, out ghostFillImage);

        // Thanh Máu Chính (Màu cam đỏ tươi)
        GameObject mainHpObj = CreateSliderObject("MainHealthSlider", barRect, new Color(0.95f, 0.25f, 0.15f, 1f), out healthSlider, out healthFillImage);

        // Chữ hiển thị số máu & %
        GameObject numObj = new GameObject("HealthNumbersText");
        numObj.transform.SetParent(barRect, false);
        RectTransform numRect = numObj.AddComponent<RectTransform>();
        numRect.anchorMin = Vector2.zero;
        numRect.anchorMax = Vector2.one;
        numRect.sizeDelta = Vector2.zero;
        healthNumbersText = numObj.AddComponent<TextMeshProUGUI>();
        healthNumbersText.text = "7,800 / 7,800 (100%)";
        healthNumbersText.fontSize = 17;
        healthNumbersText.fontStyle = FontStyles.Bold;
        healthNumbersText.alignment = TextAlignmentOptions.Center;
        healthNumbersText.color = Color.white;

        // Thêm Outline nhẹ cho chữ dễ đọc
        healthNumbersText.outlineWidth = 0.25f;
        healthNumbersText.outlineColor = new Color32(20, 20, 20, 255);
    }

    private GameObject CreateSliderObject(string name, RectTransform parent, Color fillColor, out Slider slider, out Image fillImage)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;

        slider = obj.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;

        GameObject fillArea = new GameObject("FillArea");
        fillArea.transform.SetParent(rect, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.sizeDelta = Vector2.zero;

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillAreaRect, false);
        RectTransform fillRect = fill.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;

        fillImage = fill.AddComponent<Image>();
        fillImage.color = fillColor;

        slider.fillRect = fillRect;
        slider.targetGraphic = fillImage;

        return obj;
    }
}

