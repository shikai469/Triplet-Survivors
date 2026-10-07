using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SettingsManager : MonoBehaviour
{
    [Header("UI Buttons")]
    public Button btnBack; // Thêm biến quản lý nút Quay Lại

    [Header("Sliders")]
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;

    void Start()
    {
        // Gán sự kiện chuyển cảnh cho nút Btn_Back bằng code
        if (btnBack != null)
        {
            btnBack.onClick.AddListener(BackToMainMenu);
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

    public void SetMasterVolume(float value)
    {
        PlayerPrefs.SetFloat("MasterVolume", value); // Lưu lại giá trị
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.UpdateVolume(); // Gọi AudioManager cập nhật
        }
    }

    public void SetMusicVolume(float value)
    {
        PlayerPrefs.SetFloat("MusicVolume", value); // Lưu lại giá trị
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.UpdateVolume();
        }
    }

    // Hàm này được gọi khi nhấn btnBack
    public void BackToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}