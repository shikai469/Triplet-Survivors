#if UNITY_EDITOR
using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Graphics = System.Drawing.Graphics;
using Color = System.Drawing.Color;
using UIImage = UnityEngine.UI.Image;

/// <summary>
/// Editor Script: Tự động sinh bản đồ Candy Crush Map (900x1500)
/// Menu: TripleT > Generate CandyCrush Map (900x1500)
/// - Đọc chính xác tọa độ RectTransform 4 nút (Easy, Normal, Hard, Endless) quy đổi sang hệ pixel 900x1500 của Bg_Main
/// - Chia 4 tầng địa hình: Thảo nguyên -> Rừng tối -> Dung nham -> Hư không (chi tiết ở rìa ngoài)
/// - Vẽ con đường uốn lượn Bezier Candy Crush qua tâm 4 nút
/// - Vùng đệm tròn phẳng (+30px) tại 4 nút làm sạch chi tiết nền để nút nổi bật
/// - Tự động thiết lập Sprite (2D and UI) và gán vào Bg_Main & LevelSelectManager
/// </summary>
public static class CandyCrushMapGeneratorEditor
{
    private const string MAP_PATH = "Assets/Sprites/UI/CandyCrush_Map_900x1500.png";
    private const int MAP_WIDTH = 900;
    private const int MAP_HEIGHT = 1500;

    [MenuItem("TripleT/Generate CandyCrush Map (900x1500)", false, 10)]
    public static void GenerateCandyCrushMap()
    {
        // 1. Đảm bảo mở đúng LevelSelectScene nếu cần
        EnsureLevelSelectSceneOpen();

        // 2. Tìm Bg_Main & LevelPathContainer
        RectTransform bgMainRT = FindBgMainRectTransform();
        if (bgMainRT == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy GameObject 'Bg_Main' trong Scene!", "Đóng");
            return;
        }

        RectTransform containerRT = FindLevelPathContainer();
        if (containerRT == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy GameObject 'LevelPathContainer' trong Scene!", "Đóng");
            return;
        }

        // 3. Đọc chính xác vị trí và bán kính của 4 nút
        float buttonRadius = 75f;
        PointF pEasy = GetNodePixelPosition(bgMainRT, containerRT, "Easy", -150f, -550f, ref buttonRadius);
        PointF pNormal = GetNodePixelPosition(bgMainRT, containerRT, "Normal", 150f, -250f, ref buttonRadius);
        PointF pHard = GetNodePixelPosition(bgMainRT, containerRT, "Hard", -150f, 50f, ref buttonRadius);
        PointF pEndless = GetNodePixelPosition(bgMainRT, containerRT, "Endless", 150f, 350f, ref buttonRadius);

        float bufferRadius = buttonRadius + 30f; // Vùng đệm rộng hơn kích thước nút 30px

        Debug.Log($"[CandyCrushMapGenerator] Tọa độ pixel trên Bg_Main (900x1500): " +
                  $"Easy=({pEasy.X:F0}, {pEasy.Y:F0}), Normal=({pNormal.X:F0}, {pNormal.Y:F0}), " +
                  $"Hard=({pHard.X:F0}, {pHard.Y:F0}), Endless=({pEndless.X:F0}, {pEndless.Y:F0}), " +
                  $"BufferRadius={bufferRadius:F0}px");

        // 4. Sinh file ảnh 900x1500
        string fullPath = Path.Combine(Directory.GetCurrentDirectory(), MAP_PATH);
        string dir = Path.GetDirectoryName(fullPath);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        RenderMapImage(fullPath, pEasy, pNormal, pHard, pEndless, bufferRadius);
        Debug.Log($"[CandyCrushMapGenerator] Đã tạo file ảnh tại: {MAP_PATH}");

        // 5. Cấu hình Sprite (2D and UI) trong AssetDatabase
        AssetDatabase.ImportAsset(MAP_PATH, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = AssetImporter.GetAtPath(MAP_PATH) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        Sprite mapSprite = AssetDatabase.LoadAssetAtPath<Sprite>(MAP_PATH);
        if (mapSprite == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Không thể nạp Sprite sau khi import!", "Đóng");
            return;
        }

        // 6. Gán trực tiếp vào Source Image của Bg_Main
        UIImage bgImg = bgMainRT.GetComponent<UIImage>();
        if (bgImg != null)
        {
            Undo.RecordObject(bgImg, "Assign CandyCrush Map to Bg_Main");
            bgImg.sprite = mapSprite;
            bgImg.color = UnityEngine.Color.white;
            EditorUtility.SetDirty(bgImg);
        }

        // 7. Gán làm ảnh nền mặc định trong LevelSelectManager
        LevelSelectManager mgr = UnityEngine.Object.FindAnyObjectByType<LevelSelectManager>();
        if (mgr != null)
        {
            Undo.RecordObject(mgr, "Assign Default Map in LevelSelectManager");
            mgr.defaultMapSprite = mapSprite;
            if (mgr.bgMain == null && bgImg != null)
                mgr.bgMain = bgImg;
            EditorUtility.SetDirty(mgr);
        }

        // 8. Đánh dấu scene dirty và thông báo
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("Thành Công",
            "Đã tạo và gán bản đồ Candy Crush (900x1500) thành công!\n\n" +
            $"• File ảnh: {MAP_PATH}\n" +
            "• Đã cấu hình: Sprite (2D and UI)\n" +
            "• Đã gán Source Image của Bg_Main\n" +
            "• Đã đặt defaultMapSprite trong LevelSelectManager", "Tuyệt vời!");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SCENE & RECTTRANSFORM HELPERS
    // ─────────────────────────────────────────────────────────────────────────

    private static void EnsureLevelSelectSceneOpen()
    {
        Scene currentScene = EditorSceneManager.GetActiveScene();
        if (currentScene.name != "LevelSelectScene")
        {
            string scenePath = "Assets/Scenes/LevelSelectScene.unity";
            if (File.Exists(scenePath))
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    EditorSceneManager.OpenScene(scenePath);
                }
            }
        }
    }

    private static RectTransform FindBgMainRectTransform()
    {
        GameObject obj = GameObject.Find("Bg_Main");
        if (obj != null) return obj.GetComponent<RectTransform>();

        // Fallback: tìm qua Image
        UIImage[] imgs = UnityEngine.Object.FindObjectsByType<UIImage>();
        foreach (var img in imgs)
        {
            if (img.gameObject.name.Equals("Bg_Main", StringComparison.OrdinalIgnoreCase))
                return img.GetComponent<RectTransform>();
        }
        return null;
    }

    private static RectTransform FindLevelPathContainer()
    {
        GameObject obj = GameObject.Find("LevelPathContainer");
        if (obj != null) return obj.GetComponent<RectTransform>();

        LevelPathRenderer r = UnityEngine.Object.FindAnyObjectByType<LevelPathRenderer>();
        if (r != null) return r.GetComponent<RectTransform>();

        return null;
    }

    private static PointF GetNodePixelPosition(RectTransform bgMainRT, RectTransform containerRT,
        string nodeName, float defaultLocalX, float defaultLocalY, ref float outButtonRadius)
    {
        Transform nodeTransform = containerRT.Find(nodeName);

        // Fallback: tìm theo tên chứa hoặc LevelNode
        if (nodeTransform == null)
        {
            LevelNode[] allNodes = UnityEngine.Object.FindObjectsByType<LevelNode>();
            foreach (var n in allNodes)
            {
                if (n.gameObject.name.IndexOf(nodeName, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    nodeTransform = n.transform;
                    break;
                }
            }
        }

        if (nodeTransform != null)
        {
            RectTransform nodeRT = nodeTransform.GetComponent<RectTransform>();
            if (nodeRT != null)
            {
                if (nodeRT.rect.width > 20f)
                    outButtonRadius = nodeRT.rect.width * 0.5f;

                // Quy đổi tâm node sang tọa độ local của Bg_Main
                Vector3 worldCenter = nodeRT.TransformPoint(nodeRT.rect.center);
                Vector3 localInBg = bgMainRT.InverseTransformPoint(worldCenter);

                // Quy đổi sang pixel của ảnh 900x1500 (tọa độ Unity: 0 ở dưới)
                float unityPixelX = (localInBg.x - bgMainRT.rect.xMin) / bgMainRT.rect.width * MAP_WIDTH;
                float unityPixelY = (localInBg.y - bgMainRT.rect.yMin) / bgMainRT.rect.height * MAP_HEIGHT;

                // Quy đổi sang hệ GDI+ Bitmap (0 ở trên)
                float gdiX = Mathf.Clamp(unityPixelX, 50f, MAP_WIDTH - 50f);
                float gdiY = Mathf.Clamp(MAP_HEIGHT - unityPixelY, 50f, MAP_HEIGHT - 50f);
                return new PointF(gdiX, gdiY);
            }
        }

        // Fallback tính toán hình học trực tiếp:
        // Bg_Main ở (0, -100), Container ở (0, 0)
        float relX = defaultLocalX - 0f;
        float relY = defaultLocalY - (-100f);
        float fbPixelX = relX + (MAP_WIDTH * 0.5f);
        float fbPixelY = relY + (MAP_HEIGHT * 0.5f);
        float fbGdiY = MAP_HEIGHT - fbPixelY;
        return new PointF(fbPixelX, fbGdiY);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GDI+ VECTOR GRAPHICS RENDERING
    // ─────────────────────────────────────────────────────────────────────────

    private static void RenderMapImage(string path, PointF pEasy, PointF pNormal, PointF pHard, PointF pEndless, float bufferRadius)
    {
        int W = MAP_WIDTH;
        int H = MAP_HEIGHT;

        using (Bitmap bmp = new Bitmap(W, H, PixelFormat.Format32bppArgb))
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.Clear(Color.FromArgb(20, 20, 30));

            // 1. NỀN 4 TẦNG ĐỊA HÌNH (GDI+ Y: 0 ở đỉnh, 1500 ở đáy)
            // • Tier 4: Hư không (0 .. 375)
            // • Tier 3: Dung nham (375 .. 750)
            // • Tier 2: Rừng tối (750 .. 1125)
            // • Tier 1: Thảo nguyên (1125 .. 1500)

            using (LinearGradientBrush bVoid = new LinearGradientBrush(
                new Rectangle(0, 0, W, 400),
                Color.FromArgb(8, 4, 20),
                Color.FromArgb(24, 10, 48),
                LinearGradientMode.Vertical))
            {
                g.FillRectangle(bVoid, 0, 0, W, 375);
            }

            using (LinearGradientBrush bLava = new LinearGradientBrush(
                new Rectangle(0, 370, W, 385),
                Color.FromArgb(28, 14, 18),
                Color.FromArgb(42, 18, 16),
                LinearGradientMode.Vertical))
            {
                g.FillRectangle(bLava, 0, 375, W, 375);
            }

            using (LinearGradientBrush bForest = new LinearGradientBrush(
                new Rectangle(0, 745, W, 385),
                Color.FromArgb(14, 28, 24),
                Color.FromArgb(22, 48, 38),
                LinearGradientMode.Vertical))
            {
                g.FillRectangle(bForest, 0, 750, W, 375);
            }

            using (LinearGradientBrush bGrass = new LinearGradientBrush(
                new Rectangle(0, 1120, W, 385),
                Color.FromArgb(38, 74, 32),
                Color.FromArgb(58, 112, 46),
                LinearGradientMode.Vertical))
            {
                g.FillRectangle(bGrass, 0, 1125, W, 375);
            }

            // 2. CÁC ĐƯỜNG PHÂN RANH HỮU CƠ MỀM MẠI GIỮA CÁC TẦNG
            DrawOrganicBoundary(g, 1125, Color.FromArgb(32, 65, 30), Color.FromArgb(18, 40, 32), W);
            DrawOrganicBoundary(g, 750, Color.FromArgb(20, 38, 30), Color.FromArgb(35, 16, 16), W);
            DrawOrganicBoundary(g, 375, Color.FromArgb(32, 14, 16), Color.FromArgb(14, 8, 32), W);

            // 3. CHI TIẾT TRANG TRÍ CHỈ NẰM Ở RÌA NGOÀI (X < 180 hoặc X > 720)
            DrawGrasslandDecorations(g, W, 1125, 1500);
            DrawDarkForestDecorations(g, W, 750, 1125);
            DrawLavaDecorations(g, W, 375, 750);
            DrawVoidDecorations(g, W, 0, 375);

            // 4. CON ĐƯỜNG CANDY CRUSH BEZIER UỐN LƯỢN NỐI CHÍNH XÁC QUA TÂM 4 NÚT
            DrawCandyCrushRoad(g, pEasy, pNormal, pHard, pEndless);

            // 5. VÙNG ĐỆM TRÒN PHẲNG (+30px) LÀM TRỐNG CHI TIẾT ĐỂ 4 NÚT NỔI BẬT TUYỆT ĐỐI
            DrawPedestal(g, pEasy, bufferRadius, 1);
            DrawPedestal(g, pNormal, bufferRadius, 2);
            DrawPedestal(g, pHard, bufferRadius, 3);
            DrawPedestal(g, pEndless, bufferRadius, 4);

            // 6. NHÃN TÊN ĐỘ KHÓ TRÊN CÁC KHOẢNG TRỐNG BÊN CẠNH NÚT
            // Nút đầu (Endless ở bên phải -> khoảng trống bên trái): "VÔ HẠN"
            float xBadgeEndless = pEndless.X < W * 0.5f ? (pEndless.X + 340f) : (pEndless.X - 340f);
            DrawDifficultyBadge(g, xBadgeEndless, pEndless.Y, "VÔ HẠN", 4);

            // Nút tiếp dưới (Hard ở bên trái -> khoảng trống bên phải): "KHÓ"
            float xBadgeHard = pHard.X < W * 0.5f ? (pHard.X + 340f) : (pHard.X - 340f);
            DrawDifficultyBadge(g, xBadgeHard, pHard.Y, "KHÓ", 3);

            // Nút tiếp dưới (Normal ở bên phải -> khoảng trống bên trái): "BÌNH THƯỜNG"
            float xBadgeNormal = pNormal.X < W * 0.5f ? (pNormal.X + 340f) : (pNormal.X - 340f);
            DrawDifficultyBadge(g, xBadgeNormal, pNormal.Y, "BÌNH THƯỜNG", 2);

            // Nút dưới cùng (Easy ở bên trái -> khoảng trống bên phải): "DỄ"
            float xBadgeEasy = pEasy.X < W * 0.5f ? (pEasy.X + 340f) : (pEasy.X - 340f);
            DrawDifficultyBadge(g, xBadgeEasy, pEasy.Y, "DỄ", 1);

            // Vignette nhẹ ở viền ngoài cùng
            using (Pen pVignette = new Pen(Color.FromArgb(80, 5, 5, 15), 18f))
            {
                g.DrawRectangle(pVignette, 9, 9, W - 18, H - 18);
            }

            bmp.Save(path, ImageFormat.Png);
        }
    }

    private static void DrawOrganicBoundary(Graphics g, float midY, Color colBelow, Color colAbove, int W)
    {
        using (GraphicsPath p = new GraphicsPath())
        {
            p.StartFigure();
            p.AddLine(0, midY - 35, 0, midY + 45);
            p.AddBezier(0, midY + 45, W * 0.25f, midY + 65, W * 0.35f, midY - 20, W * 0.5f, midY + 15);
            p.AddBezier(W * 0.5f, midY + 15, W * 0.65f, midY + 50, W * 0.85f, midY - 25, W, midY + 35);
            p.AddLine(W, midY + 35, W, midY - 35);
            p.AddBezier(W, midY - 35, W * 0.75f, midY - 55, W * 0.6f, midY - 10, W * 0.5f, midY - 35);
            p.AddBezier(W * 0.5f, midY - 35, W * 0.35f, midY - 60, W * 0.15f, midY - 15, 0, midY - 35);
            p.CloseFigure();

            using (LinearGradientBrush b = new LinearGradientBrush(
                new PointF(0, midY - 45), new PointF(0, midY + 55),
                Color.FromArgb(160, colAbove), Color.FromArgb(160, colBelow)))
            {
                g.FillPath(b, p);
            }
        }
    }

    // ── Tier 1: Thảo nguyên (X < 180 hoặc X > 720) ──────────────────────────
    private static void DrawGrasslandDecorations(Graphics g, int W, float topY, float botY)
    {
        System.Random rnd = new System.Random(101);

        for (int side = 0; side < 2; side++)
        {
            for (int i = 0; i < 4; i++)
            {
                float cy = topY + 40 + i * 85;
                float rx = 100 + rnd.Next(60);
                float ry = 40 + rnd.Next(25);
                float cx = (side == 0) ? (rx * 0.4f) : (W - rx * 0.4f);

                using (SolidBrush b = new SolidBrush(Color.FromArgb(140, 48 + rnd.Next(20), 98 + rnd.Next(25), 40)))
                {
                    g.FillEllipse(b, cx - rx, cy - ry, rx * 2, ry * 2);
                }
            }
        }

        for (int i = 0; i < 8; i++)
        {
            float tx = (i % 2 == 0) ? (30 + rnd.Next(110)) : (W - 30 - rnd.Next(110));
            float ty = topY + 50 + (i / 2) * 80 + rnd.Next(30);
            DrawStylizedPine(g, tx, ty, 28 + rnd.Next(16), 55 + rnd.Next(25),
                Color.FromArgb(28, 62, 28), Color.FromArgb(55, 120, 50));
        }

        for (int i = 0; i < 30; i++)
        {
            float fx = (rnd.Next(2) == 0) ? (20 + rnd.Next(130)) : (W - 20 - rnd.Next(130));
            float fy = topY + 20 + rnd.Next((int)(botY - topY - 40));
            Color flwCol = (i % 3 == 0) ? Color.FromArgb(220, 255, 230, 80) :
                           (i % 3 == 1) ? Color.FromArgb(220, 255, 255, 255) :
                                          Color.FromArgb(220, 255, 160, 180);
            using (SolidBrush b = new SolidBrush(flwCol))
            {
                g.FillEllipse(b, fx - 3, fy - 3, 6, 6);
            }
        }
    }

    // ── Tier 2: Rừng tối (X < 180 hoặc X > 720) ─────────────────────────────
    private static void DrawDarkForestDecorations(Graphics g, int W, float topY, float botY)
    {
        System.Random rnd = new System.Random(202);

        for (int side = 0; side < 2; side++)
        {
            float x0 = (side == 0) ? 0 : W - 180;
            using (LinearGradientBrush b = new LinearGradientBrush(
                new RectangleF(x0, topY, 180, botY - topY),
                Color.FromArgb(side == 0 ? 60 : 0, 20, 60, 50),
                Color.FromArgb(side == 0 ? 0 : 60, 10, 45, 40),
                LinearGradientMode.Horizontal))
            {
                g.FillRectangle(b, x0, topY, 180, botY - topY);
            }
        }

        for (int i = 0; i < 10; i++)
        {
            float tx = (i % 2 == 0) ? (25 + rnd.Next(125)) : (W - 25 - rnd.Next(125));
            float ty = topY + 40 + (i / 2) * 65 + rnd.Next(25);
            DrawStylizedPine(g, tx, ty, 32 + rnd.Next(14), 70 + rnd.Next(30),
                Color.FromArgb(10, 22, 18), Color.FromArgb(24, 55, 45));
        }

        for (int i = 0; i < 12; i++)
        {
            float mx = (rnd.Next(2) == 0) ? (35 + rnd.Next(110)) : (W - 35 - rnd.Next(110));
            float my = topY + 40 + rnd.Next((int)(botY - topY - 70));

            using (GraphicsPath p = new GraphicsPath())
            {
                p.AddEllipse(mx - 18, my - 18, 36, 36);
                using (PathGradientBrush pgb = new PathGradientBrush(p))
                {
                    pgb.CenterColor = Color.FromArgb(120, 80, 240, 210);
                    pgb.SurroundColors = new Color[] { Color.FromArgb(0, 80, 240, 210) };
                    g.FillPath(pgb, p);
                }
            }

            using (SolidBrush b = new SolidBrush(Color.FromArgb(240, 100, 255, 230)))
            {
                g.FillPie(b, mx - 8, my - 8, 16, 16, 180, 180);
            }
        }
    }

    // ── Tier 3: Dung nham (X < 180 hoặc X > 720) ────────────────────────────
    private static void DrawLavaDecorations(Graphics g, int W, float topY, float botY)
    {
        System.Random rnd = new System.Random(303);

        for (int side = 0; side < 2; side++)
        {
            for (int k = 0; k < 4; k++)
            {
                float sy = topY + 40 + k * 85 + rnd.Next(25);
                float sx = (side == 0) ? 0 : W;
                float ex = (side == 0) ? (60 + rnd.Next(80)) : (W - 60 - rnd.Next(80));
                float ey = sy + rnd.Next(-30, 30);
                float mx = (sx + ex) * 0.5f + rnd.Next(-20, 20);
                float my = (sy + ey) * 0.5f + rnd.Next(-25, 25);

                using (Pen penGlow = new Pen(Color.FromArgb(100, 255, 75, 10), 16f))
                {
                    penGlow.StartCap = LineCap.Round;
                    penGlow.EndCap = LineCap.Round;
                    g.DrawCurve(penGlow, new PointF[] { new PointF(sx, sy), new PointF(mx, my), new PointF(ex, ey) });
                }
                using (Pen penCore = new Pen(Color.FromArgb(240, 255, 220, 80), 4f))
                {
                    penCore.StartCap = LineCap.Round;
                    penCore.EndCap = LineCap.Round;
                    g.DrawCurve(penCore, new PointF[] { new PointF(sx, sy), new PointF(mx, my), new PointF(ex, ey) });
                }
            }
        }

        for (int i = 0; i < 8; i++)
        {
            float rx = (i % 2 == 0) ? (20 + rnd.Next(110)) : (W - 20 - rnd.Next(110));
            float ry = topY + 40 + (i / 2) * 80 + rnd.Next(30);

            PointF[] rockPts = new PointF[]
            {
                new PointF(rx - 20, ry + 35),
                new PointF(rx - 8, ry - 30),
                new PointF(rx + 6, ry - 42),
                new PointF(rx + 22, ry + 35)
            };
            using (SolidBrush b = new SolidBrush(Color.FromArgb(18, 10, 12)))
            {
                g.FillPolygon(b, rockPts);
            }
            using (Pen penEdge = new Pen(Color.FromArgb(70, 200, 60, 20), 2f))
            {
                g.DrawPolygon(penEdge, rockPts);
            }
        }

        for (int i = 0; i < 25; i++)
        {
            float ex = (rnd.Next(2) == 0) ? (15 + rnd.Next(140)) : (W - 15 - rnd.Next(140));
            float ey = topY + 15 + rnd.Next((int)(botY - topY - 30));
            using (SolidBrush b = new SolidBrush(Color.FromArgb(220, 255, 140 + rnd.Next(80), 20)))
            {
                g.FillEllipse(b, ex - 2, ey - 2, 4 + rnd.Next(3), 4 + rnd.Next(3));
            }
        }
    }

    // ── Tier 4: Hư không (X < 180 hoặc X > 720) ─────────────────────────────
    private static void DrawVoidDecorations(Graphics g, int W, float topY, float botY)
    {
        System.Random rnd = new System.Random(404);

        DrawNebulaPatch(g, 70, 120, 150, Color.FromArgb(70, 160, 40, 210));
        DrawNebulaPatch(g, W - 70, 140, 160, Color.FromArgb(70, 40, 140, 220));
        DrawNebulaPatch(g, 110, 280, 130, Color.FromArgb(60, 60, 20, 140));

        for (int i = 0; i < 6; i++)
        {
            float cx = (i % 2 == 0) ? (35 + rnd.Next(110)) : (W - 35 - rnd.Next(110));
            float cy = topY + 45 + (i / 2) * 100 + rnd.Next(35);
            DrawCrystalShard(g, cx, cy, 14 + rnd.Next(8), 35 + rnd.Next(15));
        }

        for (int i = 0; i < 60; i++)
        {
            float sx = (rnd.Next(3) == 0) ? (15 + rnd.Next(150)) :
                       (rnd.Next(2) == 0) ? (W - 15 - rnd.Next(150)) :
                                            (180 + rnd.Next(W - 360));
            float sy = topY + 10 + rnd.Next((int)(botY - topY - 20));

            int starSize = (i < 8) ? 4 : (i < 25) ? 3 : 2;
            int a = 140 + rnd.Next(115);
            using (SolidBrush b = new SolidBrush(Color.FromArgb(a, 240, 245, 255)))
            {
                g.FillEllipse(b, sx - starSize * 0.5f, sy - starSize * 0.5f, starSize, starSize);
                if (starSize >= 4)
                {
                    using (Pen p = new Pen(Color.FromArgb(a / 2, 220, 235, 255), 1.2f))
                    {
                        g.DrawLine(p, sx - 9, sy, sx + 9, sy);
                        g.DrawLine(p, sx, sy - 9, sx, sy + 9);
                    }
                }
            }
        }
    }

    private static void DrawStylizedPine(Graphics g, float cx, float cy, float w, float h, Color colDark, Color colLight)
    {
        using (SolidBrush brTrunk = new SolidBrush(Color.FromArgb(30, 20, 15)))
        {
            g.FillRectangle(brTrunk, cx - w * 0.15f, cy + h * 0.35f, w * 0.3f, h * 0.45f);
        }

        for (int t = 2; t >= 0; t--)
        {
            float tierY = cy + (t - 1) * (h * 0.28f);
            float tierW = w * (0.6f + t * 0.2f);
            float tierH = h * 0.38f;

            PointF[] pts = new PointF[]
            {
                new PointF(cx, tierY - tierH * 0.6f),
                new PointF(cx + tierW * 0.5f, tierY + tierH * 0.4f),
                new PointF(cx, tierY + tierH * 0.25f),
                new PointF(cx - tierW * 0.5f, tierY + tierH * 0.4f)
            };

            using (LinearGradientBrush b = new LinearGradientBrush(
                new RectangleF(cx - tierW * 0.5f, tierY - tierH * 0.6f, tierW, tierH),
                colLight, colDark, LinearGradientMode.Vertical))
            {
                g.FillPolygon(b, pts);
            }
        }
    }

    private static void DrawNebulaPatch(Graphics g, float cx, float cy, float r, Color glowColor)
    {
        using (GraphicsPath p = new GraphicsPath())
        {
            p.AddEllipse(cx - r, cy - r, r * 2, r * 2);
            using (PathGradientBrush pgb = new PathGradientBrush(p))
            {
                pgb.CenterColor = glowColor;
                pgb.SurroundColors = new Color[] { Color.FromArgb(0, glowColor) };
                g.FillPath(pgb, p);
            }
        }
    }

    private static void DrawCrystalShard(Graphics g, float cx, float cy, float w, float h)
    {
        PointF[] pts = new PointF[]
        {
            new PointF(cx, cy - h * 0.5f),
            new PointF(cx + w * 0.5f, cy - h * 0.1f),
            new PointF(cx + w * 0.35f, cy + h * 0.5f),
            new PointF(cx, cy + h * 0.4f),
            new PointF(cx - w * 0.35f, cy + h * 0.5f),
            new PointF(cx - w * 0.5f, cy - h * 0.1f)
        };

        using (LinearGradientBrush b = new LinearGradientBrush(
            new RectangleF(cx - w * 0.5f, cy - h * 0.5f, w, h),
            Color.FromArgb(220, 180, 100, 255),
            Color.FromArgb(240, 60, 20, 120),
            LinearGradientMode.Vertical))
        {
            g.FillPolygon(b, pts);
        }

        using (Pen p = new Pen(Color.FromArgb(180, 240, 200, 255), 1.5f))
        {
            g.DrawLine(p, cx, cy - h * 0.5f, cx, cy + h * 0.4f);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CANDY CRUSH ROAD (UỐN LƯỢN BEZIER QUA ĐÚNG TÂM 4 NÚT)
    // ─────────────────────────────────────────────────────────────────────────

    private static void DrawCandyCrushRoad(Graphics g, PointF pEasy, PointF pNormal, PointF pHard, PointF pEndless)
    {
        using (GraphicsPath path = new GraphicsPath())
        {
            path.StartFigure();

            // Segment 1: Easy -> Normal
            path.AddBezier(pEasy, new PointF(pEasy.X, 1060), new PointF(pNormal.X, 1040), pNormal);

            // Segment 2: Normal -> Hard
            path.AddBezier(pNormal, new PointF(pNormal.X, 760), new PointF(pHard.X, 740), pHard);

            // Segment 3: Hard -> Endless
            path.AddBezier(pHard, new PointF(pHard.X, 460), new PointF(pEndless.X, 440), pEndless);

            // Layer 1: Rãnh bóng đổ tạo độ sâu
            using (Pen penShadow = new Pen(Color.FromArgb(90, 0, 0, 0), 76f))
            {
                penShadow.StartCap = LineCap.Round;
                penShadow.EndCap = LineCap.Round;
                penShadow.LineJoin = LineJoin.Round;
                g.DrawPath(penShadow, path);
            }

            // Layer 2: Viền đất đá
            using (Pen penBorder = new Pen(Color.FromArgb(255, 175, 135, 85), 58f))
            {
                penBorder.StartCap = LineCap.Round;
                penBorder.EndCap = LineCap.Round;
                penBorder.LineJoin = LineJoin.Round;
                g.DrawPath(penBorder, path);
            }

            // Layer 3: Mặt đường gạch vàng Candy Crush
            using (Pen penRoad = new Pen(Color.FromArgb(255, 248, 230, 185), 44f))
            {
                penRoad.StartCap = LineCap.Round;
                penRoad.EndCap = LineCap.Round;
                penRoad.LineJoin = LineJoin.Round;
                g.DrawPath(penRoad, path);
            }

            // Layer 4: Vệt sáng trên đá lát
            using (Pen penHighlight = new Pen(Color.FromArgb(255, 255, 244, 215), 32f))
            {
                penHighlight.StartCap = LineCap.Round;
                penHighlight.EndCap = LineCap.Round;
                penHighlight.LineJoin = LineJoin.Round;
                g.DrawPath(penHighlight, path);
            }

            // Layer 5: Vạch đứt trung tâm đặc trưng Candy Crush
            using (Pen penDashes = new Pen(Color.FromArgb(230, 255, 255, 255), 7f))
            {
                penDashes.DashStyle = DashStyle.Dash;
                penDashes.DashPattern = new float[] { 2.2f, 1.8f };
                penDashes.StartCap = LineCap.Round;
                penDashes.EndCap = LineCap.Round;
                penDashes.LineJoin = LineJoin.Round;
                g.DrawPath(penDashes, path);
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // VÙNG ĐỆM TRÒN PHẲNG (LÀM TRỐNG CHI TIẾT ĐỂ NÚT NỔI BẬT +30px)
    // ─────────────────────────────────────────────────────────────────────────

    private static void DrawPedestal(Graphics g, PointF center, float radius, int tier)
    {
        float x = center.X;
        float y = center.Y;

        Color glowColor;
        Color rimDark;
        Color rimLight;
        Color daisTop;
        Color daisBot;
        Color ringColor;

        if (tier == 1) // Easy (Thảo nguyên)
        {
            glowColor = Color.FromArgb(160, 255, 225, 90);
            rimDark   = Color.FromArgb(255, 130, 105, 60);
            rimLight  = Color.FromArgb(255, 235, 210, 150);
            daisTop   = Color.FromArgb(255, 252, 248, 235);
            daisBot   = Color.FromArgb(255, 240, 230, 205);
            ringColor = Color.FromArgb(140, 180, 150, 90);
        }
        else if (tier == 2) // Normal (Rừng tối)
        {
            glowColor = Color.FromArgb(170, 75, 235, 195);
            rimDark   = Color.FromArgb(255, 45, 95, 80);
            rimLight  = Color.FromArgb(255, 140, 225, 205);
            daisTop   = Color.FromArgb(255, 238, 250, 248);
            daisBot   = Color.FromArgb(255, 215, 238, 232);
            ringColor = Color.FromArgb(140, 60, 155, 135);
        }
        else if (tier == 3) // Hard (Dung nham)
        {
            glowColor = Color.FromArgb(180, 255, 140, 40);
            rimDark   = Color.FromArgb(255, 120, 45, 25);
            rimLight  = Color.FromArgb(255, 255, 175, 95);
            daisTop   = Color.FromArgb(255, 255, 244, 232);
            daisBot   = Color.FromArgb(255, 250, 222, 195);
            ringColor = Color.FromArgb(150, 210, 95, 45);
        }
        else // Endless (Hư không)
        {
            glowColor = Color.FromArgb(180, 210, 110, 255);
            rimDark   = Color.FromArgb(255, 80, 35, 125);
            rimLight  = Color.FromArgb(255, 205, 150, 255);
            daisTop   = Color.FromArgb(255, 248, 242, 255);
            daisBot   = Color.FromArgb(255, 232, 220, 248);
            ringColor = Color.FromArgb(150, 145, 85, 205);
        }

        // 1. Quầng sáng tỏa ra xung quanh bục
        float glowRadius = radius + 28f;
        using (GraphicsPath pGlow = new GraphicsPath())
        {
            pGlow.AddEllipse(x - glowRadius, y - glowRadius, glowRadius * 2, glowRadius * 2);
            using (PathGradientBrush pgb = new PathGradientBrush(pGlow))
            {
                pgb.CenterColor = glowColor;
                pgb.SurroundColors = new Color[] { Color.FromArgb(0, glowColor) };
                g.FillPath(pgb, pGlow);
            }
        }

        // 2. Bóng đổ đậm dưới đĩa đệm
        float shadowRadius = radius + 6f;
        using (SolidBrush brShadow = new SolidBrush(Color.FromArgb(100, 0, 0, 0)))
        {
            g.FillEllipse(brShadow, x - shadowRadius, y - shadowRadius + 5f, shadowRadius * 2, shadowRadius * 2);
        }

        // 3. Viền đá nổi bên ngoài (rộng hơn nút 30px)
        using (LinearGradientBrush brRim = new LinearGradientBrush(
            new RectangleF(x - radius, y - radius, radius * 2, radius * 2),
            rimLight, rimDark, LinearGradientMode.Vertical))
        {
            g.FillEllipse(brRim, x - radius, y - radius, radius * 2, radius * 2);
        }

        // 4. Bề mặt đĩa đệm phẳng hoàn toàn, sạch sẽ, sáng rõ để đặt nút
        float innerRadius = radius - 7f;
        using (LinearGradientBrush brDais = new LinearGradientBrush(
            new RectangleF(x - innerRadius, y - innerRadius, innerRadius * 2, innerRadius * 2),
            daisTop, daisBot, LinearGradientMode.Vertical))
        {
            g.FillEllipse(brDais, x - innerRadius, y - innerRadius, innerRadius * 2, innerRadius * 2);
        }

        // 5. Vòng chỉ định viền mép nút
        float socketRadius = 78f;
        using (Pen penSocket = new Pen(ringColor, 2.5f))
        {
            g.DrawEllipse(penSocket, x - socketRadius, y - socketRadius, socketRadius * 2, socketRadius * 2);
        }

        // 6. Ánh sáng vát cạnh (Bevel highlight)
        using (Pen penBevel = new Pen(Color.FromArgb(120, 255, 255, 255), 2f))
        {
            g.DrawArc(penBevel, x - innerRadius, y - innerRadius, innerRadius * 2, innerRadius * 2, 190, 160);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // NHÃN TÊN ĐỘ KHÓ (BADGE CANDY CRUSH TẠI KHOẢNG TRỐNG)
    // ─────────────────────────────────────────────────────────────────────────

    private static void DrawDifficultyBadge(Graphics g, float cx, float cy, string title, int tier)
    {
        float bw = 240f;
        float bh = 68f;
        float bx = cx - bw * 0.5f;
        float by = cy - bh * 0.5f;

        Color bgDark, bgLight, rimCol, textCol, glowCol;
        if (tier == 1) // Easy (Thảo nguyên)
        {
            bgDark  = Color.FromArgb(245, 18, 54, 24);
            bgLight = Color.FromArgb(255, 45, 95, 45);
            rimCol  = Color.FromArgb(255, 180, 230, 110);
            textCol = Color.FromArgb(255, 255, 245, 210);
            glowCol = Color.FromArgb(120, 100, 220, 80);
        }
        else if (tier == 2) // Normal (Rừng tối)
        {
            bgDark  = Color.FromArgb(245, 12, 38, 38);
            bgLight = Color.FromArgb(255, 25, 75, 70);
            rimCol  = Color.FromArgb(255, 110, 235, 210);
            textCol = Color.FromArgb(255, 235, 255, 250);
            glowCol = Color.FromArgb(120, 60, 210, 180);
        }
        else if (tier == 3) // Hard (Dung nham)
        {
            bgDark  = Color.FromArgb(245, 52, 14, 12);
            bgLight = Color.FromArgb(255, 98, 28, 20);
            rimCol  = Color.FromArgb(255, 255, 160, 60);
            textCol = Color.FromArgb(255, 255, 235, 210);
            glowCol = Color.FromArgb(140, 255, 90, 20);
        }
        else // Endless (Hư không)
        {
            bgDark  = Color.FromArgb(245, 28, 10, 48);
            bgLight = Color.FromArgb(255, 60, 22, 105);
            rimCol  = Color.FromArgb(255, 215, 150, 255);
            textCol = Color.FromArgb(255, 250, 235, 255);
            glowCol = Color.FromArgb(140, 180, 90, 255);
        }

        // Quầng sáng mềm bên ngoài
        for (int i = 0; i < 4; i++)
        {
            float gExp = i * 4f;
            int gAlpha = (4 - i) * 15;
            using (GraphicsPath pGlow = GetRoundedRectPath(bx - gExp, by - gExp, bw + gExp * 2, bh + gExp * 2, bh * 0.5f + gExp))
            using (Pen penGlow = new Pen(Color.FromArgb(gAlpha, glowCol), 4f))
            {
                g.DrawPath(penGlow, pGlow);
            }
        }

        // Bóng đổ đậm bên dưới
        using (GraphicsPath pShadow = GetRoundedRectPath(bx, by + 5f, bw, bh, bh * 0.5f))
        using (SolidBrush bShadow = new SolidBrush(Color.FromArgb(130, 0, 0, 0)))
        {
            g.FillPath(bShadow, pShadow);
        }

        // Thân badge hình viên nhộng (pill)
        using (GraphicsPath pBody = GetRoundedRectPath(bx, by, bw, bh, bh * 0.5f))
        {
            using (LinearGradientBrush bBody = new LinearGradientBrush(
                new PointF(cx, by), new PointF(cx, by + bh), bgLight, bgDark))
            {
                g.FillPath(bBody, pBody);
            }

            // Viền ánh kim
            using (Pen pRim = new Pen(rimCol, 3.5f))
            {
                g.DrawPath(pRim, pBody);
            }
        }

        // Họa tiết kim cương 2 bên
        DrawDiamond(g, bx + 16f, cy, 6f, rimCol);
        DrawDiamond(g, bx + bw - 16f, cy, 6f, rimCol);

        // Chữ tên độ khó
        using (System.Drawing.Font f = new System.Drawing.Font("Arial", 25f, System.Drawing.FontStyle.Bold))
        using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
        {
            using (SolidBrush bTxtShadow = new SolidBrush(Color.FromArgb(200, 0, 0, 0)))
            {
                g.DrawString(title, f, bTxtShadow, cx + 2f, cy + 2f, sf);
            }
            using (SolidBrush bTxt = new SolidBrush(textCol))
            {
                g.DrawString(title, f, bTxt, cx, cy, sf);
            }
        }
    }

    private static GraphicsPath GetRoundedRectPath(float x, float y, float w, float h, float r)
    {
        GraphicsPath p = new GraphicsPath();
        float d = r * 2f;
        p.AddArc(x, y, d, d, 180, 90);
        p.AddArc(x + w - d, y, d, d, 270, 90);
        p.AddArc(x + w - d, y + h - d, d, d, 0, 90);
        p.AddArc(x, y + h - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }

    private static void DrawDiamond(Graphics g, float cx, float cy, float r, Color col)
    {
        PointF[] pts = new PointF[]
        {
            new PointF(cx, cy - r),
            new PointF(cx + r * 0.8f, cy),
            new PointF(cx, cy + r),
            new PointF(cx - r * 0.8f, cy)
        };
        using (SolidBrush b = new SolidBrush(col))
        {
            g.FillPolygon(b, pts);
        }
    }
}
#endif
