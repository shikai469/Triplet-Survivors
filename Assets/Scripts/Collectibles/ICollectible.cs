using UnityEngine;

/// <summary>
/// Giao diện dùng chung cho tất cả các vật phẩm có thể bị người chơi hút (XP, Gold, Loot Drop).
/// Tối ưu hiệu năng: Giúp PlayerPickup chỉ cần gọi GetComponent một lần duy nhất thay vì quét từng loại riêng biệt.
/// </summary>
public interface ICollectible
{
    void AttractToPlayer(Transform player);
}
