using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Đấu trường Vòng Tròn Lửa (Ring of Fire Arena).
/// Xuất hiện khi Boss xuất hiện, giam giữ chặt Người chơi và Boss bên trong vòng tròn lửa cháy,
/// không cho bất kỳ ai thoát ra cho đến khi Boss bị tiêu diệt.
/// </summary>
public class FireRingArena : MonoBehaviour
{
    public static FireRingArena Instance { get; private set; }

    [Header("Arena Dimensions")]
    [Tooltip("Bán kính vòng tròn lửa")]
    public float radius = 8.5f;
    public float playerPadding = 0.6f;
    public float bossPadding = 1.0f;

    [Header("Visual Settings")]
    public int segments = 64;
    public float lineWidth = 0.35f;
    public Color flameColorInner = new Color(1f, 0.9f, 0.2f, 1f);  // Vàng lửa rực
    public Color flameColorOuter = new Color(1f, 0.25f, 0.05f, 1f); // Cam đỏ

    private Vector3 center;
    private Transform playerTransform;
    private Transform bossTransform;
    private LineRenderer lineRenderer;
    private EdgeCollider2D edgeCollider;
    private ParticleSystem flameParticles;
    private bool isActive = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    /// <summary>
    /// Kích hoạt Đấu trường Vòng Tròn Lửa bao quanh Người chơi và Boss.
    /// </summary>
    public void ActivateArena(Vector3 arenaCenter, Transform player, Transform boss, float arenaRadius = 8.5f)
    {
        center = arenaCenter;
        center.z = 0f;
        transform.position = center;
        radius = arenaRadius;
        playerTransform = player;
        bossTransform = boss;

        SetupVisuals();
        SetupCollider();
        SetupParticles();

        isActive = true;
        gameObject.SetActive(true);

        Debug.Log($"<color=red>[FIRE RING ARENA] VÒNG TRÒN LỬA ĐÃ ĐƯỢC THIẾT LẬP! Bán kính: {radius}m tại {center}</color>");
    }

    /// <summary>
    /// Tạo LineRenderer vòng tròn lửa bập bùng bằng code procedural không phụ thuộc material ngoài
    /// </summary>
    private void SetupVisuals()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null) lineRenderer = gameObject.AddComponent<LineRenderer>();

        lineRenderer.useWorldSpace = true;
        lineRenderer.loop = true;
        lineRenderer.positionCount = segments;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;

        // Tìm Shader phù hợp của URP hoặc Sprites
        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Color");

        Material mat = new Material(shader);
        mat.color = flameColorOuter;
        lineRenderer.material = mat;
        lineRenderer.sortingOrder = 14; // Nằm trên sàn, dưới nhân vật

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(flameColorInner, 0.0f),
                new GradientColorKey(flameColorOuter, 0.5f),
                new GradientColorKey(flameColorInner, 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.95f, 0.0f),
                new GradientAlphaKey(1.0f, 0.5f),
                new GradientAlphaKey(0.95f, 1.0f)
            }
        );
        lineRenderer.colorGradient = gradient;
    }

    /// <summary>
    /// Tạo EdgeCollider2D vòng tròn khép kín ngăn cản đạn và vật lý
    /// </summary>
    private void SetupCollider()
    {
        edgeCollider = GetComponent<EdgeCollider2D>();
        if (edgeCollider == null) edgeCollider = gameObject.AddComponent<EdgeCollider2D>();

        Vector2[] points = new Vector2[segments + 1];
        float angleStep = 360f / segments;

        for (int i = 0; i <= segments; i++)
        {
            float rad = (i * angleStep) * Mathf.Deg2Rad;
            points[i] = new Vector2(Mathf.Cos(rad) * radius, Mathf.Sin(rad) * radius);
        }

        edgeCollider.points = points;
    }

    /// <summary>
    /// Sinh hệ thống hạt Particle lửa bốc cháy dọc theo chu vi vòng tròn
    /// </summary>
    private void SetupParticles()
    {
        flameParticles = GetComponent<ParticleSystem>();
        if (flameParticles == null) flameParticles = gameObject.AddComponent<ParticleSystem>();

        var main = flameParticles.main;
        main.loop = true;
        main.startLifetime = 0.6f;
        main.startSpeed = 1.2f;
        main.startSize = 0.55f;
        main.maxParticles = 300;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = flameParticles.emission;
        emission.rateOverTime = 120f;

        var shape = flameParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = radius;
        shape.radiusThickness = 0.1f; // Chỉ sinh hạt ở đúng mép viền vòng tròn

        var colorOverLifetime = flameParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient colorGrad = new Gradient();
        colorGrad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 0.95f, 0.3f), 0f),
                new GradientColorKey(new Color(1f, 0.3f, 0.05f), 0.6f),
                new GradientColorKey(new Color(0.3f, 0.05f, 0f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.8f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = colorGrad;

        var sizeOverLifetime = flameParticles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.3f);
        sizeCurve.AddKey(0.4f, 1f);
        sizeCurve.AddKey(1f, 0.1f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        ParticleSystemRenderer renderer = GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.sortingOrder = 16;
            Shader s = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default");
            renderer.material = new Material(s);
        }

        flameParticles.Play();
    }

    void Update()
    {
        if (!isActive) return;

        // Cập nhật hiệu ứng ngọn lửa bập bùng sóng động
        if (lineRenderer != null)
        {
            float angleStep = 360f / segments;
            for (int i = 0; i < segments; i++)
            {
                float angle = i * angleStep;
                float rad = angle * Mathf.Deg2Rad;
                // Hiệu ứng ngọn lửa dao động bập bùng
                float flicker = Mathf.Sin(Time.time * 9f + i * 0.7f) * 0.18f;
                float currentR = radius + flicker;
                Vector3 pos = center + new Vector3(Mathf.Cos(rad) * currentR, Mathf.Sin(rad) * currentR, 0f);
                lineRenderer.SetPosition(i, pos);
            }
        }
    }

    void LateUpdate()
    {
        if (!isActive) return;

        // 1. Giam giữ chặt Người chơi bên trong vòng tròn lửa (Zero Glitch / Tunneling)
        if (playerTransform != null)
        {
            Vector2 offset = (Vector2)playerTransform.position - (Vector2)center;
            float dist = offset.magnitude;
            float maxPlayerDist = radius - playerPadding;

            if (dist > maxPlayerDist)
            {
                playerTransform.position = (Vector2)center + offset.normalized * maxPlayerDist;

                Rigidbody2D rb = playerTransform.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    float outVelocity = Vector2.Dot(rb.linearVelocity, offset.normalized);
                    if (outVelocity > 0f)
                    {
                        rb.linearVelocity -= offset.normalized * outVelocity;
                    }
                }
            }
        }

        // 2. Giam giữ chặt Boss bên trong vòng tròn lửa
        if (bossTransform != null)
        {
            Vector2 bossOffset = (Vector2)bossTransform.position - (Vector2)center;
            float bossDist = bossOffset.magnitude;
            float maxBossDist = radius - bossPadding;

            if (bossDist > maxBossDist)
            {
                bossTransform.position = (Vector2)center + bossOffset.normalized * maxBossDist;

                Rigidbody2D bossRb = bossTransform.GetComponent<Rigidbody2D>();
                if (bossRb != null)
                {
                    float outVelocity = Vector2.Dot(bossRb.linearVelocity, bossOffset.normalized);
                    if (outVelocity > 0f)
                    {
                        bossRb.linearVelocity -= bossOffset.normalized * outVelocity;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Tắt vòng tròn lửa khi Boss bị tiêu diệt
    /// </summary>
    public void DeactivateArena()
    {
        isActive = false;
        if (flameParticles != null) flameParticles.Stop();
        if (lineRenderer != null) lineRenderer.enabled = false;
        if (edgeCollider != null) edgeCollider.enabled = false;

        StartCoroutine(FadeOutAndDestroy());
    }

    private IEnumerator FadeOutAndDestroy()
    {
        yield return new WaitForSeconds(1.5f);
        Destroy(gameObject);
    }
}

