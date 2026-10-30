using UnityEngine;

/// <summary>
/// Kỹ năng 7: Giáp Hộ Thể (Energy Shield) [Max Lv 3]
/// Cơ chế: Cung cấp lớp giáp ảo bảo vệ người chơi. Khi bị tấn công, lớp giáp đỡ sát thương trước.
/// Tự hồi lại sau khoảng thời gian không nhận sát thương.
/// Tiến trình: Lên cấp tăng chỉ số/máu của lớp giáp (Lv 1: 30, Lv 2: 60, Lv 3: 100).
/// </summary>
public class EnergyShieldSkill : MonoBehaviour
{
    [Header("Skill Level")]
    public int currentLevel = 0;
    public const int MaxLevel = 3;

    [Header("Shield Stats")]
    public float maxShield = 0f;
    public float currentShield = 0f;
    public float regenDelay = 5.0f; // 5s không nhận sát thương sẽ hồi

    private float noDamageTimer = 0f;
    private GameObject shieldVisual;
    private SpriteRenderer shieldSr;

    public void SetLevel(int level)
    {
        currentLevel = Mathf.Clamp(level, 0, MaxLevel);
        enabled = (currentLevel > 0);

        if (currentLevel > 0)
        {
            switch (currentLevel)
            {
                case 1: maxShield = 30f; break;
                case 2: maxShield = 60f; break;
                case 3: maxShield = 100f; break;
            }

            if (currentShield <= 0)
            {
                currentShield = maxShield;
            }
            else
            {
                currentShield = Mathf.Min(currentShield, maxShield);
            }

            CreateVisualIfNeeded();
            UpdateVisual();
        }
        else
        {
            maxShield = 0f;
            currentShield = 0f;
            if (shieldVisual != null) shieldVisual.SetActive(false);
        }
    }

    private void CreateVisualIfNeeded()
    {
        if (shieldVisual == null)
        {
            shieldVisual = new GameObject("ShieldAura");
            shieldVisual.transform.SetParent(transform);
            shieldVisual.transform.localPosition = Vector3.zero;
            shieldVisual.transform.localScale = new Vector3(1.35f, 1.35f, 1f);

            shieldSr = shieldVisual.AddComponent<SpriteRenderer>();
            shieldSr.sprite = ProceduralSkillAssets.GetSprite("EnergyShieldCircle");
            // Đặt layer 9 (dưới Player là 10, dưới xa HealthCanvas là 50)
            shieldSr.sortingOrder = 9;

            // Đảm bảo thanh máu PlayerHealthCanvas luôn hiển thị rõ ràng trên cùng, không bao giờ bị khiên che khuất
            Canvas healthCanvas = GetComponentInChildren<Canvas>();
            if (healthCanvas != null)
            {
                healthCanvas.overrideSorting = true;
                healthCanvas.sortingOrder = 50;
            }
        }
    }

    void Update()
    {
        if (currentLevel <= 0) return;

        // Xoay nhẹ vòng khiên cho sinh động
        if (shieldVisual != null && shieldVisual.activeSelf)
        {
            shieldVisual.transform.Rotate(0, 0, 30f * Time.deltaTime);
        }

        // Tự động hồi giáp nếu không dính sát thương đủ thời gian quy định
        if (currentShield < maxShield)
        {
            noDamageTimer += Time.deltaTime;
            if (noDamageTimer >= regenDelay)
            {
                RegenerateShield();
            }
        }
    }

    private void RegenerateShield()
    {
        currentShield = maxShield;
        noDamageTimer = 0f;
        UpdateVisual();
        Debug.Log($"[ENERGY SHIELD] Giáp Hộ Thể đã hồi phục đầy đủ: {currentShield}/{maxShield}!");
    }

    /// <summary>
    /// Hấp thụ sát thương tới nhân vật. Trả về phần sát thương còn dư sau khi giáp đã đỡ.
    /// </summary>
    public int AbsorbDamage(int incomingDamage)
    {
        noDamageTimer = 0f; // Reset thời gian chờ hồi giáp

        if (currentLevel <= 0 || currentShield <= 0)
        {
            return incomingDamage;
        }

        if (currentShield >= incomingDamage)
        {
            currentShield -= incomingDamage;
            UpdateVisual();
            return 0; // Giáp đỡ toàn bộ sát thương!
        }
        else
        {
            int remainingDamage = incomingDamage - Mathf.RoundToInt(currentShield);
            currentShield = 0f;
            UpdateVisual(); // Giáp vỡ
            return remainingDamage;
        }
    }

    private void UpdateVisual()
    {
        if (shieldVisual != null)
        {
            shieldVisual.SetActive(currentShield > 0);
        }
    }
}
