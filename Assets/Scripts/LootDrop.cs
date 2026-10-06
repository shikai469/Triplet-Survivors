using UnityEngine;
using TMPro;

public enum LootEffect { Heal, Magnet, Reroll, Invincible }

public class LootDrop : MonoBehaviour
{
    [Header("UI Prefab")]
    public GameObject floatingTextPrefab;

    [Header("Effect Values")]
    public float healAmount = 20f;
    public float invincibleDuration = 3f;

    [Header("Magnet Physics")]
    public float initialSpeed = 3f;
    public float maxSpeed = 16f;
    public float acceleration = 14f;

    private Transform targetPlayer;
    private float currentSpeed;
    private bool isCollected = false;

    void Start()
    {
        currentSpeed = initialSpeed;
    }

    void Update()
    {
        if (targetPlayer != null && !isCollected)
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, maxSpeed, acceleration * Time.deltaTime);
            transform.position = Vector3.MoveTowards(transform.position, targetPlayer.position, currentSpeed * Time.deltaTime);

            float distanceToPlayer = Vector3.Distance(transform.position, targetPlayer.position);
            if (distanceToPlayer < 1.5f)
            {
                ApplyLootEffect();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !isCollected)
        {
            ApplyLootEffect();
        }
    }

    public void AttractToPlayer(Transform player)
    {
        if (!isCollected)
        {
            targetPlayer = player;
        }
    }

    // ĐÃ SỬA: Bỏ tham số player, gọi thẳng qua Instance để chống lỗi
    private void ApplyLootEffect()
    {
        isCollected = true;

        LootEffect effect = (LootEffect)Random.Range(0, 4);
        string message = "";

        switch (effect)
        {
            case LootEffect.Heal:
                if (PlayerHealth.Instance != null)
                {
                    PlayerHealth.Instance.Heal(healAmount);
                }
                message = "+HP";
                break;
                
            case LootEffect.Magnet:
                if (PlayerStats.Instance != null)
                {
                    TriggerMagnet(PlayerStats.Instance.transform);
                }
                message = "Nam châm!";
                break;
                
            case LootEffect.Reroll:
                UpgradeManager upgradeMgr = FindAnyObjectByType<UpgradeManager>();
                if (upgradeMgr != null && upgradeMgr.AddReroll(3))
                    message = "+1 Làm mới";
                else
                    message = "Làm mới (Max)";
                break;
                
            case LootEffect.Invincible:
                if (PlayerHealth.Instance != null)
                {
                    PlayerHealth.Instance.ActivateInvincibility(invincibleDuration);
                }
                message = "Bất tử!";
                break;
        }

        // ĐÃ SỬA: Lấy vị trí của Player để hiện Text thay vì vị trí của vật phẩm
        Vector3 playerPos = transform.position; // Vị trí dự phòng
        if (PlayerHealth.Instance != null)
        {
            playerPos = PlayerHealth.Instance.transform.position;
        }

        ShowFloatingText(playerPos, message);
        
        Destroy(gameObject); 
    }

    private void TriggerMagnet(Transform playerTransform)
    {
        GameObject[] xpDrops = GameObject.FindGameObjectsWithTag("XPCrystal");
        foreach (GameObject xp in xpDrops) xp.GetComponent<XPCrystal>().AttractToPlayer(playerTransform);

        GameObject[] goldDrops = GameObject.FindGameObjectsWithTag("GoldCoin");
        foreach (GameObject gold in goldDrops) gold.GetComponent<GoldCoin>().AttractToPlayer(playerTransform);
    }

    private void ShowFloatingText(Vector3 position, string text)
    {
        if (floatingTextPrefab != null)
        {
            GameObject floatText = Instantiate(floatingTextPrefab, position + new Vector3(0, 1f, 0), Quaternion.identity);
            TMP_Text tmpText = floatText.GetComponentInChildren<TMP_Text>();

            // Thêm lớp bảo vệ phòng trường hợp Prefab Text chưa setup chuẩn
            if (tmpText != null)
            {
                tmpText.text = text;
            }
        }
    }
}