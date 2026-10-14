using System.Collections.Generic;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    // Danh sách tĩnh theo dõi toàn bộ quái vật đang sống trong scene (Zero GC Alloc)
    public static readonly List<EnemyHealth> ActiveEnemies = new List<EnemyHealth>(128);

    [Header("Stats")]
    public float baseMaxHealth = 30f;
    public float maxHealth;
    public float currentHealth;
    public int armor = 0;
    public float knockbackResistance = 50f; // Theo tài liệu: 50 (dễ lùi), 35 (trung bình), 0 (miễn nhiễm)
    public bool isBoss = false;             // Đánh dấu thực thể là Boss

    void OnEnable()
    {
        ActiveEnemies.Add(this);
    }

    void OnDisable()
    {
        ActiveEnemies.Remove(this);
    }

    void Awake()
    {
        maxHealth = baseMaxHealth;
        currentHealth = maxHealth;
    }

    // Hàm nhận buff theo wave / thời gian và độ khó từ Spawner
    public void InitStats(float hpMultiplier, int extraArmor)
    {
        InitStats(hpMultiplier, extraArmor, 50f, false);
    }

    public void InitStats(float hpMultiplier, int extraArmor, float knockbackRes = 50f, bool boss = false)
    {
        maxHealth = baseMaxHealth * hpMultiplier;
        currentHealth = maxHealth;
        armor = extraArmor;
        knockbackResistance = knockbackRes;
        isBoss = boss;
    }

    public void TakeDamage(int incomingDamage, bool isCrit, Vector2 hitDirection = default)
    {
        // Công thức tính giáp theo Section 3.2: Damage_Thực_Nhận = Max(1, Damage_Đầu_Vào - Armor)
        int actualDamage = Mathf.Max(1, incomingDamage - armor);
        currentHealth -= actualDamage;

        // Áp dụng lực đẩy lùi (Knockback) nếu quái không miễn nhiễm (Section 3.4)
        if (hitDirection != default && knockbackResistance > 0f)
        {
            EnemyMovement movement = GetComponent<EnemyMovement>();
            if (movement != null)
            {
                movement.ApplyKnockback(hitDirection, knockbackResistance);
            }
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.AddKill();
        }

        // Báo hiệu cho GameStageManager nếu đây là Boss
        if (isBoss && GameStageManager.Instance != null)
        {
            GameStageManager.Instance.OnBossDefeated(this);
        }

        LootDropSystem loot = GetComponent<LootDropSystem>();
        if (loot != null)
        {
            loot.DropLoot();
        }

        Destroy(gameObject);
    }
}