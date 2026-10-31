using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor Tool tự động tạo Asset đồ họa, Prefab và thiết lập Đấu Trường Map phong cách chuẩn Brotato
/// theo 3 chủ đề cấp độ:
/// - Thường: Rừng nguyên sinh huyền bí
/// - Khó: Hỏa sơn và dung nham
/// - Vô Hạn: Vực sâu hư không
/// - Dễ: Đất cát nguyên bản
/// </summary>
public static class BrotatoMapBuilder
{
    private const string SPRITES_FOLDER = "Assets/Sprites/BrotatoMap";
    private const string PREFAB_PATH = "Assets/Prefabs/BrotatoArenaMap.prefab";
    private const string SCENE_PATH = "Assets/Scenes/GameplayScene.unity";

    public const float ARENA_WIDTH = 38f;
    public const float ARENA_HEIGHT = 24f;
    public const float WALL_THICKNESS = 2f;
    public const int PPU = 32;

    [MenuItem("Tools/Brotato/1. Generate Brotato Textures & Prefab")]
    public static void GenerateAllAssets()
    {
        EnsureFolderExists(SPRITES_FOLDER);

        Debug.Log("<color=yellow>[BROTATO MAP] Đang tạo bộ texture procedural chuẩn phong cách Brotato cho các cấp độ...</color>");

        string defaultFloorPath = Path.Combine(SPRITES_FOLDER, "BrotatoFloor.png");
        string forestPath = Path.Combine(SPRITES_FOLDER, "BrotatoFloor_Forest.png");
        string volcanoPath = Path.Combine(SPRITES_FOLDER, "BrotatoFloor_Volcano.png");
        string voidPath = Path.Combine(SPRITES_FOLDER, "BrotatoFloor_Void.png");
        string voidOuterPath = Path.Combine(SPRITES_FOLDER, "BrotatoOuterVoid.png");
        string cosmicOuterPath = Path.Combine(SPRITES_FOLDER, "BrotatoOuterVoid_Cosmic.png");

        GenerateDefaultFloorTexture(defaultFloorPath);
        GenerateForestFloorTexture(forestPath);
        GenerateVolcanoFloorTexture(volcanoPath);
        GenerateVoidFloorTexture(voidPath);
        GenerateOuterVoidTexture(voidOuterPath);
        GenerateCosmicOuterVoidTexture(cosmicOuterPath);

        AssetDatabase.Refresh();

        ConfigureSpriteImporter(defaultFloorPath, PPU, TextureWrapMode.Clamp);
        ConfigureSpriteImporter(forestPath, PPU, TextureWrapMode.Clamp);
        ConfigureSpriteImporter(volcanoPath, PPU, TextureWrapMode.Clamp);
        ConfigureSpriteImporter(voidPath, PPU, TextureWrapMode.Clamp);
        ConfigureSpriteImporter(voidOuterPath, PPU, TextureWrapMode.Repeat);
        ConfigureSpriteImporter(cosmicOuterPath, PPU, TextureWrapMode.Repeat);

        AssetDatabase.Refresh();

        Debug.Log("<color=green>[BROTATO MAP] Đã tạo thành công bộ Sprite Brotato! Đang tạo Prefab...</color>");
        CreateArenaPrefab();
    }

    [MenuItem("Tools/Brotato/2. Replace Original Map in GameplayScene")]
    public static void ReplaceMapInGameplayScene()
    {
        if (!File.Exists(PREFAB_PATH))
        {
            GenerateAllAssets();
        }

        Scene currentScene = SceneManager.GetActiveScene();
        if (currentScene.path != SCENE_PATH)
        {
            currentScene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
        }

        Debug.Log("<color=cyan>[BROTATO MAP] Đang cập nhật map trong GameplayScene...</color>");

        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.backgroundColor = Color.black;
            Transform oldBg = mainCam.transform.Find("InfiniteBackground");
            if (oldBg != null)
            {
                Undo.DestroyObjectImmediate(oldBg.gameObject);
                Debug.Log("[BROTATO MAP] Đã gỡ bỏ InfiniteBackground cũ khỏi Main Camera.");
            }
        }

        InfiniteBackground[] oldBgs = Object.FindObjectsByType<InfiniteBackground>();
        foreach (var bg in oldBgs)
        {
            Undo.DestroyObjectImmediate(bg.gameObject);
        }

        BrotatoArena oldArena = Object.FindAnyObjectByType<BrotatoArena>();
        if (oldArena != null)
        {
            Undo.DestroyObjectImmediate(oldArena.gameObject);
        }

        GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
        if (prefabAsset == null)
        {
            Debug.LogError($"[BROTATO MAP] Không tìm thấy Prefab tại {PREFAB_PATH}");
            return;
        }

        GameObject arenaInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset);
        arenaInstance.transform.position = Vector3.zero;
        Undo.RegisterCreatedObjectUndo(arenaInstance, "Create Brotato Arena Map");

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerObj.transform.position = Vector3.zero;
            Debug.Log("[BROTATO MAP] Đã đặt vị trí Player về trung tâm đấu trường (0, 0, 0).");
        }

        if (mainCam != null)
        {
            mainCam.transform.position = new Vector3(0, 0, -10f);
            CameraFollow camFollow = mainCam.GetComponent<CameraFollow>();
            if (camFollow != null)
            {
                camFollow.clampToArena = true;
                if (playerObj != null) camFollow.player = playerObj.transform;
            }
        }

        EditorSceneManager.MarkSceneDirty(currentScene);
        EditorSceneManager.SaveScene(currentScene);

        Debug.Log("<color=green>=========================================================</color>");
        Debug.Log("<color=green>[BROTATO MAP] HOÀN TẤT! ĐẤU TRƯỜNG CHUẨN BROTATO ĐÃ ĐƯỢC ÁP DỤNG THÀNH CÔNG!</color>");
        Debug.Log("<color=green>=========================================================</color>");
    }

    private static void CreateArenaPrefab()
    {
        EnsureFolderExists("Assets/Prefabs");

        GameObject root = new GameObject("BrotatoArenaMap");
        BrotatoArena arenaScript = root.AddComponent<BrotatoArena>();
        arenaScript.arenaWidth = ARENA_WIDTH;
        arenaScript.arenaHeight = ARENA_HEIGHT;
        arenaScript.wallThickness = WALL_THICKNESS;

        // 1. Outer Void
        GameObject outerVoidObj = new GameObject("OuterVoid");
        outerVoidObj.transform.SetParent(root.transform, false);
        outerVoidObj.transform.localPosition = Vector3.zero;
        SpriteRenderer voidRenderer = outerVoidObj.AddComponent<SpriteRenderer>();
        voidRenderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Path.Combine(SPRITES_FOLDER, "BrotatoOuterVoid.png"));
        voidRenderer.drawMode = SpriteDrawMode.Tiled;
        voidRenderer.size = new Vector2(100f, 70f);
        voidRenderer.sortingOrder = -20;
        arenaScript.outerVoidRenderer = voidRenderer;

        // 2. Arena Floor
        GameObject floorObj = new GameObject("Floor");
        floorObj.transform.SetParent(root.transform, false);
        floorObj.transform.localPosition = Vector3.zero;
        SpriteRenderer floorRenderer = floorObj.AddComponent<SpriteRenderer>();
        floorRenderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Path.Combine(SPRITES_FOLDER, "BrotatoFloor.png"));
        floorRenderer.sortingOrder = -10;
        arenaScript.floorRenderer = floorRenderer;

        // Gán các sprite themed maps vào BrotatoArena component
        arenaScript.defaultFloorSprite = AssetDatabase.LoadAssetAtPath<Sprite>(Path.Combine(SPRITES_FOLDER, "BrotatoFloor.png"));
        arenaScript.forestFloorSprite = AssetDatabase.LoadAssetAtPath<Sprite>(Path.Combine(SPRITES_FOLDER, "BrotatoFloor_Forest.png"));
        arenaScript.volcanoFloorSprite = AssetDatabase.LoadAssetAtPath<Sprite>(Path.Combine(SPRITES_FOLDER, "BrotatoFloor_Volcano.png"));
        arenaScript.voidFloorSprite = AssetDatabase.LoadAssetAtPath<Sprite>(Path.Combine(SPRITES_FOLDER, "BrotatoFloor_Void.png"));
        arenaScript.cosmicVoidSprite = AssetDatabase.LoadAssetAtPath<Sprite>(Path.Combine(SPRITES_FOLDER, "BrotatoOuterVoid_Cosmic.png"));

        // 3. Walls Root
        GameObject wallsRoot = new GameObject("Walls");
        wallsRoot.transform.SetParent(root.transform, false);
        arenaScript.wallsRoot = wallsRoot.transform;

        float halfW = ARENA_WIDTH * 0.5f;     // 19
        float halfH = ARENA_HEIGHT * 0.5f;    // 12
        float wallCenterDistX = halfW + (WALL_THICKNESS * 0.5f); // 20
        float wallCenterDistY = halfH + (WALL_THICKNESS * 0.5f); // 13

        CreateInvisibleWall("Wall_Top", wallsRoot.transform,
            new Vector3(0, wallCenterDistY, 0),
            new Vector2(ARENA_WIDTH + (WALL_THICKNESS * 2f), WALL_THICKNESS));

        CreateInvisibleWall("Wall_Bottom", wallsRoot.transform,
            new Vector3(0, -wallCenterDistY, 0),
            new Vector2(ARENA_WIDTH + (WALL_THICKNESS * 2f), WALL_THICKNESS));

        CreateInvisibleWall("Wall_Left", wallsRoot.transform,
            new Vector3(-wallCenterDistX, 0, 0),
            new Vector2(WALL_THICKNESS, ARENA_HEIGHT + (WALL_THICKNESS * 2f)));

        CreateInvisibleWall("Wall_Right", wallsRoot.transform,
            new Vector3(wallCenterDistX, 0, 0),
            new Vector2(WALL_THICKNESS, ARENA_HEIGHT + (WALL_THICKNESS * 2f)));

        // 4. Corners Root
        GameObject cornersRoot = new GameObject("Corners");
        cornersRoot.transform.SetParent(root.transform, false);
        cornersRoot.SetActive(false);
        arenaScript.cornersRoot = cornersRoot.transform;

        PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
        Object.DestroyImmediate(root);

        Debug.Log($"[BROTATO MAP] Đã tạo Prefab thành công tại: {PREFAB_PATH}");
    }

    private static void CreateInvisibleWall(string name, Transform parent, Vector3 localPos, Vector2 size)
    {
        GameObject wallObj = new GameObject(name);
        wallObj.transform.SetParent(parent, false);
        wallObj.transform.localPosition = localPos;
        wallObj.layer = LayerMask.NameToLayer("Default");

        BoxCollider2D collider = wallObj.AddComponent<BoxCollider2D>();
        collider.size = size;
        collider.offset = Vector2.zero;
    }

    #region Procedural Texture Generators

    private static float CalcJaggedDist(int x, int y, int width, int height)
    {
        float jagLeft = Mathf.Sin(y * 0.03f) * 4.5f + Mathf.Sin(y * 0.09f + 1.2f) * 3.0f + ((y % 5) - 2) * 0.8f + 8.0f;
        float jagRight = Mathf.Sin(y * 0.03f + 2.5f) * 4.5f + Mathf.Sin(y * 0.09f + 3.8f) * 3.0f + (((y + 2) % 5) - 2) * 0.8f + 8.0f;
        float jagBottom = Mathf.Sin(x * 0.03f) * 4.5f + Mathf.Sin(x * 0.09f + 2.1f) * 3.0f + ((x % 5) - 2) * 0.8f + 8.0f;
        float jagTop = Mathf.Sin(x * 0.03f + 1.7f) * 4.5f + Mathf.Sin(x * 0.09f + 0.4f) * 3.0f + (((x + 3) % 5) - 2) * 0.8f + 8.0f;

        float dLeft = x - jagLeft;
        float dRight = (width - 1 - jagRight) - x;
        float dBottom = y - jagBottom;
        float dTop = (height - 1 - jagTop) - y;

        float d = Mathf.Min(Mathf.Min(dLeft, dRight), Mathf.Min(dBottom, dTop));

        if (dLeft < 20f && dBottom < 20f)
        {
            float cx = 20f - dLeft; float cy = 20f - dBottom;
            d = 20f - Mathf.Sqrt(cx * cx + cy * cy);
        }
        else if (dRight < 20f && dBottom < 20f)
        {
            float cx = 20f - dRight; float cy = 20f - dBottom;
            d = 20f - Mathf.Sqrt(cx * cx + cy * cy);
        }
        else if (dLeft < 20f && dTop < 20f)
        {
            float cx = 20f - dLeft; float cy = 20f - dTop;
            d = 20f - Mathf.Sqrt(cx * cx + cy * cy);
        }
        else if (dRight < 20f && dTop < 20f)
        {
            float cx = 20f - dRight; float cy = 20f - dTop;
            d = 20f - Mathf.Sqrt(cx * cx + cy * cy);
        }
        return d;
    }

    private static void GenerateDefaultFloorTexture(string filePath)
    {
        int width = Mathf.RoundToInt(ARENA_WIDTH * PPU);
        int height = Mathf.RoundToInt(ARENA_HEIGHT * PPU);

        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color baseDirt = new Color(119f / 255f, 101f / 255f, 87f / 255f);
        Color darkRim = new Color(14f / 255f, 11f / 255f, 9f / 255f);
        Color edgeShadow = new Color(78f / 255f, 64f / 255f, 54f / 255f);
        Color clear = new Color(0, 0, 0, 0);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float d = CalcJaggedDist(x, y, width, height);
                if (d < 0f) tex.SetPixel(x, y, clear);
                else if (d < 2.0f) tex.SetPixel(x, y, darkRim);
                else if (d < 10.0f) tex.SetPixel(x, y, Color.Lerp(edgeShadow, baseDirt, (d - 2f) / 8f));
                else
                {
                    float n = (Mathf.Sin(x * 0.015f + y * 0.012f) * 2.2f + Mathf.Cos(x * 0.035f - y * 0.028f) * 1.8f) / 255f;
                    tex.SetPixel(x, y, new Color(Mathf.Clamp01(baseDirt.r + n), Mathf.Clamp01(baseDirt.g + n), Mathf.Clamp01(baseDirt.b + n), 1f));
                }
            }
        }
        tex.Apply();
        File.WriteAllBytes(filePath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    private static void GenerateForestFloorTexture(string filePath)
    {
        // Đồng bộ logic tạo map rừng
        int width = 1216, height = 768;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color baseGround = new Color(74f / 255f, 84f / 255f, 60f / 255f);
        Color darkRim = new Color(18f / 255f, 22f / 255f, 14f / 255f);
        Color edgeShadow = new Color(42f / 255f, 48f / 255f, 34f / 255f);
        Color clear = new Color(0, 0, 0, 0);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float d = CalcJaggedDist(x, y, width, height);
                if (d < 0f) tex.SetPixel(x, y, clear);
                else if (d < 2.0f) tex.SetPixel(x, y, darkRim);
                else if (d < 12.0f) tex.SetPixel(x, y, Color.Lerp(edgeShadow, baseGround, (d - 2f) / 10f));
                else
                {
                    float n = (Mathf.Sin(x * 0.015f + y * 0.012f) * 2.5f + Mathf.Cos(x * 0.035f - y * 0.028f) * 2.0f) / 255f;
                    tex.SetPixel(x, y, new Color(Mathf.Clamp01(baseGround.r + n), Mathf.Clamp01(baseGround.g + n), Mathf.Clamp01(baseGround.b + n), 1f));
                }
            }
        }
        tex.Apply();
        File.WriteAllBytes(filePath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    private static void GenerateVolcanoFloorTexture(string filePath)
    {
        int width = 1216, height = 768;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color baseGround = new Color(95f / 255f, 77f / 255f, 77f / 255f);
        Color darkRim = new Color(26f / 255f, 16f / 255f, 14f / 255f);
        Color edgeShadow = new Color(52f / 255f, 40f / 255f, 40f / 255f);
        Color clear = new Color(0, 0, 0, 0);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float d = CalcJaggedDist(x, y, width, height);
                if (d < 0f) tex.SetPixel(x, y, clear);
                else if (d < 2.0f) tex.SetPixel(x, y, darkRim);
                else if (d < 12.0f) tex.SetPixel(x, y, Color.Lerp(edgeShadow, baseGround, (d - 2f) / 10f));
                else
                {
                    float n = (Mathf.Sin(x * 0.015f + y * 0.012f) * 2.8f + Mathf.Cos(x * 0.035f - y * 0.028f) * 2.2f) / 255f;
                    tex.SetPixel(x, y, new Color(Mathf.Clamp01(baseGround.r + n), Mathf.Clamp01(baseGround.g + n * 0.8f), Mathf.Clamp01(baseGround.b + n * 0.8f), 1f));
                }
            }
        }
        tex.Apply();
        File.WriteAllBytes(filePath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    private static void GenerateVoidFloorTexture(string filePath)
    {
        int width = 1216, height = 768;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color baseGround = new Color(40f / 255f, 32f / 255f, 83f / 255f);
        Color darkRim = new Color(12f / 255f, 8f / 255f, 26f / 255f);
        Color edgeShadow = new Color(24f / 255f, 18f / 255f, 52f / 255f);
        Color clear = new Color(0, 0, 0, 0);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float d = CalcJaggedDist(x, y, width, height);
                if (d < 0f) tex.SetPixel(x, y, clear);
                else if (d < 2.0f) tex.SetPixel(x, y, darkRim);
                else if (d < 12.0f) tex.SetPixel(x, y, Color.Lerp(edgeShadow, baseGround, (d - 2f) / 10f));
                else
                {
                    float n = (Mathf.Sin(x * 0.015f + y * 0.012f) * 2.5f + Mathf.Cos(x * 0.035f - y * 0.028f) * 2.0f) / 255f;
                    tex.SetPixel(x, y, new Color(Mathf.Clamp01(baseGround.r + n * 0.8f), Mathf.Clamp01(baseGround.g + n * 0.6f), Mathf.Clamp01(baseGround.b + n * 1.2f), 1f));
                }
            }
        }
        tex.Apply();
        File.WriteAllBytes(filePath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    private static void GenerateOuterVoidTexture(string filePath)
    {
        int size = 256;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.black;
        tex.SetPixels(pixels);
        tex.Apply();
        File.WriteAllBytes(filePath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    private static void GenerateCosmicOuterVoidTexture(string filePath)
    {
        int size = 512;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color spaceBase = new Color(12f / 255f, 8f / 255f, 28f / 255f);
        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = spaceBase;
        tex.SetPixels(pixels);
        tex.Apply();
        File.WriteAllBytes(filePath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    #endregion

    private static void ConfigureSpriteImporter(string path, int ppu, TextureWrapMode wrapMode)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = ppu;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = wrapMode;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;

            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);

            importer.SaveAndReimport();
        }
    }

    private static void EnsureFolderExists(string relativePath)
    {
        if (!AssetDatabase.IsValidFolder(relativePath))
        {
            string parent = Path.GetDirectoryName(relativePath).Replace('\\', '/');
            string folderName = Path.GetFileName(relativePath);
            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolderExists(parent);
            }
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
