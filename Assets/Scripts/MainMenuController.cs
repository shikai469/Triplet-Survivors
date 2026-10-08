using UnityEngine;
using UnityEngine.SceneManagement; // Thư viện bắt buộc để load màn chơi

public class MainMenuController : MonoBehaviour
{
    public void PlayGame()
    {
        SceneManager.LoadScene("LevelSelectScene");
    }

    public void OpenPowerUps()
    {
        SceneManager.LoadScene("UpgradeScene");
    }

    public void OpenSettings()
    {
        SceneManager.LoadScene("SettingsScene");
    }

    public void QuitGame()
    {
        Debug.Log("Thoát ứng dụng!");
        Application.Quit(); // Lệnh này sẽ đóng app khi bạn build ra game thật (.apk / .exe)
    }
}