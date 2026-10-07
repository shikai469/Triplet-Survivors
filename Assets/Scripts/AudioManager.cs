using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance; // Singleton để dễ dàng gọi từ các script khác
    public AudioSource bgmSource; // Nguồn phát nhạc nền (Background Music)

    void Awake()
    {
        // Đảm bảo chỉ có 1 AudioManager duy nhất tồn tại khi chuyển cảnh
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Lệnh này giúp object KHÔNG bị hủy khi chuyển scene
        }
        else
        {
            Destroy(gameObject); // Nếu đã có 1 AudioManager khác từ trước, hủy bản sao mới này
            return;
        }
    }

    void Start()
    {
        UpdateVolume(); // Cập nhật âm lượng ngay khi bắt đầu
    }

    // Hàm này dùng để cập nhật lại âm lượng dựa trên giá trị đã lưu
    public void UpdateVolume()
    {
        // Lấy giá trị âm lượng đã lưu, nếu chưa lưu thì mặc định là 1 (100%)
        float masterVol = PlayerPrefs.GetFloat("MasterVolume", 1f);
        float musicVol = PlayerPrefs.GetFloat("MusicVolume", 1f);

        if (bgmSource != null)
        {
            // Âm lượng nhạc nền = Âm tổng * Âm nhạc
            bgmSource.volume = masterVol * musicVol;
        }
    }
}