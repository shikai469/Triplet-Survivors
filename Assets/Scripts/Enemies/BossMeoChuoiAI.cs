using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Trí tuệ nhân tạo (AI) và Điều khiển toàn bộ bộ kỹ năng của Boss Mèo Chuối:
/// - 3 Giai đoạn tiến trình máu (100%-70%, 70%-30%, <30% Cuồng Nộ)
/// - Skill 1: Lao Đầu Ăn Vạ (Tantrum Rush) -> Đâm vào tường lửa bị CHOÁNG 1.6s
/// - Skill 2: Khóc Thét Tuôn Rơi (Tear Barrage) -> Bullet hell nước mắt
/// - Skill 3: Bẫy Vỏ Chuối (Slippery Banana Peels) -> Làm trượt và giảm tốc
/// - Skill 4: Mưa Chuối Rơi (Banana Orbital Strike) -> Oanh tạc AoE
/// - Skill 5: Tiếng Khóc Siêu Thanh (Ultrasonic Shockwave) -> Nộ Phase 3
/// </summary>
public class BossMeoChuoiAI : MonoBehaviour
{
    [Header("Component References")]
    private EnemyHealth enemyHealth;
    private EnemyMovement enemyMovement;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private Transform playerTransform;

    [Header("Skill Balance & Stats")]
    public float baseMoveSpeed = 1.6f;
    public float rushSpeed = 8.5f;
    public int rushDamage = 26;
    public int tearDamage = 14;
    public int bananaStrikeDamage = 22;
    public int shockwaveDamage = 16;

    // Trạng thái nội bộ
    private int currentPhase = 1;
    private bool isExecutingSkill = false;
    private bool isStunned = false;
    private float initialMaxHp = 6000f;
    private GameObject activeTelegraphLine;
    private GameObject activeDizzyStars;
    private Coroutine mainLoopCoroutine;

    void Awake()
    {
        enemyHealth = GetComponent<EnemyHealth>();
        enemyMovement = GetComponent<EnemyMovement>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        if (PlayerStats.Instance != null)
        {
            playerTransform = PlayerStats.Instance.transform;
        }

        if (enemyHealth != null)
        {
            initialMaxHp = enemyHealth.maxHealth;
        }

        if (enemyMovement != null)
        {
            enemyMovement.moveSpeed = baseMoveSpeed;
        }

        mainLoopCoroutine = StartCoroutine(BossCombatRoutine());
    }

    void OnDisable()
    {
        if (mainLoopCoroutine != null)
        {
            StopCoroutine(mainLoopCoroutine);
            mainLoopCoroutine = null;
        }
        CleanupVisuals();
    }

    void Update()
    {
        // Luôn cập nhật Phase theo % máu hiện tại
        UpdatePhase();

        // Tự động lật mặt Sprite theo hướng di chuyển nếu đang không thi triển skill đặc thù
        if (!isExecutingSkill && !isStunned && playerTransform != null && spriteRenderer != null)
        {
            spriteRenderer.flipX = playerTransform.position.x < transform.position.x;
        }
    }

    private void UpdatePhase()
    {
        if (enemyHealth == null) return;

        float hpRatio = enemyHealth.currentHealth / (initialMaxHp > 0 ? initialMaxHp : 1f);

        if (hpRatio > 0.70f)
        {
            currentPhase = 1;
        }
        else if (hpRatio > 0.30f)
        {
            if (currentPhase != 2)
            {
                currentPhase = 2;
                if (enemyMovement != null) enemyMovement.moveSpeed = baseMoveSpeed * 1.2f;
                Debug.Log("<color=yellow>[BOSS MÈO CHUỐI] BƯỚC VÀO PHASE 2: BẮT ĐẦU KHÓC NỨC NỞ!</color>");
            }
        }
        else
        {
            if (currentPhase != 3)
            {
                currentPhase = 3;
                if (enemyMovement != null) enemyMovement.moveSpeed = baseMoveSpeed * 1.35f;
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = new Color(1f, 0.7f, 0.7f, 1f); // Nộ đỏ rực
                }
                Debug.Log("<color=red>[BOSS MÈO CHUỐI] BƯỚC VÀO PHASE 3: CUỒNG NỘ TUYỆT VỌNG!</color>");
            }
        }
    }

    // =========================================================================
    // VÒNG LẶP CHIẾN ĐẤU CHÍNH CỦA BOSS (COMBAT LOOP)
    // =========================================================================
    private IEnumerator BossCombatRoutine()
    {
        // Chờ 1.5s lúc Boss vừa đáp xuống đấu trường để người chơi định thần
        yield return new WaitForSeconds(1.5f);

        int skillIndex = 0;

        while (enemyHealth != null && enemyHealth.currentHealth > 0)
        {
            // 1. Giai đoạn rượt đuổi người chơi thông thường
            float chaseTime = currentPhase == 1 ? 4.5f : (currentPhase == 2 ? 3.5f : 2.5f);
            SetMovementEnabled(true);
            PlayAnimation("Walk");

            yield return new WaitForSeconds(chaseTime);

            if (isStunned || enemyHealth.currentHealth <= 0) yield return null;

            // 2. Lựa chọn tung kỹ năng tương ứng với Phase hiện tại
            isExecutingSkill = true;
            SetMovementEnabled(false);

            if (currentPhase == 1)
            {
                // Phase 1: Luân phiên Lao Đầu Ăn Vạ và Bẫy Vỏ Chuối
                if (skillIndex % 2 == 0) yield return StartCoroutine(SkillTantrumRush());
                else yield return StartCoroutine(SkillBananaPeelTrap(count: 3));
            }
            else if (currentPhase == 2)
            {
                // Phase 2: Lao Đầu Ăn Vạ, Khóc Thét Tuôn Rơi, Mưa Chuối Rơi, Bẫy Vỏ Chuối
                int roll = skillIndex % 4;
                switch (roll)
                {
                    case 0: yield return StartCoroutine(SkillTearBarrage(waves: 3, tearsPerWave: 5)); break;
                    case 1: yield return StartCoroutine(SkillTantrumRush()); break;
                    case 2: yield return StartCoroutine(SkillBananaOrbitalStrike(count: 4)); break;
                    case 3: yield return StartCoroutine(SkillBananaPeelTrap(count: 4)); break;
                }
            }
            else
            {
                // Phase 3 Cuồng Nộ: Tiếng Khóc Siêu Thanh, Mưa Chuối Rơi, Khóc Thét dày đặc, Lao Đầu liên tục
                int roll = skillIndex % 4;
                switch (roll)
                {
                    case 0: yield return StartCoroutine(SkillUltrasonicWhineShockwave()); break;
                    case 1: yield return StartCoroutine(SkillTearBarrage(waves: 4, tearsPerWave: 7)); break;
                    case 2: yield return StartCoroutine(SkillBananaOrbitalStrike(count: 6)); break;
                    case 3: yield return StartCoroutine(SkillTantrumRush()); break;
                }
            }

            skillIndex++;
            isExecutingSkill = false;

            // Thời gian nghỉ ngắn tạo cơ hội (recovery window) cho người chơi
            float restTime = currentPhase == 3 ? 0.6f : 1.0f;
            yield return new WaitForSeconds(restTime);
        }
    }

    // =========================================================================
    // SKILL 1: LAO ĐẦU ĂN VẠ (TANTRUM RUSH -> STUN VÀO TƯỜNG LỬA)
    // =========================================================================
    private IEnumerator SkillTantrumRush()
    {
        if (playerTransform == null) yield break;

        PlayAnimation("Walk");
        Vector2 targetDir = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;

        // 1. Telegraph: Kẻ tia đỏ chỉ báo đường lao
        ShowTelegraphLine(transform.position, targetDir, length: 11f);

        // Boss rung lắc nhẹ 0.8s lấy đà
        float telegraphElapsed = 0f;
        Vector3 basePos = transform.position;
        while (telegraphElapsed < 0.85f)
        {
            telegraphElapsed += Time.deltaTime;
            float wiggle = Mathf.Sin(telegraphElapsed * 35f) * 0.08f;
            transform.position = basePos + new Vector3(wiggle, 0f, 0f);
            yield return null;
        }

        HideTelegraphLine();

        // 2. Bắt đầu cú phi thân thần tốc
        float rushDuration = 1.3f;
        float elapsed = 0f;
        bool crashedIntoWall = false;

        float currentRushSpeed = currentPhase == 3 ? rushSpeed * 1.25f : rushSpeed;

        while (elapsed < rushDuration)
        {
            elapsed += Time.deltaTime;

            if (rb != null)
            {
                rb.linearVelocity = targetDir * currentRushSpeed;
            }
            else
            {
                transform.position += (Vector3)(targetDir * currentRushSpeed * Time.deltaTime);
            }

            // Kiểm tra tông trúng người chơi
            if (playerTransform != null && Vector2.Distance(transform.position, playerTransform.position) <= 1.2f)
            {
                PlayerHealth ph = playerTransform.GetComponent<PlayerHealth>();
                if (ph != null) ph.TakeDamage(rushDamage);

                PlayerMovement pm = playerTransform.GetComponent<PlayerMovement>();
                if (pm != null) pm.ApplyKnockback(targetDir, force: 8f, duration: 0.3f);
            }

            // Kiểm tra va chạm mép Đấu Trường Vòng Tròn Lửa
            if (FireRingArena.Instance != null)
            {
                Vector2 center = FireRingArena.Instance.transform.position;
                float dist = Vector2.Distance(center, transform.position);
                float maxAllowed = FireRingArena.Instance.radius - FireRingArena.Instance.bossPadding;

                if (dist >= maxAllowed - 0.2f)
                {
                    // ĐÂM TRÚNG TƯỜNG LỬA -> BỊ CHOÁNG!
                    crashedIntoWall = true;
                    break;
                }
            }

            yield return null;
        }

        if (rb != null) rb.linearVelocity = Vector2.zero;

        // 3. Nếu đâm trúng tường lửa: Bị Choáng (Stun) 1.6s
        if (crashedIntoWall)
        {
            yield return StartCoroutine(HandleWallStun(duration: 1.6f));
        }
    }

    private IEnumerator HandleWallStun(float duration)
    {
        isStunned = true;
        PlayAnimation("Cry"); // Mèo khóc nấc vì đâm đầu sưng trán!

        // Hiện hiệu ứng sao quay trên đầu
        ShowDizzyStars();

        Debug.Log("<color=cyan>[BOSS MÈO CHUỐI] ĐÂM ĐẦU VÀO TƯỜNG LỬA! BỊ CHOÁNG 1.6 GIÂY!</color>");

        yield return new WaitForSeconds(duration);

        HideDizzyStars();
        isStunned = false;
        PlayAnimation("Walk");
    }

    // =========================================================================
    // SKILL 2: KHÓC THÉT TUÔN RƠI (TEAR BARRAGE)
    // =========================================================================
    private IEnumerator SkillTearBarrage(int waves = 3, int tearsPerWave = 5)
    {
        if (playerTransform == null) yield break;

        PlayAnimation("Cry");

        // Rung rinh lấy hơi khóc 0.5s
        yield return new WaitForSeconds(0.5f);

        for (int w = 0; w < waves; w++)
        {
            if (enemyHealth != null && enemyHealth.currentHealth <= 0) break;

            Vector2 baseDir = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
            float totalArc = currentPhase == 3 ? 120f : 60f;
            float angleStep = tearsPerWave > 1 ? totalArc / (tearsPerWave - 1) : 0f;
            float startAngle = -totalArc * 0.5f;

            for (int i = 0; i < tearsPerWave; i++)
            {
                float currentAngle = startAngle + (i * angleStep);
                Vector2 shootDir = Quaternion.Euler(0, 0, currentAngle) * baseDir;

                GameObject tearObj = new GameObject("BossTear");
                tearObj.transform.position = transform.position + (Vector3)(shootDir * 0.5f);
                BossTearProjectile tear = tearObj.AddComponent<BossTearProjectile>();
                float speed = currentPhase == 3 ? 6.2f : 5.0f;
                tear.Init(shootDir, speed, tearDamage);
            }

            yield return new WaitForSeconds(0.32f);
        }

        yield return new WaitForSeconds(0.3f);
        PlayAnimation("Walk");
    }

    // =========================================================================
    // SKILL 3: BẪY VỎ CHUỐI (BANANA PEEL TRAP)
    // =========================================================================
    private IEnumerator SkillBananaPeelTrap(int count = 3)
    {
        PlayAnimation("Walk");

        // Boss quay 1 vòng xoay tít để quăng vỏ chuối
        float spinTime = 0.4f;
        float spinElapsed = 0f;
        while (spinElapsed < spinTime)
        {
            spinElapsed += Time.deltaTime;
            transform.Rotate(0, 0, 720f * Time.deltaTime);
            yield return null;
        }
        transform.rotation = Quaternion.identity;

        Vector3 arenaCenter = FireRingArena.Instance != null 
            ? FireRingArena.Instance.transform.position 
            : transform.position;
        float maxRadius = FireRingArena.Instance != null 
            ? FireRingArena.Instance.radius * 0.75f 
            : 5f;

        for (int i = 0; i < count; i++)
        {
            // Lấy vị trí ngẫu nhiên trong đấu trường
            Vector2 randCircle = Random.insideUnitCircle * maxRadius;
            Vector3 targetLand = arenaCenter + new Vector3(randCircle.x, randCircle.y, 0f);

            GameObject peelObj = new GameObject("BananaPeel");
            peelObj.transform.position = transform.position;
            BossBananaPeelTrap peel = peelObj.AddComponent<BossBananaPeelTrap>();
            peel.SetupToss(targetLand, duration: 0.55f);
        }

        yield return new WaitForSeconds(0.3f);
    }

    // =========================================================================
    // SKILL 4: MƯA CHUỐI RƠI (BANANA ORBITAL STRIKE)
    // =========================================================================
    private IEnumerator SkillBananaOrbitalStrike(int count = 4)
    {
        PlayAnimation("Cry"); // Khóc cầu mưa chuối

        Vector3 arenaCenter = FireRingArena.Instance != null 
            ? FireRingArena.Instance.transform.position 
            : transform.position;
        float maxRadius = FireRingArena.Instance != null 
            ? FireRingArena.Instance.radius * 0.8f 
            : 5.5f;

        // Vị trí thứ nhất luôn nhắm gần người chơi để bắt người chơi phải di chuyển
        if (playerTransform != null)
        {
            SpawnBananaStrike(playerTransform.position);
        }

        // Các vị trí còn lại rải đều trong đấu trường
        for (int i = 1; i < count; i++)
        {
            Vector2 randCircle = Random.insideUnitCircle * maxRadius;
            Vector3 strikePos = arenaCenter + new Vector3(randCircle.x, randCircle.y, 0f);
            SpawnBananaStrike(strikePos);
        }

        yield return new WaitForSeconds(1.3f);
        PlayAnimation("Walk");
    }

    private void SpawnBananaStrike(Vector3 pos)
    {
        GameObject strikeObj = new GameObject("BananaBombStrike");
        BossBananaBombStrike strike = strikeObj.AddComponent<BossBananaBombStrike>();
        strike.LaunchStrike(pos, warningTime: 1.15f, dmg: bananaStrikeDamage);
    }

    // =========================================================================
    // SKILL 5: TIẾNG KHÓC SIÊU THANH (ULTRASONIC WHINE SHOCKWAVE - NỘ PHASE 3)
    // =========================================================================
    private IEnumerator SkillUltrasonicWhineShockwave()
    {
        PlayAnimation("Cry");

        // Gồng năng lượng phát sáng 0.8s
        float chargeTime = 0.8f;
        float elapsed = 0f;
        Vector3 baseScale = transform.localScale;

        while (elapsed < chargeTime)
        {
            elapsed += Time.deltaTime;
            // Đầu và thân mèo phồng to nhẹ biểu lộ cảm xúc nén tiếng khóc
            float pulse = Mathf.Sin(elapsed * 25f) * 0.15f;
            transform.localScale = baseScale * (1f + pulse);
            yield return null;
        }

        transform.localScale = baseScale;

        float arenaRadius = FireRingArena.Instance != null ? FireRingArena.Instance.radius : 9f;

        // Phát ra 2 đợt sóng âm cách nhau 0.5s
        for (int i = 0; i < 2; i++)
        {
            if (enemyHealth != null && enemyHealth.currentHealth <= 0) break;

            GameObject waveObj = new GameObject($"Shockwave_{i}");
            BossUltrasonicWave wave = waveObj.AddComponent<BossUltrasonicWave>();
            wave.InitWave(transform.position, arenaRadius, speed: 7.2f, dmg: shockwaveDamage);

            yield return new WaitForSeconds(0.48f);
        }

        yield return new WaitForSeconds(0.4f);
        PlayAnimation("Walk");
    }

    // =========================================================================
    // CÁC HÀM TIỆN ÍCH TRỢ GIÚP HIỂN THỊ & HOẠT ẢNH
    // =========================================================================
    private void PlayAnimation(string stateName)
    {
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.Play(stateName);
        }
    }

    private void SetMovementEnabled(bool isEnabled)
    {
        if (enemyMovement != null)
        {
            enemyMovement.enabled = isEnabled;
        }
        if (!isEnabled && rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    private void ShowTelegraphLine(Vector3 start, Vector2 dir, float length)
    {
        if (activeTelegraphLine == null)
        {
            activeTelegraphLine = new GameObject("TantrumTelegraph");
            LineRenderer lr = activeTelegraphLine.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.positionCount = 2;
            lr.startWidth = 0.25f;
            lr.endWidth = 0.25f;

            Shader s = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (s == null) s = Shader.Find("Sprites/Default");
            Material mat = new Material(s);
            mat.color = new Color(1f, 0.15f, 0.15f, 0.85f);
            lr.material = mat;
            lr.sortingOrder = 13;
        }

        LineRenderer renderer = activeTelegraphLine.GetComponent<LineRenderer>();
        if (renderer != null)
        {
            renderer.SetPosition(0, start);
            renderer.SetPosition(1, start + (Vector3)(dir * length));
        }

        activeTelegraphLine.SetActive(true);
    }

    private void HideTelegraphLine()
    {
        if (activeTelegraphLine != null)
        {
            activeTelegraphLine.SetActive(false);
        }
    }

    private void ShowDizzyStars()
    {
        if (activeDizzyStars == null)
        {
            activeDizzyStars = new GameObject("DizzyStarsEffect");
            activeDizzyStars.transform.parent = transform;
            activeDizzyStars.transform.localPosition = Vector3.zero;
            activeDizzyStars.AddComponent<BossDizzyStarsEffect>();
        }
        activeDizzyStars.SetActive(true);
    }

    private void HideDizzyStars()
    {
        if (activeDizzyStars != null)
        {
            activeDizzyStars.SetActive(false);
        }
    }

    private void CleanupVisuals()
    {
        if (activeTelegraphLine != null) Destroy(activeTelegraphLine);
        if (activeDizzyStars != null) Destroy(activeDizzyStars);
    }
}

