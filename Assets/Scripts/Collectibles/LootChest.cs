using System.Collections.Generic;
using UnityEngine;

public class LootChest : MonoBehaviour
{
    // Danh sách theo dõi toàn bộ rương đang tồn tại và chưa mở trên sân (Zero GC Alloc)
    public static readonly List<LootChest> ActiveChests = new List<LootChest>();

    [Header("Chest Settings")]
    public int baseHp = 1;
    public int hp = 1;
    public GameObject lootDropPrefab; // Kéo Prefab vật phẩm rớt vào đây
    public float destroyDelay = 2f;   // Thời gian rương tồn tại sau khi vỡ (2 giây)

    private bool isOpened = false;
    private Animator animator;
    private BoxCollider2D boxCollider;
    private string originalTag = "Enemy";

    void Awake()
    {
        animator = GetComponent<Animator>();
        boxCollider = GetComponent<BoxCollider2D>();
        originalTag = gameObject.tag;
    }

    void OnEnable()
    {
        // Reset trạng thái khi lấy từ pool
        isOpened = false;
        hp = baseHp > 0 ? baseHp : 1;
        gameObject.tag = originalTag;

        if (boxCollider != null)
        {
            boxCollider.enabled = true;
        }

        if (!ActiveChests.Contains(this))
        {
            ActiveChests.Add(this);
        }
    }

    void OnDisable()
    {
        ActiveChests.Remove(this);
    }

    public void TakeDamage(int damage)
    {
        if (isOpened) return;

        hp -= damage;
        if (hp <= 0)
        {
            OpenChest();
        }
    }

    private void OpenChest()
    {
        isOpened = true;
        ActiveChests.Remove(this); // Ngừng nhắm bắn ngay khi rương mở
        gameObject.tag = "Untagged";

        // Tắt BoxCollider2D để người chơi không bị kẹt khi đi ngang qua rương đã vỡ
        if (boxCollider != null) boxCollider.enabled = false;

        // Kích hoạt animation mở rương
        if (animator != null)
        {
            animator.SetTrigger("Open");
        }

        // Sinh ra vật phẩm LootDrop tại vị trí của rương (từ Object Pool)
        if (lootDropPrefab != null)
        {
            if (ObjectPool.Instance != null)
                ObjectPool.Instance.Get(lootDropPrefab, transform.position, Quaternion.identity);
            else
                Instantiate(lootDropPrefab, transform.position, Quaternion.identity);
        }

        // Trả rương về Object Pool sau thời gian delay
        if (ObjectPool.Instance != null)
            ObjectPool.Instance.Return(gameObject, destroyDelay);
        else
            Destroy(gameObject, destroyDelay);
    }
}