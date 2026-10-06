using UnityEngine;

public class LootChest : MonoBehaviour
{
    [Header("Chest Settings")]
    public int hp = 1;
    public GameObject lootDropPrefab; // Kéo Prefab vật phẩm rớt vào đây
    public float destroyDelay = 2f;   // [THÊM MỚI] Thời gian rương tồn tại sau khi vỡ (2 giây)

    private bool isOpened = false;
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
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
        gameObject.tag = "Untagged"; // Ngừng nhận đạn từ súng

        // Tắt BoxCollider2D để người chơi không bị kẹt khi đi ngang qua rương đã vỡ
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null) col.enabled = false;

        // Kích hoạt animation mở rương
        if (animator != null)
        {
            animator.SetTrigger("Open");
        }

        // Sinh ra vật phẩm LootDrop tại vị trí của rương
        if (lootDropPrefab != null)
        {
            Instantiate(lootDropPrefab, transform.position, Quaternion.identity);
        }

        // [THÊM MỚI] Xóa rương khỏi bản đồ sau số giây đã cài đặt
        Destroy(gameObject, destroyDelay);
    }
}