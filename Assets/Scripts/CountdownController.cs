using System.Collections;
using UnityEngine;
using TMPro;

public class CountdownController : MonoBehaviour
{
    public TextMeshProUGUI startCountdownText; // Chữ đếm ngược 3 2 1
    public GameObject dimBackground; // Nền mờ
    public GameObject gameplayUIRoot; // Khung UI trong game (chứa Joystick, máu, timer...)

    void Start()
    {
        StartCoroutine(CountdownRoutine());
    }

    IEnumerator CountdownRoutine()
    {
        // Đóng băng game
        Time.timeScale = 0f;

        // Bật nền mờ
        if (dimBackground != null) dimBackground.SetActive(true);

        // ẨN TOÀN BỘ UI TRONG GAME (Joystick, máu, thời gian sinh tồn...)
        if (gameplayUIRoot != null) gameplayUIRoot.SetActive(false);

        // Bật chữ đếm ngược
        startCountdownText.gameObject.SetActive(true);

        string[] sequence = { "3", "2", "1", "GO!" };

        foreach (string word in sequence)
        {
            yield return StartCoroutine(AnimateWord(word));
        }

        // Kết thúc đếm ngược: Ẩn chữ và nền mờ
        startCountdownText.gameObject.SetActive(false);
        if (dimBackground != null) dimBackground.SetActive(false);

        // HIỆN LẠI UI TRONG GAME khi bắt đầu chơi
        if (gameplayUIRoot != null) gameplayUIRoot.SetActive(true);

        // Rã đông game
        Time.timeScale = 1f;
    }

    IEnumerator AnimateWord(string word)
    {
        startCountdownText.text = word;
        Color originalColor = startCountdownText.color;

        float duration = 1f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float timePercent = elapsed / duration;

            float scale = Mathf.Lerp(1.5f, 1f, timePercent);
            startCountdownText.transform.localScale = new Vector3(scale, scale, 1f);

            float alpha = timePercent < 0.7f ? 1f : Mathf.Lerp(1f, 0f, (timePercent - 0.7f) / 0.3f);
            startCountdownText.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);

            yield return null;
        }
    }
}