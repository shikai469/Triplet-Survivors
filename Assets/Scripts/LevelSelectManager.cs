using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class LevelSelectManager : MonoBehaviour
{
    [Header("UI Animations")]
    public Animator uiAnimator;

    [Header("Background Transition")]
    public Image bgMain;
    public Image bgTransition;
    public float fadeSpeed = 2f;

    [Header("Detail Panel Texts")]
    public TextMeshProUGUI txtLevelName;
    public TextMeshProUGUI txtDetail;
    public TextMeshProUGUI txtBossTime;
    public TextMeshProUGUI txtClearCount;
    public TextMeshProUGUI txtDeathCount;
    public TextMeshProUGUI txtSurvivalTime;

    [Header("Navigation Buttons")]
    public Button btnBack; // Nút Quay Lại
    public Button btnPlay; // Nút CHƠI (Bắt Đầu Game)

    [Header("Default Selection (Optional)")]
    public LevelNode defaultSelectedNode;

    private Coroutine fadeCoroutine;
    private string selectedLevelName;
    private DifficultyLevel selectedDifficulty = DifficultyLevel.Normal;

    void Start()
    {
        // Gán sự kiện cho 2 nút điều hướng
        if (btnBack != null)
        {
            btnBack.onClick.AddListener(BackToMainMenu);
        }

        if (btnPlay != null)
        {
            btnPlay.onClick.AddListener(StartGame);
        }

        // Tự động chọn màn đã chơi lần trước hoặc node mặc định
        DifficultyLevel lastDiff = GameDataManager.GetSelectedDifficulty();
        LevelNode[] allNodes = FindObjectsByType<LevelNode>(FindObjectsSortMode.None);

        LevelNode targetNode = null;
        foreach (var node in allNodes)
        {
            if (node.difficulty == lastDiff)
            {
                targetNode = node;
                break;
            }
        }

        if (targetNode == null && defaultSelectedNode != null)
        {
            targetNode = defaultSelectedNode;
        }

        if (targetNode != null)
        {
            OnLevelSelected(targetNode);
        }
    }

    public void OnLevelSelected(LevelNode node)
    {
        if (node == null) return;

        // Làm mới dữ liệu kỷ lục thực tế
        node.RefreshRecords();

        selectedDifficulty = node.difficulty;
        selectedLevelName = node.levelName;

        // 1. Cập nhật toàn bộ thông tin lên UI
        if (txtLevelName != null) txtLevelName.text = node.levelName;
        if (txtDetail != null) txtDetail.text = node.levelDetail;

        if (txtBossTime != null) txtBossTime.text = $"Boss Time: {node.bossTime}";
        if (txtClearCount != null) txtClearCount.text = $"Clear Count: {node.clearCount}";
        if (txtDeathCount != null) txtDeathCount.text = $"Death Count: {node.deathCount}";
        if (txtSurvivalTime != null) txtSurvivalTime.text = $"Survival Time: {node.survivalTime}";

        // 2. Kích hoạt Animation giao diện
        if (uiAnimator != null)
        {
            uiAnimator.SetTrigger("Show");
        }

        // 3. Đổi ảnh nền mượt mà
        if (node.mapPreviewImage != null && bgTransition != null && bgMain != null)
        {
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(CrossfadeBackground(node.mapPreviewImage));
        }
    }

    private IEnumerator CrossfadeBackground(Sprite newBg)
    {
        bgTransition.sprite = newBg;

        Color c = bgTransition.color;
        c.a = 0f;
        bgTransition.color = c;

        while (bgTransition.color.a < 1f)
        {
            c.a += Time.deltaTime * fadeSpeed;
            bgTransition.color = c;
            yield return null;
        }

        bgMain.sprite = newBg;
        c.a = 0f;
        bgTransition.color = c;
    }

    // --- CÁC HÀM XỬ LÝ NÚT BẤM ---

    public void BackToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void StartGame()
    {
        // Lưu trạng thái độ khó đã chọn vào GameDataManager
        GameDataManager.SetSelectedDifficulty(selectedDifficulty);
        Debug.Log($"[LEVEL SELECT] Bắt đầu màn: {selectedLevelName} | Độ khó: {selectedDifficulty} -> Tải GameplayScene");

        // Tải duy nhất 1 Scene Gameplay (Scene tự thích ứng độ khó)
        SceneManager.LoadScene("GameplayScene");
    }
}