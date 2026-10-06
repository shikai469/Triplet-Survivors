using UnityEngine;

public class PlayerXpPickup : MonoBehaviour
{
    [Header("Magnet Range")]
    public float baseMagnetRange = 3f; // Tầm hút gốc
    public LayerMask collectiblesLayer;

    void Update()
    {
        // Tính bán kính hút theo công thức: Base * (1 + Bonus)
        float currentRange = baseMagnetRange;
        if (PlayerStats.Instance != null)
        {
            currentRange = baseMagnetRange * (1f + PlayerStats.Instance.magnetRangeBonus);
        }

        // Quét tìm vật phẩm rơi trong bán kính nam châm
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, currentRange, collectiblesLayer);

        foreach (Collider2D col in hitColliders)
        {
            XPCrystal xp = col.GetComponent<XPCrystal>();
            if (xp != null) xp.AttractToPlayer(transform);

            GoldCoin gold = col.GetComponent<GoldCoin>();
            if (gold != null) gold.AttractToPlayer(transform);

            // ĐÃ SỬA: Gọi và hút LootDrop thay vì LootChest
            LootDrop drop = col.GetComponent<LootDrop>();
            if (drop != null) drop.AttractToPlayer(transform);
        }
    }

    void OnDrawGizmosSelected()
    {
        // Vẽ vòng tròn tầm hút trong Scene view để tiện căn chỉnh
        Gizmos.color = Color.yellow;
        float currentRange = baseMagnetRange;
        if (PlayerStats.Instance != null)
        {
            currentRange = baseMagnetRange * (1f + PlayerStats.Instance.magnetRangeBonus);
        }
        Gizmos.DrawWireSphere(transform.position, currentRange);
    }
}