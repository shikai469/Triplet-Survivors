using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GameOverController : MonoBehaviour
{
    public static GameOverController Instance;

    [Header("UI Canvas Groups & Panels")]
    public CanvasGroup gameOverCanvasGroup;
    public GameObject gameplayUIRoot;
    public GameObject levelUpPanel;

    [Header("Texts")]
    public TextMeshProUGUI textGameOver;
    public TextMeshProUGUI textScore;
    public TextMeshProUGUI textStats;
    public TextMeshProUGUI textRankStamp;
    public GameObject bottomActionGroup;

    [Header("Buttons")]
    public Button btnRetry;
    public Button btnGiveUp;

    [Header("Score & Stamp Settings")]
    public int maxScoreCap = 99999;          // Trần điểm tối đa 9999
    public float stampSpacingX = 35f;       // Khoảng cách từ mép chữ điểm đến con dấu

    [HideInInspector]
    public bool IsGameOver = false;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (gameOverCanvasGroup != null)
        {
            gameOverCanvasGroup.alpha = 0f;
            gameOverCanvasGroup.gameObject.SetActive(false);
        }

        if (btnRetry != null) btnRetry.onClick.AddListener(OnRetryClicked);
        if (btnGiveUp != null) btnGiveUp.onClick.AddListener(OnGiveUpClicked);
    }

    public void TriggerGameOver(int kills, int goldCollected, float surviveTime)
    {
        if (IsGameOver) return;
        IsGameOver = true;

        if (levelUpPanel != null)
        {
            levelUpPanel.SetActive(false);
        }

        transform.SetAsLastSibling();
        if (gameOverCanvasGroup != null)
        {
            gameOverCanvasGroup.transform.SetAsLastSibling();
        }

        StopPlayerCompletely();

        if (gameplayUIRoot != null)
        {
            gameplayUIRoot.SetActive(false);
        }

        StartCoroutine(GameOverSequenceRoutine(kills, goldCollected, surviveTime));
    }

    private void StopPlayerCompletely()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            Rigidbody2D rb = playerObj.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.bodyType = RigidbodyType2D.Kinematic;
            }

            MonoBehaviour playerMove = playerObj.GetComponent("PlayerMovement") as MonoBehaviour;
            if (playerMove != null)
            {
                playerMove.enabled = false;
            }
        }
    }

    private IEnumerator GameOverSequenceRoutine(int kills, int goldCollected, float surviveTime)
    {
        gameOverCanvasGroup.gameObject.SetActive(true);
        if (levelUpPanel != null) levelUpPanel.SetActive(false);

        if (textScore != null) textScore.gameObject.SetActive(false);
        if (textStats != null) textStats.gameObject.SetActive(false);
        if (textRankStamp != null) textRankStamp.gameObject.SetActive(false);
        if (bottomActionGroup != null) bottomActionGroup.SetActive(false);

        // BƯỚC 1: Hiện mờ dần nền và GAME OVER
        float fadeDuration = 0.5f;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            gameOverCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }
        gameOverCanvasGroup.alpha = 1f;
        Time.timeScale = 0f;

        yield return new WaitForSecondsRealtime(0.25f);

        // BƯỚC 2: Tính điểm và kịch trần ở 9999
        int calculatedScore = (kills * 10) + (goldCollected * 5) + Mathf.RoundToInt(surviveTime * 2f);
        int targetScore = Mathf.Min(calculatedScore, maxScoreCap); // Chặn cứng tối đa 9999

        if (textScore != null)
        {
            textScore.gameObject.SetActive(true);
            float countDuration = 1.0f;
            elapsed = 0f;
            while (elapsed < countDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / countDuration);
                int currentScore = Mathf.RoundToInt(Mathf.Lerp(0, targetScore, progress));
                textScore.text = $"Điểm: {currentScore}";
                yield return null;
            }
            textScore.text = $"Điểm: {targetScore}";
            textScore.ForceMeshUpdate(); // Cập nhật kích thước khung chữ
        }

        yield return new WaitForSecondsRealtime(0.2f);

        // BƯỚC 3: Hiện Kill và Vàng
        if (textStats != null)
        {
            textStats.gameObject.SetActive(true);
            textStats.text = $"Kill: {kills}    Vàng: {goldCollected}";
        }

        yield return new WaitForSecondsRealtime(0.35f);

        // BƯỚC 4: Tự động căn chỉnh vị trí con dấu bên cạnh điểm và dập dấu
        if (bottomActionGroup != null)
        {
            bottomActionGroup.SetActive(true);
        }

        if (textRankStamp != null && textScore != null)
        {
            SetupRankText(targetScore);
            AlignStampPosition(); // Tự động đẩy con dấu sang phải cạnh đuôi chữ điểm
            yield return StartCoroutine(StampAnimationRoutine(textRankStamp.transform));
        }
    }

    // Tự động tính vị trí mép phải của chữ điểm để đặt con dấu
    private void AlignStampPosition()
    {
        RectTransform scoreRect = textScore.rectTransform;
        RectTransform stampRect = textRankStamp.rectTransform;

        // Chiều rộng thực tế của chuỗi ký tự "Điểm: xxxx"
        float textWidth = textScore.preferredWidth;

        // Nếu textScore căn giữa (Center Alignment):
        // Mép phải của chữ sẽ nằm tại: PosX của textScore + (textWidth / 2)
        float textRightEdgeX = scoreRect.localPosition.x + (textWidth * 0.5f);

        // Đặt con dấu bắt đầu ngay sau mép phải + khoảng cách đệm
        float stampTargetX = textRightEdgeX + stampSpacingX + (stampRect.sizeDelta.x * 0.5f);

        stampRect.localPosition = new Vector3(stampTargetX, scoreRect.localPosition.y + 15f, 0f);
    }

    private void SetupRankText(int score)
    {
        textRankStamp.gameObject.SetActive(true);
        if (score >= 3000)
        {
            textRankStamp.text = "SSS";
            textRankStamp.color = new Color(1f, 0.85f, 0f, 1f); // Vàng óng
        }
        else if (score >= 2000)
        {
            textRankStamp.text = "SS";
            textRankStamp.color = new Color(0.64f, 0.21f, 0.93f, 1f); // Tím huyền thoại (#A335EE)
        }
        else if (score >= 1200)
        {
            textRankStamp.text = "S";
            textRankStamp.color = new Color(1f, 0.27f, 0f, 1f); // Đỏ cam rực (#FF4500)
        }
        else if (score >= 600)
        {
            textRankStamp.text = "A";
            textRankStamp.color = new Color(0f, 0.75f, 1f, 1f); // Xanh lam (#00BFFF)
        }
        else
        {
            textRankStamp.text = "B";
            textRankStamp.color = new Color(0.75f, 0.75f, 0.75f, 1f); // Xám bạc (#C0C0C0)
        }
    }

    private IEnumerator StampAnimationRoutine(Transform targetTransform)
    {
        float duration = 0.22f;
        float elapsed = 0f;
        Vector3 initialScale = Vector3.one * 3.5f;
        Vector3 targetScale = Vector3.one;

        targetTransform.localScale = initialScale;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            targetTransform.localScale = Vector3.Lerp(initialScale, targetScale, t * t);
            yield return null;
        }

        targetTransform.localScale = targetScale;
    }

    void OnRetryClicked()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void OnGiveUpClicked()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}