using UnityEngine;
using TMPro;

public enum LootEffect { Heal, Magnet, Reroll, Invincible }

public class LootDrop : MonoBehaviour, ICollectible
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

    void OnEnable()
    {
        currentSpeed = initialSpeed;
        targetPlayer = null;
        isCollected = false;
    }

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

            float distanceSqr = (transform.position - targetPlayer.position).sqrMagnitude;
            if (distanceSqr < 2.25f) // 1.5 * 1.5 = 2.25f
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

    // Bỏ tham số player, gọi thẳng qua Instance để chống lỗi
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
                UpgradeManager upgradeMgr = UpgradeManager.Instance;
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

        // Lấy vị trí của Player để hiện Text thay vì vị trí của vật phẩm
        Vector3 playerPos = transform.position; // Vị trí dự phòng
        if (PlayerHealth.Instance != null)
        {
            playerPos = PlayerHealth.Instance.transform.position;
        }

        ShowFloatingText(playerPos, message);
        
        // Trả về Object Pool thay vì Destroy
        if (ObjectPool.Instance != null)
            ObjectPool.Instance.Return(gameObject);
        else
            Destroy(gameObject); 
    }

    private void TriggerMagnet(Transform playerTransform)
    {
        int xpCount = XPCrystal.ActiveCrystals.Count;
        for (int i = 0; i < xpCount; i++)
        {
            XPCrystal xp = XPCrystal.ActiveCrystals[i];
            if (xp != null) xp.AttractToPlayer(playerTransform);
        }

        int goldCount = GoldCoin.ActiveCoins.Count;
        for (int i = 0; i < goldCount; i++)
        {
            GoldCoin gold = GoldCoin.ActiveCoins[i];
            if (gold != null) gold.AttractToPlayer(playerTransform);
        }
    }

    private void ShowFloatingText(Vector3 position, string text)
    {
        if (floatingTextPrefab != null)
        {
            Vector3 spawnPos = position + new Vector3(0, 1f, 0);
            GameObject floatText = ObjectPool.Instance != null
                ? ObjectPool.Instance.Get(floatingTextPrefab, spawnPos, Quaternion.identity)
                : Instantiate(floatingTextPrefab, spawnPos, Quaternion.identity);

            TMP_Text tmpText = floatText.GetComponentInChildren<TMP_Text>();
            if (tmpText != null)
            {
                tmpText.text = text;
            }
        }
    }
}