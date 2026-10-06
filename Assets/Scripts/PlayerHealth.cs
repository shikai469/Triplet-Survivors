using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHealth : MonoBehaviour
{
    public static PlayerHealth Instance;

    [Header("HP Settings (Isekai Core)")]
    public float maxHealth = 100f;
    public float currentHealth;
    public float hpRecoveryPerSecond = 0f; // Hồi máu mỗi giây
    public int revivalCount = 0;           // Số lần hồi sinh (mặc định 0)

    [Header("Invincibility Settings")]
    public float invincibilityDuration = 0.5f; // Chuẩn 0.5s sau khi dính đòn
    private bool isInvincible = false;

    [Header("UI References")]
    public Slider healthSlider;        // Thanh máu Slider
    public TextMeshProUGUI healthText; // Hiển thị số dạng "100 / 100"

    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        currentHealth = maxHealth;
        UpdateHealthUI();
    }

    void Update()
    {
        // Cơ chế hồi máu tự động mỗi giây
        if (hpRecoveryPerSecond > 0f && currentHealth > 0 && currentHealth < maxHealth)
        {
            currentHealth += hpRecoveryPerSecond * Time.deltaTime;
            currentHealth = Mathf.Min(currentHealth, maxHealth);
            UpdateHealthUI();
        }
    }

    // Nhận sát thương và áp dụng trừ Giáp (Armor)
    public void TakeDamage(int incomingDamage)
    {
        if (isInvincible || currentHealth <= 0) return;

        // Lấy chỉ số giáp từ PlayerStats
        int playerArmor = 0;
        if (PlayerStats.Instance != null)
        {
            playerArmor = PlayerStats.Instance.armor;
        }

        // Công thức chuẩn: Max(1, Damage - Armor)
        int actualDamage = Mathf.Max(1, incomingDamage - playerArmor);
        currentHealth -= actualDamage;

        UpdateHealthUI();

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(InvincibilityRoutine(invincibilityDuration));
        }
    }

    public void Heal(float amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        UpdateHealthUI();
    }

    IEnumerator InvincibilityRoutine(float duration)
    {
        isInvincible = true;

        // Hiệu ứng nhấp nháy mờ sprite trong lúc bất tử
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(1f, 1f, 1f, 0.3f);
                yield return new WaitForSeconds(0.1f);
                spriteRenderer.color = Color.white;
                yield return new WaitForSeconds(0.1f);
            }
            elapsed += 0.2f;
        }

        if (spriteRenderer != null) spriteRenderer.color = Color.white;
        isInvincible = false;
    }

    void Die()
    {
        if (revivalCount > 0)
        {
            revivalCount--;
            currentHealth = maxHealth * 0.5f;
            UpdateHealthUI();
            StartCoroutine(InvincibilityRoutine(3.0f));
            return;
        }

        // 1. Lấy dữ liệu vàng và quái hạ gục trong trận
        int totalGoldCollected = 0;
        int totalKills = 0;

        if (PlayerStats.Instance != null)
        {
            totalGoldCollected = PlayerStats.Instance.goldCount;
            // Lưu toàn bộ vàng kiếm được vào tài khoản
            GameDataManager.AddGold(totalGoldCollected);
            Debug.Log($"[GAME OVER] Đã lưu {totalGoldCollected} vàng vào tài khoản!");
        }

        if (GameStageManager.Instance != null)
        {
            totalKills = GameStageManager.Instance.killCount;
        }

        // 2. Kích hoạt Panel Game Over
        float surviveTime = Time.timeSinceLevelLoad;
        if (GameOverController.Instance != null)
        {
            GameOverController.Instance.TriggerGameOver(totalKills, totalGoldCollected, surviveTime);
        }
        else
        {
            Time.timeScale = 0f;
        }

        Debug.Log("Player đã bị tiêu diệt! Kích hoạt Game Over.");
    }

    public void UpdateHealthUI()
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }

        if (healthText != null)
        {
            healthText.text = $"{Mathf.CeilToInt(currentHealth)} / {Mathf.CeilToInt(maxHealth)}";
        }
    }

    // Hàm public để rương loot gọi tính năng bất tử
    public void ActivateInvincibility(float duration)
    {
        StartCoroutine(InvincibilityRoutine(duration));
    }
}