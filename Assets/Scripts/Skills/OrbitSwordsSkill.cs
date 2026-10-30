using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Kỹ năng 4: Vung Gậy (Orbit Bat) [Max Lv 3]
/// Cơ chế: Tạo gậy bóng chày xoay tròn quanh nhân vật người chơi, gây sát thương cho quái chạm vào.
/// Tiến trình: Lên cấp tăng số lượng gậy. Đạt tối đa Lv 3 sẽ có 3 gậy bay quanh người.
/// </summary>
public class OrbitSwordsSkill : MonoBehaviour
{
    [Header("Skill Level")]
    public int currentLevel = 0;
    public const int MaxLevel = 3;

    [Header("Orbit Settings")]
    public float orbitRadius = 1.6f;
    public float rotationSpeed = 220f; // Độ mỗi giây
    public float baseDamage = 25f;
    public Sprite customSwordSprite;

    private float currentAngle = 0f;
    private readonly List<GameObject> activeSwords = new List<GameObject>();
    private GameObject swordsContainer;

    public void SetLevel(int level)
    {
        currentLevel = Mathf.Clamp(level, 0, MaxLevel);
        enabled = (currentLevel > 0);
        RebuildSwords();
    }

    void OnDisable()
    {
        ClearSwords();
    }

    void OnDestroy()
    {
        ClearSwords();
    }

    private void ClearSwords()
    {
        for (int i = 0; i < activeSwords.Count; i++)
        {
            if (activeSwords[i] != null)
            {
                Destroy(activeSwords[i]);
            }
        }
        activeSwords.Clear();

        if (swordsContainer != null)
        {
            Destroy(swordsContainer);
            swordsContainer = null;
        }
    }

    private void RebuildSwords()
    {
        ClearSwords();
        if (currentLevel <= 0) return;

        // Tạo container riêng biệt độc lập, KHÔNG gán làm con của Player
        // Tránh hoàn toàn xung đột vật lý Nested Rigidbody2D (khiến nhân vật bị kéo phóng tốc độ cao)
        if (swordsContainer == null)
        {
            swordsContainer = new GameObject("OrbitSwords_Container");
        }

        Collider2D playerCollider = GetComponent<Collider2D>();
        int swordCount = currentLevel; // Lv 1: 1 gậy, Lv 2: 2 gậy, Lv 3: 3 gậy

        for (int i = 0; i < swordCount; i++)
        {
            GameObject sword = new GameObject($"OrbitBat_{i}");
            sword.transform.SetParent(swordsContainer.transform);

            // Sprite gậy
            SpriteRenderer sr = sword.AddComponent<SpriteRenderer>();
            sr.sprite = customSwordSprite != null ? customSwordSprite : ProceduralSkillAssets.GetSprite("SwordBlade");
            sr.sortingOrder = 12;
            sword.transform.localScale = new Vector3(1.2f, 1.2f, 1f);

            // Trigger Collider
            BoxCollider2D box = sword.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(0.4f, 1.2f);

            // Bỏ qua va chạm tuyệt đối với Player
            if (playerCollider != null)
            {
                Physics2D.IgnoreCollision(box, playerCollider, true);
            }

            Rigidbody2D rb = sword.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0;

            OrbitBladeLogic blade = sword.AddComponent<OrbitBladeLogic>();
            // Tỷ lệ sát thương theo cấp: Lv 1: 45% + 2, Lv 2: 60% + 4, Lv 3: 75% + 6
            blade.damageRatio = 0.45f + (currentLevel - 1) * 0.15f;
            blade.flatBonus = 2f + (currentLevel - 1) * 2f;

            activeSwords.Add(sword);
        }

        UpdateSwordPositions();
    }

    void LateUpdate()
    {
        if (currentLevel <= 0 || activeSwords.Count == 0) return;

        currentAngle += rotationSpeed * Time.deltaTime;
        if (currentAngle >= 360f) currentAngle -= 360f;

        UpdateSwordPositions();
    }

    private void UpdateSwordPositions()
    {
        int count = activeSwords.Count;
        if (count == 0) return;

        float angleStep = 360f / count;
        Vector3 playerPos = transform.position;

        for (int i = 0; i < count; i++)
        {
            if (activeSwords[i] == null) continue;

            float angleRad = (currentAngle + i * angleStep) * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad), 0f) * orbitRadius;
            activeSwords[i].transform.position = playerPos + offset;

            // Xoay gậy hướng theo vòng xoay
            float rotAngle = (currentAngle + i * angleStep) - 90f;
            activeSwords[i].transform.rotation = Quaternion.Euler(0, 0, rotAngle);
        }
    }
}

/// <summary>
/// Gây sát thương định kỳ cho quái chạm vào lưỡi kiếm xoay quanh người chơi
/// </summary>
public class OrbitBladeLogic : MonoBehaviour
{
    public float damageRatio = 0.45f;
    public float flatBonus = 2f;
    private readonly Dictionary<Collider2D, float> lastHitTimes = new Dictionary<Collider2D, float>();
    private const float HitCooldown = 0.35f; // Thời gian giãn cách giữa 2 lần chém cùng 1 quái

    void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            float now = Time.time;

            if (lastHitTimes.TryGetValue(other, out float lastHit) && (now - lastHit) < HitCooldown)
            {
                return;
            }

            lastHitTimes[other] = now;

            // Sát thương scale trực tiếp từ tổng sát thương nhân vật
            float totalPlayerDmg = PlayerStats.Instance != null ? PlayerStats.Instance.GetTotalAttackDamage() : 12f;
            float finalDmg = (totalPlayerDmg * damageRatio) + flatBonus;

            EnemyHealth enemy = other.GetComponent<EnemyHealth>();
            if (enemy != null)
            {
                Vector2 pushDir = (other.transform.position - transform.position).normalized;
                enemy.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(finalDmg)), false, pushDir);
            }

            LootChest chest = other.GetComponent<LootChest>();
            if (chest != null)
            {
                chest.TakeDamage(1);
            }
        }
    }
}
