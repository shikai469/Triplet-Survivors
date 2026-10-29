using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MonsterSetupTool
{
    private const string PREFAB_DIR = "Assets/Prefabs";
    private const string ANIM_DIR = "Assets/Animations";

    [MenuItem("Tools/Setup New Monsters")]
    public static void SetupNewMonsters()
    {
        AssetDatabase.Refresh();

        // 1. Cấu hình Sprite Importer cho các file PNG của Blood Monster và Demon
        string bloodWalkPath = "Assets/MonsterAssets/Characters(100x100 split)/Blood Monster_A/Blood Monster_A with shadows/Blood Monster_A_Walk.png";
        string bloodIdlePath = "Assets/MonsterAssets/Characters(100x100 split)/Blood Monster_A/Blood Monster_A with shadows/Blood Monster_A_Idle.png";
        string demonWalkPath = "Assets/MonsterAssets/Characters(100x100 split)/Demon_A/Demon_A with shadows/Demon_A_Walk.png";
        string demonIdlePath = "Assets/MonsterAssets/Characters(100x100 split)/Demon_A/Demon_A with shadows/Demon_A_Idle.png";

        SliceSpritesheet(bloodWalkPath, 8, 100, 100);
        SliceSpritesheet(bloodIdlePath, 6, 100, 100);
        SliceSpritesheet(demonWalkPath, 8, 100, 100);
        SliceSpritesheet(demonIdlePath, 6, 100, 100);

        AssetDatabase.Refresh();

        // 2. Tạo Animation Clips & Animator Controllers
        if (!System.IO.Directory.Exists(ANIM_DIR))
        {
            System.IO.Directory.CreateDirectory(ANIM_DIR);
            AssetDatabase.Refresh();
        }

        // Tạo Animator Controller cho Blood Monster
        var bloodWalkSprites = LoadSprites(bloodWalkPath);
        var bloodIdleSprites = LoadSprites(bloodIdlePath);
        var bloodWalkClip = CreateAnimationClip("BloodMonster_Walk", bloodWalkSprites, 10f);
        var bloodIdleClip = CreateAnimationClip("BloodMonster_Idle", bloodIdleSprites, 8f);
        var bloodController = CreateMonsterAnimatorController("BloodMonster_Controller", bloodWalkClip, bloodIdleClip);

        // Tạo Animator Controller cho Demon
        var demonWalkSprites = LoadSprites(demonWalkPath);
        var demonIdleSprites = LoadSprites(demonIdlePath);
        var demonWalkClip = CreateAnimationClip("Demon_Walk", demonWalkSprites, 10f);
        var demonIdleClip = CreateAnimationClip("Demon_Idle", demonIdleSprites, 8f);
        var demonController = CreateMonsterAnimatorController("Demon_Controller", demonWalkClip, demonIdleClip);

        // 3. Lấy Prefab tham chiếu Drop Items (XP & Gold)
        GameObject xpPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/XP.prefab");
        GameObject goldPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Gold.prefab");

        // 4. Tạo Prefab cho Blood Monster
        Sprite defaultBloodSprite = bloodWalkSprites.Count > 0 ? bloodWalkSprites[0] : null;
        GameObject bloodPrefab = CreateMonsterPrefab(
            prefabName: "Enemy-BloodMonster",
            sprite: defaultBloodSprite,
            controller: bloodController,
            colliderRadius: 0.35f,
            moveSpeed: 2.3f,
            attackDamage: 12,
            maxHp: 35f,
            armor: 0,
            knockbackRes: 40f,
            xpPrefab: xpPrefab,
            goldPrefab: goldPrefab,
            isBoss: false,
            scale: 1.0f
        );

        // 5. Tạo Prefab cho Demon (Quái Tinh Anh)
        Sprite defaultDemonSprite = demonWalkSprites.Count > 0 ? demonWalkSprites[0] : null;
        GameObject demonPrefab = CreateMonsterPrefab(
            prefabName: "Enemy-Demon",
            sprite: defaultDemonSprite,
            controller: demonController,
            colliderRadius: 0.42f,
            moveSpeed: 1.8f,
            attackDamage: 18,
            maxHp: 65f,
            armor: 1,
            knockbackRes: 25f,
            xpPrefab: xpPrefab,
            goldPrefab: goldPrefab,
            isBoss: false,
            scale: 1.15f
        );

        // 6. Tạo Prefab cho Boss Demon (Trùm Cuối Màn Chơi)
        GameObject bossDemonPrefab = CreateMonsterPrefab(
            prefabName: "Boss-Demon",
            sprite: defaultDemonSprite,
            controller: demonController,
            colliderRadius: 0.45f,
            moveSpeed: 1.4f,
            attackDamage: 32,
            maxHp: 450f,
            armor: 3,
            knockbackRes: 0f,
            xpPrefab: xpPrefab,
            goldPrefab: goldPrefab,
            isBoss: true,
            scale: 2.0f
        );

        // 7. Gán các quái mới vào GameplayScene
        UpdateGameplaySceneSpawners(bloodPrefab, demonPrefab, bossDemonPrefab);

        Debug.Log("<color=green>[MONSTER SETUP] Đã tạo thành công Prefab Quái Máu (Blood Monster), Ác Quỷ (Demon) và Trùm Ác Quỷ (Boss Demon)! Đã liên kết vào GameplayScene.</color>");
    }

    private static void SliceSpritesheet(string assetPath, int frameCount, int frameW, int frameH)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning("Không tìm thấy texture tại: " + assetPath);
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 100;
        importer.filterMode = FilterMode.Point; // Pixel Art sắc nét
        importer.textureCompression = TextureImporterCompression.Uncompressed;

        string baseName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        var metaList = new List<SpriteMetaData>();

        for (int i = 0; i < frameCount; i++)
        {
            var meta = new SpriteMetaData
            {
                name = $"{baseName}_{i}",
                rect = new Rect(i * frameW, 0, frameW, frameH),
                alignment = (int)SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f)
            };
            metaList.Add(meta);
        }

#pragma warning disable CS0618
        importer.spritesheet = metaList.ToArray();
#pragma warning restore CS0618
        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();
    }

    private static List<Sprite> LoadSprites(string assetPath)
    {
        var sprites = new List<Sprite>();
        var assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        foreach (var obj in assets)
        {
            if (obj is Sprite s)
            {
                sprites.Add(s);
            }
        }
        sprites.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return sprites;
    }

    private static AnimationClip CreateAnimationClip(string clipName, List<Sprite> sprites, float sampleRate)
    {
        string clipPath = $"{ANIM_DIR}/{clipName}.anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, clipPath);
        }

        clip.frameRate = sampleRate;

        // Cấu hình Loop
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        // Tạo Keyframes cho SpriteRenderer.m_Sprite
        EditorCurveBinding binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "",
            propertyName = "m_Sprite"
        };

        if (sprites != null && sprites.Count > 0)
        {
            ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[sprites.Count];
            float timePerFrame = 1f / sampleRate;

            for (int i = 0; i < sprites.Count; i++)
            {
                keyframes[i] = new ObjectReferenceKeyframe
                {
                    time = i * timePerFrame,
                    value = sprites[i]
                };
            }

            AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);
        }

        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static RuntimeAnimatorController CreateMonsterAnimatorController(string controllerName, AnimationClip walkClip, AnimationClip idleClip)
    {
        string path = $"{ANIM_DIR}/{controllerName}.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        }

        var rootSm = controller.layers[0].stateMachine;
        rootSm.states = new ChildAnimatorState[0]; // Reset states

        // State mặc định: Walk (quái luôn di chuyển về người chơi)
        var walkState = rootSm.AddState("Walk");
        walkState.motion = walkClip;
        rootSm.defaultState = walkState;

        if (idleClip != null)
        {
            var idleState = rootSm.AddState("Idle");
            idleState.motion = idleClip;
        }

        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static GameObject CreateMonsterPrefab(
        string prefabName,
        Sprite sprite,
        RuntimeAnimatorController controller,
        float colliderRadius,
        float moveSpeed,
        int attackDamage,
        float maxHp,
        int armor,
        float knockbackRes,
        GameObject xpPrefab,
        GameObject goldPrefab,
        bool isBoss,
        float scale)
    {
        string path = $"{PREFAB_DIR}/{prefabName}.prefab";

        GameObject go = new GameObject(prefabName);
        go.tag = "Enemy";
        go.transform.localScale = Vector3.one * scale;

        // 1. SpriteRenderer
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = isBoss ? 15 : 10;
        Material unlitMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Settings/Sprite-Unlit-Default.mat");
        if (unlitMat != null) sr.sharedMaterial = unlitMat;

        // 2. Animator
        if (controller != null)
        {
            Animator anim = go.AddComponent<Animator>();
            anim.runtimeAnimatorController = controller;
        }

        // 3. Rigidbody2D
        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.linearDamping = 10f;
        rb.angularDamping = 0.05f;
        rb.mass = isBoss ? 200f : 50f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // 4. CircleCollider2D
        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.radius = colliderRadius;

        // 5. EnemyMovement
        EnemyMovement em = go.AddComponent<EnemyMovement>();
        em.moveSpeed = moveSpeed;
        em.attackDamage = attackDamage;
        em.knockbackResistance = knockbackRes;

        // 6. EnemyHealth
        EnemyHealth eh = go.AddComponent<EnemyHealth>();
        eh.baseMaxHealth = maxHp;
        eh.maxHealth = maxHp;
        eh.currentHealth = maxHp;
        eh.armor = armor;
        eh.knockbackResistance = knockbackRes;
        eh.isBoss = isBoss;

        // 7. LootDropSystem
        LootDropSystem lds = go.AddComponent<LootDropSystem>();
        var items = new List<LootItem>();
        if (xpPrefab != null)
        {
            items.Add(new LootItem { itemPrefab = xpPrefab, dropChance = isBoss ? 0.3f : 0.75f });
        }
        if (goldPrefab != null)
        {
            items.Add(new LootItem { itemPrefab = goldPrefab, dropChance = isBoss ? 0.7f : 0.25f });
        }
        lds.lootTable = items.ToArray();

        // Lưu ra file Prefab
        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        GameObject.DestroyImmediate(go);
        return savedPrefab;
    }

    private static void UpdateGameplaySceneSpawners(GameObject bloodPrefab, GameObject demonPrefab, GameObject bossDemonPrefab)
    {
        var activeScene = EditorSceneManager.GetActiveScene();
        if (activeScene.name != "GameplayScene")
        {
            EditorSceneManager.OpenScene("Assets/Scenes/GameplayScene.unity");
        }

        GameObject choDoPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy-ChoDo.prefab");

        // Tìm EnemySpawner
        EnemySpawner spawner = Object.FindAnyObjectByType<EnemySpawner>();
        if (spawner != null)
        {
            spawner.enemyPrefab = choDoPrefab != null ? choDoPrefab : bloodPrefab;
            spawner.enemyPrefabs = new GameObject[]
            {
                choDoPrefab,
                bloodPrefab,
                demonPrefab
            };
            EditorUtility.SetDirty(spawner);
        }

        // Tìm GameStageManager để gán Boss
        GameStageManager stageManager = Object.FindAnyObjectByType<GameStageManager>();
        if (stageManager != null)
        {
            stageManager.bossPrefab = bossDemonPrefab;
            if (stageManager.activeConfig != null)
            {
                stageManager.activeConfig.bossPrefabOverride = bossDemonPrefab;
            }
            EditorUtility.SetDirty(stageManager);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
    }
}
