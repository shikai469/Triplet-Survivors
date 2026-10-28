using System.Collections.Generic;
using UnityEngine;

public class XPCrystal : MonoBehaviour, ICollectible
{
    // Danh sách theo dõi toàn bộ ngọc EXP đang tồn tại trên bản đồ (Zero GC Alloc)
    public static readonly List<XPCrystal> ActiveCrystals = new List<XPCrystal>(128);

    [Header("XP Settings")]
    public int xpValue = 5;
    public float initialSpeed = 3f;
    public float maxSpeed = 16f;
    public float acceleration = 14f;

    private Transform targetPlayer;
    private float currentSpeed;
    private bool isCollected = false;

    void OnEnable()
    {
        ActiveCrystals.Add(this);
        // Reset trạng thái khi lấy từ pool
        targetPlayer = null;
        currentSpeed = initialSpeed;
        isCollected = false;
    }

    void OnDisable()
    {
        ActiveCrystals.Remove(this);
    }

    void Update()
    {
        if (targetPlayer != null)
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, maxSpeed, acceleration * Time.deltaTime);
            transform.position = Vector3.MoveTowards(transform.position, targetPlayer.position, currentSpeed * Time.deltaTime);
        }
    }

    public void AttractToPlayer(Transform player)
    {
        targetPlayer = player;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !isCollected)
        {
            isCollected = true;

            if (PlayerStats.Instance != null)
            {
                PlayerStats.Instance.AddXP(xpValue);
            }

            // Trả về pool thay vì Destroy
            if (ObjectPool.Instance != null)
                ObjectPool.Instance.Return(gameObject);
            else
                Destroy(gameObject);
        }
    }
}