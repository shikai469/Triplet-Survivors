using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor Tool tự động tạo Asset đồ họa, Prefab và thiết lập Đấu Trường Map phong cách Brotato
/// vào GameplayScene của Triplet Survivors.
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

        Debug.Log("<color=yellow>[BROTATO MAP] Đang tạo bộ texture procedural chuẩn phong cách Brotato...</color>");

        string floorPath = Path.Combine(SPRITES_FOLDER, "BrotatoFloor.png");
        string wallHPath = Path.Combine(SPRITES_FOLDER, "BrotatoWallHorizontal.png");
        string wallVPath = Path.Combine(SPRITES_FOLDER, "BrotatoWallVertical.png");
        string cornerPath = Path.Combine(SPRITES_FOLDER, "BrotatoCornerPost.png");
        string voidPath = Path.Combine(SPRITES_FOLDER, "BrotatoOuterVoid.png");

        GenerateFloorTexture(floorPath);
        GenerateWallHorizontalTexture(wallHPath);
        GenerateWallVerticalTexture(wallVPath);
        GenerateCornerPostTexture(cornerPath);
        GenerateOuterVoidTexture(voidPath);

        AssetDatabase.Refresh();

        // Cấu hình import texture thành Sprite 2D
        ConfigureSpriteImporter(floorPath, PPU, TextureWrapMode.Clamp);
        ConfigureSpriteImporter(wallHPath, PPU, TextureWrapMode.Clamp);
        ConfigureSpriteImporter(wallVPath, PPU, TextureWrapMode.Clamp);
        ConfigureSpriteImporter(cornerPath, PPU, TextureWrapMode.Clamp);
        ConfigureSpriteImporter(voidPath, PPU, TextureWrapMode.Repeat);

        AssetDatabase.Refresh();

        Debug.Log("<color=green>[BROTATO MAP] Đã tạo thành công bộ Sprite Brotato! Đang tạo Prefab...</color>");
        CreateArenaPrefab();
    }

    [MenuItem("Tools/Brotato/2. Replace Original Map in GameplayScene")]
    public static void ReplaceMapInGameplayScene()
    {
        // 1. Đảm bảo Assets đã được tạo
        if (!File.Exists(PREFAB_PATH))
        {
            GenerateAllAssets();
        }

        // 2. Mở GameplayScene nếu chưa mở
        Scene currentScene = SceneManager.GetActiveScene();
        if (currentScene.path != SCENE_PATH)
        {
            currentScene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
        }

        Debug.Log("<color=cyan>[BROTATO MAP] Đang thay thế map gốc trong GameplayScene...</color>");

        // 3. Xử lý đối tượng InfiniteBackground cũ (thường là con của Main Camera)
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            Transform oldBg = mainCam.transform.Find("InfiniteBackground");
            if (oldBg != null)
            {
                Undo.DestroyObjectImmediate(oldBg.gameObject);
                Debug.Log("[BROTATO MAP] Đã gỡ bỏ InfiniteBackground cũ khỏi Main Camera.");
            }
        }

        // Tìm thêm bất kỳ InfiniteBackground nào khác trong scene (nếu có)
        InfiniteBackground[] oldBgs = Object.FindObjectsByType<InfiniteBackground>(FindObjectsSortMode.None);
        foreach (var bg in oldBgs)
        {
            Undo.DestroyObjectImmediate(bg.gameObject);
        }

        // 4. Xóa đối tượng BrotatoArenaMap cũ nếu đã tồn tại để cập nhật bản mới nhất
        BrotatoArena oldArena = Object.FindObjectsByType<BrotatoArena>(FindObjectsSortMode.None).Length > 0
            ? Object.FindObjectsByType<BrotatoArena>(FindObjectsSortMode.None)[0]
            : null;

        if (oldArena != null)
        {
            Undo.DestroyObjectImmediate(oldArena.gameObject);
        }

        // 5. Instantiate Prefab BrotatoArenaMap vào Scene
        GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
        if (prefabAsset == null)
        {
            Debug.LogError($"[BROTATO MAP] Không tìm thấy Prefab tại {PREFAB_PATH}");
            return;
        }

        GameObject arenaInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset);
        arenaInstance.transform.position = Vector3.zero;
        Undo.RegisterCreatedObjectUndo(arenaInstance, "Create Brotato Arena Map");

        // 6. Reset vị trí Player về tâm đấu trường (0, 0, 0)
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerObj.transform.position = Vector3.zero;
            Debug.Log("[BROTATO MAP] Đã đặt vị trí Player về trung tâm đấu trường (0, 0, 0).");
        }

        // 7. Cập nhật CameraFollow
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

        // 8. Đánh dấu scene dirty và lưu
        EditorSceneManager.MarkSceneDirty(currentScene);
        EditorSceneManager.SaveScene(currentScene);

        Debug.Log("<color=green>=========================================================</color>");
        Debug.Log("<color=green>[BROTATO MAP] HOÀN TẤT! ĐÃ THAY THẾ BẢN ĐỒ GỐC THÀNH CÔNG VỚI ĐẤU TRƯỜNG BROTATO!</color>");
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

        // 1. Outer Void (Nền tối bên ngoài đấu trường làm nổi bật sàn đấu)
        GameObject outerVoidObj = new GameObject("OuterVoid");
        outerVoidObj.transform.SetParent(root.transform, false);
        outerVoidObj.transform.localPosition = new Vector3(0, 0, 0);
        SpriteRenderer voidRenderer = outerVoidObj.AddComponent<SpriteRenderer>();
        voidRenderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Path.Combine(SPRITES_FOLDER, "BrotatoOuterVoid.png"));
        voidRenderer.drawMode = SpriteDrawMode.Tiled;
        voidRenderer.size = new Vector2(90f, 65f); // Bao phủ rộng rãi toàn bộ tầm nhìn xa
        voidRenderer.sortingOrder = -20;

        // 2. Arena Floor (Mặt sàn chính đấu trường)
        GameObject floorObj = new GameObject("Floor");
        floorObj.transform.SetParent(root.transform, false);
        floorObj.transform.localPosition = Vector3.zero;
        SpriteRenderer floorRenderer = floorObj.AddComponent<SpriteRenderer>();
        floorRenderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Path.Combine(SPRITES_FOLDER, "BrotatoFloor.png"));
        floorRenderer.sortingOrder = -10;
        arenaScript.floorRenderer = floorRenderer;

        // 3. Walls Root (Chứa 4 bờ tường rào chặn va chạm)
        GameObject wallsRoot = new GameObject("Walls");
        wallsRoot.transform.SetParent(root.transform, false);
        arenaScript.wallsRoot = wallsRoot.transform;

        Sprite wallHSprite = AssetDatabase.LoadAssetAtPath<Sprite>(Path.Combine(SPRITES_FOLDER, "BrotatoWallHorizontal.png"));
        Sprite wallVSprite = AssetDatabase.LoadAssetAtPath<Sprite>(Path.Combine(SPRITES_FOLDER, "BrotatoWallVertical.png"));

        float halfW = ARENA_WIDTH * 0.5f;     // 19
        float halfH = ARENA_HEIGHT * 0.5f;    // 12
        float wallCenterDistX = halfW + (WALL_THICKNESS * 0.5f); // 19 + 1 = 20
        float wallCenterDistY = halfH + (WALL_THICKNESS * 0.5f); // 12 + 1 = 13

        // Wall Top (Ngang)
        CreateWallObject("Wall_Top", wallsRoot.transform,
            new Vector3(0, wallCenterDistY, 0),
            new Vector2(ARENA_WIDTH + (WALL_THICKNESS * 2f), WALL_THICKNESS),
            wallHSprite, 15);

        // Wall Bottom (Ngang)
        CreateWallObject("Wall_Bottom", wallsRoot.transform,
            new Vector3(0, -wallCenterDistY, 0),
            new Vector2(ARENA_WIDTH + (WALL_THICKNESS * 2f), WALL_THICKNESS),
            wallHSprite, 15);

        // Wall Left (Dọc - dài 28 units để nối liền góc trên dưới)
        CreateWallObject("Wall_Left", wallsRoot.transform,
            new Vector3(-wallCenterDistX, 0, 0),
            new Vector2(WALL_THICKNESS, ARENA_HEIGHT + (WALL_THICKNESS * 2f)),
            wallVSprite, 15);

        // Wall Right (Dọc - dài 28 units để nối liền góc trên dưới)
        CreateWallObject("Wall_Right", wallsRoot.transform,
            new Vector3(wallCenterDistX, 0, 0),
            new Vector2(WALL_THICKNESS, ARENA_HEIGHT + (WALL_THICKNESS * 2f)),
            wallVSprite, 15);

        // 4. Corners Root (4 trụ cột mốc đặt chuẩn xác tại 4 góc giao nhau của tường)
        GameObject cornersRoot = new GameObject("Corners");
        cornersRoot.transform.SetParent(root.transform, false);
        arenaScript.cornersRoot = cornersRoot.transform;

        Sprite cornerSprite = AssetDatabase.LoadAssetAtPath<Sprite>(Path.Combine(SPRITES_FOLDER, "BrotatoCornerPost.png"));
        CreateCornerObject("Corner_TL", cornersRoot.transform, new Vector3(-wallCenterDistX, wallCenterDistY, 0), cornerSprite);
        CreateCornerObject("Corner_TR", cornersRoot.transform, new Vector3(wallCenterDistX, wallCenterDistY, 0), cornerSprite);
        CreateCornerObject("Corner_BL", cornersRoot.transform, new Vector3(-wallCenterDistX, -wallCenterDistY, 0), cornerSprite);
        CreateCornerObject("Corner_BR", cornersRoot.transform, new Vector3(wallCenterDistX, -wallCenterDistY, 0), cornerSprite);

        // Lưu Prefab
        PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
        Object.DestroyImmediate(root);

        Debug.Log($"[BROTATO MAP] Đã tạo Prefab thành công tại: {PREFAB_PATH}");
    }

    private static void CreateWallObject(string name, Transform parent, Vector3 localPos, Vector2 size, Sprite sprite, int sortingOrder)
    {
        GameObject wallObj = new GameObject(name);
        wallObj.transform.SetParent(parent, false);
        wallObj.transform.localPosition = localPos;
        wallObj.layer = LayerMask.NameToLayer("Default");

        SpriteRenderer sr = wallObj.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = sortingOrder;

        BoxCollider2D collider = wallObj.AddComponent<BoxCollider2D>();
        collider.size = size;
        collider.offset = Vector2.zero;
    }

    private static void CreateCornerObject(string name, Transform parent, Vector3 localPos, Sprite sprite)
    {
        GameObject cornerObj = new GameObject(name);
        cornerObj.transform.SetParent(parent, false);
        cornerObj.transform.localPosition = localPos;

        SpriteRenderer sr = cornerObj.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 18; // Nằm trên tường một chút để tạo độ sâu
    }

    #region Procedural Texture Generators

    /// <summary>
    /// Tạo texture mặt sàn đấu trường đặc trưng phong cách Brotato:
    /// Nền đất ấm màu be nâu, các hạt sỏi mịn, vạch ranh giới đấu trường hazard bo xung quanh,
    /// hoa văn nứt đất nhẹ và viền bóng đổ.
    /// </summary>
    private static void GenerateFloorTexture(string filePath)
    {
        int width = Mathf.RoundToInt(ARENA_WIDTH * PPU);   // 38 * 32 = 1216
        int height = Mathf.RoundToInt(ARENA_HEIGHT * PPU); // 24 * 32 = 768

        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);

        Color baseDirt = new Color(0.76f, 0.58f, 0.38f);      // Đất cát ấm sáng (#C29461)
        Color midDirt = new Color(0.68f, 0.50f, 0.32f);       // Đất trung tính (#AE8052)
        Color darkDirt = new Color(0.55f, 0.38f, 0.22f);      // Đất bóng râm (#8C6138)
        Color hazardLine = new Color(0.65f, 0.22f, 0.16f);    // Vạch đỏ ranh giới (#A63829)
        Color hazardGlow = new Color(0.92f, 0.80f, 0.55f);    // Vạch vàng cát (#EBCB8C)
        Color pebbleColor = new Color(0.42f, 0.34f, 0.28f);   // Sỏi đá vụn (#6B5747)

        int borderMargin = PPU; // Cách mép 1 unit (32 pixels)

        for (int y = 0; y < height; y++)
        {
            float ny = (float)y / height;
            for (int x = 0; x < width; x++)
            {
                float nx = (float)x / width;

                // 1. Nhiễu Perlin nhiều tầng tạo kết cấu bề mặt đất cát tự nhiên
                float n1 = Mathf.PerlinNoise(nx * 8f, ny * 6f);
                float n2 = Mathf.PerlinNoise(nx * 24f + 3.1f, ny * 18f + 5.7f) * 0.3f;
                float n3 = Mathf.PerlinNoise(nx * 60f + 12.3f, ny * 45f + 9.1f) * 0.15f;
                float noiseVal = n1 + n2 + n3;

                // 2. Trộn màu đất chính
                Color c = Color.Lerp(midDirt, baseDirt, noiseVal);

                // 3. Vignette nhẹ ở 4 góc và viền sát mép sàn để tạo cảm giác sâu
                float distFromCenter = Vector2.Distance(new Vector2(nx, ny), new Vector2(0.5f, 0.5f));
                c = Color.Lerp(c, darkDirt, Mathf.Clamp01((distFromCenter - 0.35f) * 0.8f));

                // 4. Lưới ô mờ nhẹ (Grid tiles mỗi 1 unit = 32px) như sàn đấu Brotato
                if (x % PPU == 0 || y % PPU == 0)
                {
                    c = Color.Lerp(c, darkDirt, 0.12f);
                }

                // 5. Vạch ranh giới đấu trường (Arena Hazard Boundary) cách bờ tường 1 unit
                bool onHazardRed = (x >= borderMargin && x <= borderMargin + 5) ||
                                  (x <= width - borderMargin && x >= width - borderMargin - 5) ||
                                  (y >= borderMargin && y <= borderMargin + 5) ||
                                  (y <= height - borderMargin && y >= height - borderMargin - 5);

                bool onHazardInnerGold = (x == borderMargin + 8 || x == width - borderMargin - 8 ||
                                         y == borderMargin + 8 || y == height - borderMargin - 8);

                if (onHazardRed && x >= borderMargin && x <= width - borderMargin && y >= borderMargin && y <= height - borderMargin)
                {
                    // Vạch nét đứt phong cách cảnh báo hazard
                    int dash = ((x + y) / 12) % 2;
                    c = dash == 0 ? hazardLine : hazardGlow;
                }
                else if (onHazardInnerGold && x >= borderMargin + 8 && x <= width - borderMargin - 8 && y >= borderMargin + 8 && y <= height - borderMargin - 8)
                {
                    c = Color.Lerp(c, hazardGlow, 0.5f);
                }

                // 6. Điểm xuyết các đốm sỏi cát ngẫu nhiên
                float pebbleNoise = Mathf.PerlinNoise(nx * 120f + 55f, ny * 120f + 88f);
                if (pebbleNoise > 0.82f && !onHazardRed)
                {
                    c = Color.Lerp(c, pebbleColor, 0.6f);
                }

                tex.SetPixel(x, y, c);
            }
        }

        tex.Apply();
        File.WriteAllBytes(filePath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    /// <summary>
    /// Tạo texture tường ngang (Top & Bottom) với rào chắn đá bê tông và vạch sọc cảnh báo hazard
    /// </summary>
    private static void GenerateWallHorizontalTexture(string filePath)
    {
        int width = Mathf.RoundToInt((ARENA_WIDTH + WALL_THICKNESS * 2f) * PPU); // (38 + 4) * 32 = 1344
        int height = Mathf.RoundToInt(WALL_THICKNESS * PPU);                    // 2 * 32 = 64

        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);

        Color stoneDark = new Color(0.24f, 0.18f, 0.14f);       // Nền đá sẫm (#3D2E24)
        Color stoneMid = new Color(0.38f, 0.28f, 0.22f);        // Thân đá (#614738)
        Color stoneHighlight = new Color(0.55f, 0.42f, 0.33f);  // Gờ vát sáng (#8C6B54)
        Color hazardYellow = new Color(0.88f, 0.72f, 0.18f);    // Vàng sọc cảnh báo (#E0B82E)
        Color hazardBlack = new Color(0.15f, 0.12f, 0.10f);     // Đen sọc cảnh báo (#261F1A)
        Color shadowColor = new Color(0f, 0f, 0f, 0.55f);       // Bóng đổ chân tường

        for (int y = 0; y < height; y++)
        {
            float ny = (float)y / height;
            for (int x = 0; x < width; x++)
            {
                Color c = stoneMid;

                // Gờ vát trên cùng phản quang
                if (y >= height - 6)
                {
                    c = stoneHighlight;
                }
                // Dải vạch sọc cảnh báo Hazard ở giữa thân tường (từ y=18 đến y=46)
                else if (y >= 18 && y <= 46)
                {
                    int stripe = ((x + y) / 16) % 2;
                    c = stripe == 0 ? hazardYellow : hazardBlack;
                }
                // Thân đá phía dưới
                else if (y > 8)
                {
                    float noise = Mathf.PerlinNoise((float)x * 0.15f, (float)y * 0.15f);
                    c = Color.Lerp(stoneDark, stoneMid, noise);
                }
                // Chân tường bóng đổ mềm phủ xuống mặt sàn
                else
                {
                    float shadowAlpha = (8 - y) / 8f;
                    c = Color.Lerp(stoneDark, shadowColor, shadowAlpha);
                }

                // Đường rãnh ghép giữa các khối đá mỗi 64 pixels (2 units)
                if (x % 64 == 0 || x % 64 == 1)
                {
                    c = Color.Lerp(c, stoneDark, 0.7f);
                }

                tex.SetPixel(x, y, c);
            }
        }

        tex.Apply();
        File.WriteAllBytes(filePath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    /// <summary>
    /// Tạo texture tường dọc (Left & Right) phủ trọn 28 units để giao khít với tường ngang
    /// </summary>
    private static void GenerateWallVerticalTexture(string filePath)
    {
        int width = Mathf.RoundToInt(WALL_THICKNESS * PPU);                              // 2 * 32 = 64
        int height = Mathf.RoundToInt((ARENA_HEIGHT + WALL_THICKNESS * 2f) * PPU);       // 28 * 32 = 896

        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);

        Color stoneDark = new Color(0.24f, 0.18f, 0.14f);
        Color stoneMid = new Color(0.38f, 0.28f, 0.22f);
        Color stoneHighlight = new Color(0.55f, 0.42f, 0.33f);
        Color hazardYellow = new Color(0.88f, 0.72f, 0.18f);
        Color hazardBlack = new Color(0.15f, 0.12f, 0.10f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color c = stoneMid;

                // Gờ vát mép ngoài
                if (x <= 6 || x >= width - 6)
                {
                    c = stoneHighlight;
                }
                // Dải vạch sọc cảnh báo ở giữa
                else if (x >= 18 && x <= 46)
                {
                    int stripe = ((x + y) / 16) % 2;
                    c = stripe == 0 ? hazardYellow : hazardBlack;
                }
                else
                {
                    float noise = Mathf.PerlinNoise((float)x * 0.15f, (float)y * 0.15f);
                    c = Color.Lerp(stoneDark, stoneMid, noise);
                }

                // Rãnh đá ghép ngang mỗi 64 pixels
                if (y % 64 == 0 || y % 64 == 1)
                {
                    c = Color.Lerp(c, stoneDark, 0.7f);
                }

                tex.SetPixel(x, y, c);
            }
        }

        tex.Apply();
        File.WriteAllBytes(filePath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    /// <summary>
    /// Tạo texture Trụ Cột mốc 4 góc đấu trường (Corner Post)
    /// </summary>
    private static void GenerateCornerPostTexture(string filePath)
    {
        int size = Mathf.RoundToInt(WALL_THICKNESS * 1.8f * PPU); // ~115 px (3.6 units)
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float radius = size * 0.46f;

        Color pillarBase = new Color(0.28f, 0.20f, 0.15f);
        Color pillarTop = new Color(0.52f, 0.40f, 0.30f);
        Color lightBeacon = new Color(0.95f, 0.25f, 0.20f); // Đèn đỏ beacon trên đỉnh
        Color clear = new Color(0, 0, 0, 0);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist > radius)
                {
                    tex.SetPixel(x, y, clear);
                }
                else
                {
                    // Đèn tín hiệu ở tâm
                    if (dist < radius * 0.35f)
                    {
                        float beaconFactor = 1f - (dist / (radius * 0.35f));
                        tex.SetPixel(x, y, Color.Lerp(lightBeacon, Color.white, beaconFactor * 0.5f));
                    }
                    // Thân trụ đa giác
                    else
                    {
                        float angle = Mathf.Atan2(y - center.y, x - center.x);
                        float shade = (Mathf.Sin(angle * 4f) + 1f) * 0.5f;
                        Color c = Color.Lerp(pillarBase, pillarTop, shade);

                        // Viền ngoài
                        if (dist >= radius - 3f)
                        {
                            c = new Color(0.18f, 0.12f, 0.08f);
                        }
                        tex.SetPixel(x, y, c);
                    }
                }
            }
        }

        tex.Apply();
        File.WriteAllBytes(filePath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    /// <summary>
    /// Tạo texture nền hư không bao quanh đấu trường (Outer Void)
    /// Tông màu tím đen huyền bí, tương phản cao để tôn vinh sàn đấu chính rực rỡ
    /// </summary>
    private static void GenerateOuterVoidTexture(string filePath)
    {
        int size = 256;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

        Color voidBase = new Color(0.08f, 0.06f, 0.10f);     // Tím than tối (#14101A)
        Color voidPattern = new Color(0.12f, 0.09f, 0.15f);  // Đốm đá hư không (#1F1726)

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float n = Mathf.PerlinNoise((float)x / 32f, (float)y / 32f);
                Color c = Color.Lerp(voidBase, voidPattern, n);
                tex.SetPixel(x, y, c);
            }
        }

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
            importer.filterMode = FilterMode.Bilinear;
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
