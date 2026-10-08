using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // Thêm thư viện này để load Scene
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
    public Button btnBack; // Khai báo nút Quay Lại
    public Button btnPlay; // Khai báo nút CHƠI (Bắt Đầu Game)

    private Coroutine fadeCoroutine;
    private string selectedLevelName; // Biến lưu lại tên map người chơi vừa chọn

    void Start()
    {
        // Gán sự kiện cho 2 nút khi Scene vừa load lên
        if (btnBack != null)
        {
            btnBack.onClick.AddListener(BackToMainMenu);
        }

        if (btnPlay != null)
        {
            btnPlay.onClick.AddListener(StartGame);
        }
    }

    public void OnLevelSelected(LevelNode node)
    {
        // Lưu lại tên map để khi bấm "CHƠI" thì biết truyền vào màn nào
        selectedLevelName = node.levelName;

        // 1. Cập nhật toàn bộ thông tin lên UI
        if (txtLevelName != null) txtLevelName.text = node.levelName;
        if (txtDetail != null) txtDetail.text = node.levelDetail;

        if (txtBossTime != null) txtBossTime.text = $"Boss Time: {node.bossTime}";
        if (txtClearCount != null) txtClearCount.text = $"Clear Count: {node.clearCount}";
        if (txtDeathCount != null) txtDeathCount.text = $"Death Count: {node.deathCount}";
        if (txtSurvivalTime != null) txtSurvivalTime.text = $"Survival Time: {node.survivalTime}";

        // 2. Kích hoạt Animation
        if (uiAnimator != null)
        {
            uiAnimator.SetTrigger("Show");
        }

        // 3. Đổi ảnh nền
        if (node.mapPreviewImage != null)
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
        // Tùy theo logic game của bạn, bạn có thể truyền selectedLevelName sang GameplayScene.
        // Tạm thời, ta chỉ load scene GameplayScene.
        Debug.Log("Đang tải map: " + selectedLevelName);
        SceneManager.LoadScene("GameplayScene");
    }
}