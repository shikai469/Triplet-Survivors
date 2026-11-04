using System.Collections;
using UnityEngine;

/// <summary>
/// Các thực thể kỹ năng (Projectiles, Traps, AoE Strikes, Waves) của Boss Mèo Chuối.
/// </summary>

// =========================================================================
// 1. ĐẠN GIỌT NƯỚC MẮT (TEAR PROJECTILE)
// =========================================================================
public class BossTearProjectile : MonoBehaviour
{
    private Vector2 moveDir;
    private float moveSpeed = 5.0f;
    private int damage = 14;
    private float lifeTimer = 4.0f;
    private SpriteRenderer sr;
    private bool hasHit = false;

    public void Init(Vector2 dir, float speed, int dmg)
    {
        moveDir = dir.normalized;
        moveSpeed = speed;
        damage = dmg;
        hasHit = false;
        lifeTimer = 4.0f;

        // Xoay đầu nhọn của giọt nước theo hướng bay
        float angle = Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        sr = GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = BossMeoChuoiAssets.GetTearSprite();
            sr.sortingOrder = 16;
        }

        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col == null)
        {
            col = gameObject.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.2f;
        }
    }

    void Update()
    {
        if (hasHit) return;

        transform.position += (Vector3)(moveDir * moveSpeed * Time.deltaTime);
        lifeTimer -= Time.deltaTime;

        // Tự biến mất khi ra ngoài đấu trường hoặc hết thời gian
        if (lifeTimer <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        if (FireRingArena.Instance != null)
        {
            Vector2 offset = (Vector2)transform.position - (Vector2)FireRingArena.Instance.transform.position;
            if (offset.magnitude > FireRingArena.Instance.radius)
            {
                Destroy(gameObject);
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;

        if (other.CompareTag("Player"))
        {
            hasHit = true;
            PlayerHealth ph = other.GetComponent<PlayerHealth>();
            if (ph != null) ph.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}

// =========================================================================
// 2. BẪY VỎ CHUỐI (BANANA PEEL TRAP)
// =========================================================================
public class BossBananaPeelTrap : MonoBehaviour
{
    private float lifeTimer = 10.0f;
    private bool isArmed = false;
    private SpriteRenderer sr;

    public void SetupToss(Vector3 targetPos, float duration = 0.6f)
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = BossMeoChuoiAssets.GetBananaPeelSprite();
            sr.sortingOrder = 12; // Dưới chân người chơi một chút
        }

        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col == null)
        {
            col = gameObject.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.35f;
        }

        StartCoroutine(TossRoutine(targetPos, duration));
    }

    private IEnumerator TossRoutine(Vector3 targetPos, float duration)
    {
        Vector3 startPos = transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Quỹ đạo hình vòng cung (Parabola)
            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);
            float arcHeight = Mathf.Sin(t * Mathf.PI) * 1.8f;
            currentPos.y += arcHeight;

            transform.position = currentPos;
            transform.Rotate(0, 0, 720f * Time.deltaTime);
            yield return null;
        }

        transform.position = targetPos;
        transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
        isArmed = true;
    }

    void Update()
    {
        if (!isArmed) return;

        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 2f && sr != null)
        {
            // Nhấp nháy trước khi biến mất
            float alpha = (Mathf.Sin(Time.time * 12f) + 1f) * 0.5f;
            Color c = sr.color;
            c.a = Mathf.Lerp(0.2f, 1f, alpha);
            sr.color = c;
        }

        if (lifeTimer <= 0f)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!isArmed) return;

        if (other.CompareTag("Player"))
        {
            PlayerMovement pm = other.GetComponent<PlayerMovement>();
            if (pm != null)
            {
                // Hướng trượt theo quán tính vận tốc hiện tại của Player
                Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
                Vector2 slipDir = rb != null && rb.linearVelocity.sqrMagnitude > 0.1f 
                    ? rb.linearVelocity.normalized 
                    : (Vector2)(other.transform.position - transform.position).normalized;

                pm.ApplySlip(slipDir, force: 7.2f, duration: 0.7f);
                pm.ApplySlow(multiplier: 0.5f, duration: 1.5f);
            }

            // Hiệu ứng giẫm nát vỏ chuối
            Destroy(gameObject);
        }
    }
}

// =========================================================================
// 3. MƯA CHUỐI RƠI TỪ TRÊN TRỜI (BANANA ORBITAL STRIKE)
// =========================================================================
public class BossBananaBombStrike : MonoBehaviour
{
    private Vector3 targetPosition;
    private float warningDuration = 1.2f;
    private int damage = 22;
    private float aoeRadius = 1.4f;

    public void LaunchStrike(Vector3 targetPos, float warningTime = 1.2f, int dmg = 22)
    {
        targetPosition = targetPos;
        targetPosition.z = 0f;
        transform.position = targetPosition;
        warningDuration = warningTime;
        damage = dmg;

        StartCoroutine(StrikeRoutine());
    }

    private IEnumerator StrikeRoutine()
    {
        // 1. Tạo vòng tròn cảnh báo đỏ co giãn trên mặt đất
        GameObject ringObj = new GameObject("WarningCircle");
        ringObj.transform.parent = transform;
        ringObj.transform.position = targetPosition;
        SpriteRenderer ringSr = ringObj.AddComponent<SpriteRenderer>();
        ringSr.sprite = BossMeoChuoiAssets.GetWarningCircleSprite();
        ringSr.sortingOrder = 11;
        ringObj.transform.localScale = Vector3.zero;

        float elapsed = 0f;
        while (elapsed < warningDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / warningDuration);

            // Vòng tròn nở to dần đến bán kính aoe
            float targetScale = aoeRadius * 2f;
            ringObj.transform.localScale = Vector3.one * Mathf.Lerp(0.3f, targetScale, t);

            // Nhấp nháy màu đỏ tăng dần tần số cảnh báo nguy hiểm
            float flash = Mathf.Sin(t * 18f) * 0.3f + 0.7f;
            ringSr.color = new Color(1f, 0.2f, 0.2f, flash);

            yield return null;
        }

        Destroy(ringObj);

        // 2. Quả chuối khổng lồ từ trên trời cắm thẳng xuống mặt đất
        GameObject bananaObj = new GameObject("FallingBanana");
        bananaObj.transform.position = targetPosition + new Vector3(0f, 9.0f, 0f);
        SpriteRenderer bSr = bananaObj.AddComponent<SpriteRenderer>();
        bSr.sprite = BossMeoChuoiAssets.GetBananaMissileSprite();
        bSr.sortingOrder = 18;
        bananaObj.transform.localScale = Vector3.one * 1.5f;

        float dropTime = 0.22f;
        float dropElapsed = 0f;
        Vector3 dropStart = bananaObj.transform.position;

        while (dropElapsed < dropTime)
        {
            dropElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(dropElapsed / dropTime);
            // Rơi gia tốc cực nhanh
            float easeIn = t * t;
            bananaObj.transform.position = Vector3.Lerp(dropStart, targetPosition, easeIn);
            yield return null;
        }

        Destroy(bananaObj);

        // 3. Vụ nổ mặt đất khi chuối chạm đất
        DoExplosion();

        yield return new WaitForSeconds(0.4f);
        Destroy(gameObject);
    }

    private void DoExplosion()
    {
        // Kiểm tra gây sát thương cho người chơi
        Collider2D[] hits = Physics2D.OverlapCircleAll(targetPosition, aoeRadius);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                PlayerHealth ph = hit.GetComponent<PlayerHealth>();
                if (ph != null) ph.TakeDamage(damage);

                PlayerMovement pm = hit.GetComponent<PlayerMovement>();
                if (pm != null)
                {
                    Vector2 pushDir = (hit.transform.position - targetPosition).normalized;
                    pm.ApplyKnockback(pushDir, force: 5.5f, duration: 0.2f);
                }
            }
        }

        // Tạo hiệu ứng sóng xung kích nổ màu vàng đất
        GameObject blastObj = new GameObject("BananaBlast");
        blastObj.transform.position = targetPosition;
        LineRenderer lr = blastObj.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop = true;
        lr.positionCount = 32;
        lr.startWidth = 0.25f;
        lr.endWidth = 0.25f;

        Shader s = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (s == null) s = Shader.Find("Sprites/Default");
        Material mat = new Material(s);
        mat.color = new Color(1f, 0.9f, 0.1f, 1f);
        lr.material = mat;
        lr.sortingOrder = 17;

        for (int i = 0; i < 32; i++)
        {
            float rad = (i / 32f) * Mathf.PI * 2f;
            Vector3 pt = targetPosition + new Vector3(Mathf.Cos(rad) * aoeRadius, Mathf.Sin(rad) * aoeRadius, 0f);
            lr.SetPosition(i, pt);
        }

        Destroy(blastObj, 0.25f);
    }
}

// =========================================================================
// 4. TIẾNG KHÓC SIÊU THANH (ULTRASONIC WHINE SHOCKWAVE - CHIÊU NỘ PHASE 3)
// =========================================================================
public class BossUltrasonicWave : MonoBehaviour
{
    private Vector3 center;
    private float maxRadius = 9.0f;
    private float expandSpeed = 6.5f;
    private int damage = 16;
    private float currentRadius = 0.5f;
    private LineRenderer lr;
    private bool hasHitPlayer = false;

    public void InitWave(Vector3 startCenter, float targetMaxRadius, float speed, int dmg)
    {
        center = startCenter;
        center.z = 0f;
        transform.position = center;
        maxRadius = targetMaxRadius;
        expandSpeed = speed;
        damage = dmg;
        currentRadius = 0.6f;
        hasHitPlayer = false;

        lr = gameObject.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop = true;
        lr.positionCount = 48;
        lr.startWidth = 0.35f;
        lr.endWidth = 0.35f;

        Shader s = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (s == null) s = Shader.Find("Sprites/Default");
        Material mat = new Material(s);
        mat.color = new Color(0.25f, 0.85f, 1f, 0.9f); // Xanh biển sóng âm
        lr.material = mat;
        lr.sortingOrder = 16;
    }

    void Update()
    {
        currentRadius += expandSpeed * Time.deltaTime;

        // Cập nhật vị trí vòng sóng âm
        float angleStep = (Mathf.PI * 2f) / 48f;
        for (int i = 0; i < 48; i++)
        {
            float rad = i * angleStep;
            float wobble = Mathf.Sin(Time.time * 20f + i * 0.8f) * 0.12f;
            float r = currentRadius + wobble;
            Vector3 pt = center + new Vector3(Mathf.Cos(rad) * r, Mathf.Sin(rad) * r, 0f);
            lr.SetPosition(i, pt);
        }

        // Kiểm tra va chạm với người chơi
        if (!hasHitPlayer && PlayerStats.Instance != null)
        {
            Transform pT = PlayerStats.Instance.transform;
            float dist = Vector2.Distance(center, pT.position);
            // Nếu người chơi chạm vào vành sóng âm
            if (Mathf.Abs(dist - currentRadius) <= 0.65f)
            {
                hasHitPlayer = true;
                PlayerHealth ph = pT.GetComponent<PlayerHealth>();
                if (ph != null) ph.TakeDamage(damage);

                PlayerMovement pm = pT.GetComponent<PlayerMovement>();
                if (pm != null)
                {
                    Vector2 pushDir = ((Vector2)pT.position - (Vector2)center).normalized;
                    pm.ApplyKnockback(pushDir, force: 7.5f, duration: 0.3f);
                }
            }
        }

        // Biến mất khi đạt đến bán kính tối đa
        if (currentRadius >= maxRadius)
        {
            Destroy(gameObject);
        }
    }
}

// =========================================================================
// 5. HIỆU ỨNG NGÔI SAO CHOÁNG (DIZZY STARS KHI ĐÂM TƯỜNG LỬA)
// =========================================================================
public class BossDizzyStarsEffect : MonoBehaviour
{
    private Transform[] stars;
    private float orbitRadius = 0.85f;
    private float orbitSpeed = 280f;
    private float currentAngle = 0f;

    void Awake()
    {
        stars = new Transform[3];
        Sprite starSp = BossMeoChuoiAssets.GetDizzyStarSprite();

        for (int i = 0; i < 3; i++)
        {
            GameObject sObj = new GameObject($"Star_{i}");
            sObj.transform.parent = transform;
            SpriteRenderer sr = sObj.AddComponent<SpriteRenderer>();
            sr.sprite = starSp;
            sr.sortingOrder = 22;
            sObj.transform.localScale = Vector3.one * 1.2f;
            stars[i] = sObj.transform;
        }
    }

    void Update()
    {
        currentAngle += orbitSpeed * Time.deltaTime;
        for (int i = 0; i < 3; i++)
        {
            if (stars[i] == null) continue;
            float rad = (currentAngle + i * 120f) * Mathf.Deg2Rad;
            stars[i].localPosition = new Vector3(Mathf.Cos(rad) * orbitRadius, Mathf.Sin(rad) * (orbitRadius * 0.45f) + 1.2f, 0f);
        }
    }
}

