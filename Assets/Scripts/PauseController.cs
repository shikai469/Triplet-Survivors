using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseController : MonoBehaviour
{
    [Header("UI Panels & Buttons")]
    public GameObject pausePanel;         // Object PausePanel che mờ
    public Button btnPause;              // Nút pause trên màn hình góc trên phải
    public Button btnResume;             // Nút Tiếp Tục
    public Button btnRestart;            // Nút Bắt Đầu Lại
    public Button btnSettings;           // Nút Cài Đặt
    public Button btnQuit;               // Nút Thoát

    [Header("Gold Setting")]
    // Lưu số vàng ban đầu trước khi vào trận để hoàn trả nếu Restart
    private int startGoldThisRun = 0;

    void Start()
    {
        // Ẩn panel tạm dừng mặc định lúc mới vào màn
        if (pausePanel != null) pausePanel.SetActive(false);

        // Lưu lại mốc vàng của người chơi trước khi màn chơi bắt đầu
        startGoldThisRun = GameDataManager.GetTotalGold();

        // Gán sự kiện click
        if (btnPause != null) btnPause.onClick.AddListener(PauseGame);
        if (btnResume != null) btnResume.onClick.AddListener(ResumeGame);
        if (btnRestart != null) btnRestart.onClick.AddListener(RestartGame);
        if (btnSettings != null) btnSettings.onClick.AddListener(OpenSettings);
        if (btnQuit != null) btnQuit.onClick.AddListener(QuitToMainMenu);
    }

    // 1. Tạm dừng game
    public void PauseGame()
    {
        if (pausePanel != null) pausePanel.SetActive(true);
        Time.timeScale = 0f; // Đóng băng mọi chuyển động gameplay
    }

    // 2. Tiếp tục game
    public void ResumeGame()
    {
        Time.timeScale = 1f; // Khôi phục thời gian
        if (pausePanel != null) pausePanel.SetActive(false);
    }

    // 3. Bắt đầu lại (Khôi phục số vàng ban đầu, không cộng vàng nhặt được trong trận này)
    public void RestartGame()
    {
        Time.timeScale = 1f;

        // Trả lại số vàng về đúng mốc trước khi bắt đầu màn
        int currentGold = GameDataManager.GetTotalGold();
        int goldEarnedThisRun = currentGold - startGoldThisRun;
        if (goldEarnedThisRun > 0)
        {
            GameDataManager.AddGold(-goldEarnedThisRun);
        }

        // Tải lại scene hiện tại
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // 4. Mở Cài đặt (Thêm panel sau)
    public void OpenSettings()
    {
        Debug.Log("[SETTINGS] Sẽ mở bảng Cài Đặt sau...");
    }

    // 5. Thoát về MainMenu
    public void QuitToMainMenu()
    {
        Time.timeScale = 1f; // Luôn reset Time.timeScale = 1 trước khi load scene khác
        SceneManager.LoadScene("MainMenu");
    }

    void OnDestroy()
    {
        // Đảm bảo Time.timeScale luôn trở lại bình thường nếu đối tượng bị hủy
        Time.timeScale = 1f;
    }
}