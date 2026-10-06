using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Stats")]
    public float baseMaxHealth = 30f;
    public float maxHealth;
    public float currentHealth;
    public int armor = 0;

    void Awake()
    {
        maxHealth = baseMaxHealth;
        currentHealth = maxHealth;
    }

    // Hàm nhận buff theo wave/thời gian từ Spawner
    public void InitStats(float hpMultiplier, int extraArmor)
    {
        maxHealth = baseMaxHealth * hpMultiplier;
        currentHealth = maxHealth;
        armor = extraArmor;
    }

    public void TakeDamage(int incomingDamage, bool isCrit)
    {
        int actualDamage = Mathf.Max(1, incomingDamage - armor);
        currentHealth -= actualDamage;

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

        LootDropSystem loot = GetComponent<LootDropSystem>();
        if (loot != null)
        {
            loot.DropLoot();
        }

        Destroy(gameObject);
    }
}