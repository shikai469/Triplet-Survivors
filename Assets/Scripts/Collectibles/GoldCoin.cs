using System.Collections.Generic;
using UnityEngine;

public class GoldCoin : MonoBehaviour, ICollectible
{
    // Danh sách theo dõi toàn bộ tiền vàng đang tồn tại trên bản đồ (Zero GC Alloc)
    public static readonly List<GoldCoin> ActiveCoins = new List<GoldCoin>(128);

    [Header("Coin Value Range")]
    public int minCoinValue = 4;       // Tối thiểu 4 vàng
    public int maxCoinValue = 8;       // Tối đa 8 vàng

    [Header("Magnet Physics")]
    public float initialSpeed = 2f;
    public float maxSpeed = 15f;
    public float acceleration = 12f;

    private Transform targetPlayer;
    private float currentSpeed;
    private bool isCollected = false;

    void OnEnable()
    {
        ActiveCoins.Add(this);
        // Reset trạng thái khi lấy từ pool
        targetPlayer = null;
        currentSpeed = initialSpeed;
        isCollected = false;
    }

    void OnDisable()
    {
        ActiveCoins.Remove(this);
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
                // Roll giá trị ngẫu nhiên trong khoảng đã đặt
                int droppedGold = Random.Range(minCoinValue, maxCoinValue + 1);
                PlayerStats.Instance.AddGold(droppedGold);
            }

            // Trả về pool thay vì Destroy
            if (ObjectPool.Instance != null)
                ObjectPool.Instance.Return(gameObject);
            else
                Destroy(gameObject);
        }
    }
}