using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class SettingsManager : MonoBehaviour
{
    // Cờ đánh dấu SettingsScene đang được mở đè từ menu Tạm Dừng trong trận
    public static bool IsOpenedFromPause = false;

    [Header("UI Buttons (Kéo nút vào đây)")]
    public Button btnBack;     // Nút Quay Lại
    public Button btnFeedback; // Nút Phản Hồi (PhanHoi)
    public Button btnPolicy;   // Nút Chính Sách (ChinhSach)

    [Header("Links")]
    public string feedbackUrl = "https://github.com/shikai469/Triplet-Survivors/issues";
    public string policyUrl = "https://github.com/shikai469/Triplet-Survivors";

    [Header("Sliders")]
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;

    void Awake()
    {
        Scene myScene = gameObject.scene;

        // Tự động tìm nút bên trong SettingsScene nếu quên kéo vào Inspector
        if (btnBack == null) btnBack = FindButtonInMyScene(myScene, "Btn_Back");
        if (btnFeedback == null) btnFeedback = FindButtonInMyScene(myScene, "PhanHoi");
        if (btnPolicy == null) btnPolicy = FindButtonInMyScene(myScene, "ChinhSach");

        // Nếu mở đè (Additive) lên GameplayScene:
        // Tắt Camera phụ & EventSystem phụ của SettingsScene để không báo lỗi trùng lặp
        // và nâng thứ tự hiển thị (sortingOrder) của Canvas lên trên cùng
        if (IsOpenedFromPause)
        {
            foreach (GameObject root in myScene.GetRootGameObjects())
            {
                Camera cam = root.GetComponentInChildren<Camera>(true);
                if (cam != null) cam.gameObject.SetActive(false);

                EventSystem es = root.GetComponentInChildren<EventSystem>(true);
                if (es != null) es.gameObject.SetActive(false);

                Canvas canvas = root.GetComponentInChildren<Canvas>(true);
                if (canvas != null)
                {
                    canvas.overrideSorting = true;
                    canvas.sortingOrder = 100; // Hiển thị đè lên trên PausePanel
                }
            }
        }
    }

    void Start()
    {
        // Gán sự kiện cho nút Btn_Back (xoá listener cũ nếu có để tránh gọi trùng)
        if (btnBack != null)
        {
            btnBack.onClick.RemoveAllListeners();
            btnBack.onClick.AddListener(OnBackClicked);
        }

        // Gán sự kiện mở link cho nút PhanHoi
        if (btnFeedback != null)
        {
            btnFeedback.onClick.RemoveListener(OpenFeedbackLink);
            btnFeedback.onClick.AddListener(OpenFeedbackLink);
        }

        // Gán sự kiện mở link cho nút ChinhSach
        if (btnPolicy != null)
        {
            btnPolicy.onClick.RemoveListener(OpenPolicyLink);
            btnPolicy.onClick.AddListener(OpenPolicyLink);
        }

        // Cập nhật vị trí thanh kéo theo giá trị đã lưu
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.value = PlayerPrefs.GetFloat("MasterVolume", 1f);
            masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVolume", 1f);
            musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
        }
    }

    private Button FindButtonInMyScene(Scene scene, string targetName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform[] allChildren = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform t in allChildren)
            {
                if (t.name == targetName)
                    return t.GetComponent<Button>();
            }
        }
        return null;
    }

    public void OpenFeedbackLink()
    {
        if (!string.IsNullOrEmpty(feedbackUrl))
        {
            Application.OpenURL(feedbackUrl);
        }
    }

    public void OpenPolicyLink()
    {
        if (!string.IsNullOrEmpty(policyUrl))
        {
            Application.OpenURL(policyUrl);
        }
    }

    public void SetMasterVolume(float value)
    {
        PlayerPrefs.SetFloat("MasterVolume", value);
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.UpdateVolume();
        }
    }

    public void SetMusicVolume(float value)
    {
        PlayerPrefs.SetFloat("MusicVolume", value);
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.UpdateVolume();
        }
    }

    // Xử lý thông minh khi nhấn nút < Quay Lại (Btn_Back)
    public void OnBackClicked()
    {
        if (IsOpenedFromPause)
        {
            // Nếu mở từ lúc Pause trong trận -> Chỉ đóng SettingsScene để quay lại bảng Tạm Dừng
            IsOpenedFromPause = false;
            SceneManager.UnloadSceneAsync(gameObject.scene);
        }
        else
        {
            // Nếu mở từ MainMenu -> Quay về MainMenu như bình thường
            SceneManager.LoadScene("MainMenu");
        }
    }

    // Giữ lại tên hàm cũ phòng trường hợp nút trong Inspector đã gán sẵn hàm này
    public void BackToMainMenu()
    {
        OnBackClicked();
    }
}