using UnityEngine;

/// <summary>
/// Hiển thị chữ nổi (sát thương, nhặt vật phẩm) trôi dần lên trên.
/// Hỗ trợ Object Pool để không gây GC lag khi quái và hiệu ứng nổ ra liên tục.
/// </summary>
public class FloatingText : MonoBehaviour
{
    public float moveSpeed = 1f;
    public float destroyTime = 1.5f;

    private float timer;

    void OnEnable()
    {
        timer = 0f;
    }

    void Update()
    {
        transform.position += Vector3.up * (moveSpeed * Time.deltaTime);

        timer += Time.deltaTime;
        if (timer >= destroyTime)
        {
            if (ObjectPool.Instance != null)
                ObjectPool.Instance.Return(gameObject);
            else
                Destroy(gameObject);
        }
    }
}