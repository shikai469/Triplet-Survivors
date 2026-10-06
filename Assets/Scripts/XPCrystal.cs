using UnityEngine;

public class XPCrystal : MonoBehaviour
{
    [Header("XP Settings")]
    public int xpValue = 5;
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

            Destroy(gameObject);
        }
    }
}