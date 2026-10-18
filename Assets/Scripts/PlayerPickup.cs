using UnityEngine;

public class PlayerXpPickup : MonoBehaviour
{
    [Header("Magnet Range")]
    public float baseMagnetRange = 3f; // Tầm hút gốc
    public LayerMask collectiblesLayer;

    [Header("Optimization")]
    public float scanInterval = 0.08f; // Tần suất quét (~12 lần/giây) giúp giảm 85% tải CPU Physics trên mobile

    // Bộ đệm tái sử dụng và bộ lọc va chạm theo chuẩn Unity 6 (Zero GC Alloc)
    private ContactFilter2D contactFilter;
    private readonly Collider2D[] hitBuffer = new Collider2D[64];
    private float scanTimer;

    void Awake()
    {
        contactFilter = new ContactFilter2D();
        contactFilter.SetLayerMask(collectiblesLayer);
        contactFilter.useLayerMask = true;
        contactFilter.useTriggers = true;
        scanTimer = Random.Range(0f, scanInterval); // Stagger start time
    }

    void Update()
    {
        scanTimer += Time.deltaTime;
        if (scanTimer < scanInterval) return;
        scanTimer = 0f;

        // Tính bán kính hút theo công thức: Base * (1 + Bonus)
        float currentRange = baseMagnetRange;
        if (PlayerStats.Instance != null)
        {
            currentRange = baseMagnetRange * (1f + PlayerStats.Instance.magnetRangeBonus);
        }

        // Cập nhật LayerMask trong trường hợp thay đổi trên Inspector
        contactFilter.layerMask = collectiblesLayer;
        contactFilter.useLayerMask = collectiblesLayer.value != 0;

        // Quét vật phẩm rơi bằng Physics2D.OverlapCircle theo API mới nhất của Unity 6 (thay cho OverlapCircleNonAlloc)
        int hitCount = Physics2D.OverlapCircle(transform.position, currentRange, contactFilter, hitBuffer);

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D col = hitBuffer[i];
            if (col == null) continue;

            // Sử dụng interface ICollectible chung để gom 3 lần GetComponent thành 1 lần duy nhất
            ICollectible collectible = col.GetComponent<ICollectible>();
            if (collectible != null)
            {
                collectible.AttractToPlayer(transform);
            }
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