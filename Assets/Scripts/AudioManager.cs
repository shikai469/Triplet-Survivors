using UnityEngine;
using UnityEngine.SceneManagement; // Bắt buộc thêm thư viện này để quản lý cảnh

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    public AudioSource bgmSource;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    // Đăng ký sự kiện khi object được bật
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // Hủy đăng ký sự kiện khi object bị tắt (rất quan trọng để tránh lỗi bộ nhớ)
    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Hàm này sẽ tự động chạy mỗi khi một Scene bất kỳ load xong
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Kiểm tra xem scene vừa load có phải là GameplayScene không
        if (scene.name == "GameplayScene")
        {
            // Nếu đúng là GameplayScene, AudioManager của Menu sẽ tự sát!
            // Nhạc nền của menu sẽ tắt ngay lập tức.
            Destroy(gameObject);
        }
    }

    void Start()
    {
        UpdateVolume();
    }

    public void UpdateVolume()
    {
        float masterVol = PlayerPrefs.GetFloat("MasterVolume", 1f);
        float musicVol = PlayerPrefs.GetFloat("MusicVolume", 1f);

        if (bgmSource != null)
        {
            bgmSource.volume = masterVol * musicVol;
        }
    }
}