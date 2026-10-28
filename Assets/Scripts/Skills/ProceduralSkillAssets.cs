using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tự động sinh Sprite (Fallback Visuals) bằng code cho toàn bộ Icon kỹ năng & Vũ khí trong game.
/// Đảm bảo nếu chưa có file Sprite vẽ sẵn, game vẫn hiển thị đẹp mắt, rõ ràng và KHÔNG BAO GIỜ bị lỗi thiếu Sprite.
/// </summary>
public static class ProceduralSkillAssets
{
    private static readonly Dictionary<string, Sprite> cachedSprites = new Dictionary<string, Sprite>();

    public static Sprite GetSkillIcon(UpgradeData.UpgradeType type)
    {
        string key = "Icon_" + type.ToString();
        if (cachedSprites.TryGetValue(key, out Sprite sp) && sp != null) return sp;

        Sprite generated = GenerateIcon(type);
        cachedSprites[key] = generated;
        return generated;
    }

    public static Sprite GetSprite(string name)
    {
        if (cachedSprites.TryGetValue(name, out Sprite sp) && sp != null) return sp;

        Sprite generated = GenerateCustomSprite(name);
        cachedSprites[name] = generated;
        return generated;
    }

    private static Sprite GenerateIcon(UpgradeData.UpgradeType type)
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color[] colors = new Color[size * size];

        Color bg = new Color(0.12f, 0.14f, 0.18f, 1f);
        Color border = new Color(0.3f, 0.35f, 0.45f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                if (x == 0 || x == size - 1 || y == 0 || y == size - 1)
                    colors[y * size + x] = border;
                else
                    colors[y * size + x] = bg;
            }
        }

        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);

        switch (type)
        {
            case UpgradeData.UpgradeType.DamageUp:
                DrawSwordShape(colors, size, new Color(1f, 0.25f, 0.25f, 1f));
                break;
            case UpgradeData.UpgradeType.PiercingShot:
                DrawSniperBeam(colors, size, new Color(1f, 0.85f, 0.2f, 1f));
                break;
            case UpgradeData.UpgradeType.Multishot:
                DrawMultiShotIcon(colors, size, new Color(0.2f, 0.8f, 1f, 1f));
                break;
            case UpgradeData.UpgradeType.BurstFire:
                DrawBurstFireIcon(colors, size, new Color(1f, 0.55f, 0.1f, 1f));
                break;
            case UpgradeData.UpgradeType.OrbitSwords:
                DrawOrbitSwordsIcon(colors, size, new Color(0.85f, 0.9f, 1f, 1f));
                break;
            case UpgradeData.UpgradeType.SeagullAlly:
                DrawBirdIcon(colors, size, new Color(0.95f, 0.95f, 1f, 1f));
                break;
            case UpgradeData.UpgradeType.GrenadeToss:
                DrawGrenadeIcon(colors, size, new Color(0.35f, 0.85f, 0.35f, 1f));
                break;
            case UpgradeData.UpgradeType.EnergyShield:
                DrawShieldIcon(colors, size, new Color(0.2f, 0.85f, 1f, 1f));
                break;
            case UpgradeData.UpgradeType.BouncingBoomerang:
                DrawBoomerangIcon(colors, size, new Color(0.95f, 0.75f, 0.3f, 1f));
                break;
            case UpgradeData.UpgradeType.SelectItem_Coin:
                DrawCoinIcon(colors, size, new Color(1f, 0.85f, 0.1f, 1f));
                break;
            case UpgradeData.UpgradeType.SelectItem_HealPack:
                DrawCrossIcon(colors, size, new Color(1f, 0.25f, 0.35f, 1f));
                break;
            default:
                DrawCircle(colors, size, center, 16f, new Color(0.9f, 0.7f, 0.2f, 1f));
                break;
        }

        tex.SetPixels(colors);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
    }

    private static Sprite GenerateCustomSprite(string name)
    {
        int w = 32;
        int h = 32;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color[] colors = new Color[w * h];

        // Mặc định trong suốt
        for (int i = 0; i < colors.Length; i++) colors[i] = Color.clear;

        switch (name)
        {
            case "SwordBlade":
                // Vẽ thanh kiếm dài nhọn
                DrawSwordBlade(colors, w, h);
                break;

            case "Seagull":
                // Vẽ chú chim hải âu 2 cánh xòe
                DrawSeagullSprite(colors, w, h);
                break;

            case "SunflowerSeed":
                // Đạn hạt hướng dương oval vàng
                DrawSunflowerSeed(colors, w, h);
                break;

            case "Grenade":
                // Quả lựu đạn xanh rêu tròn
                DrawGrenadeSprite(colors, w, h);
                break;

            case "Boomerang":
                // Boomerang cong
                DrawBoomerangSprite(colors, w, h);
                break;

            case "EnergyShieldCircle":
                // Vòng bảo vệ năng lượng mờ
                return GenerateShieldRing(64);

            default:
                DrawCircle(colors, w, new Vector2(w * 0.5f, h * 0.5f), 10f, Color.white);
                break;
        }

        tex.SetPixels(colors);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32f);
    }

    private static Sprite GenerateShieldRing(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color[] colors = new Color[size * size];

        Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
        float radius = size * 0.44f;
        float thickness = size * 0.08f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), c);
                float diff = Mathf.Abs(dist - radius);
                if (diff < thickness)
                {
                    float alpha = 1f - (diff / thickness);
                    colors[y * size + x] = new Color(0.2f, 0.85f, 1f, alpha * 0.85f);
                }
                else if (dist < radius)
                {
                    colors[y * size + x] = new Color(0.1f, 0.6f, 1f, 0.15f);
                }
                else
                {
                    colors[y * size + x] = Color.clear;
                }
            }
        }

        tex.SetPixels(colors);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    // --- CÁC HÀM VẼ HÌNH HỌC DẠNG PIXEL ---
    private static void DrawSwordShape(Color[] c, int size, Color color)
    {
        for (int y = 14; y < size - 14; y++)
        {
            int x = y; // Đường chéo
            SetPixelSafe(c, size, x, y, color);
            SetPixelSafe(c, size, x + 1, y, color);
            SetPixelSafe(c, size, x, y + 1, color);
            SetPixelSafe(c, size, x - 1, y, color);
        }
        // Chuôi kiếm
        for (int d = -5; d <= 5; d++)
        {
            SetPixelSafe(c, size, 22 + d, 22 - d, Color.yellow);
        }
    }

    private static void DrawSniperBeam(Color[] c, int size, Color color)
    {
        // Tia chớp xuyên thấu
        for (int x = 8; x < size - 8; x++)
        {
            SetPixelSafe(c, size, x, size / 2, color);
            SetPixelSafe(c, size, x, size / 2 + 1, color);
            SetPixelSafe(c, size, x, size / 2 - 1, color);
        }
        // Đầu mũi tên xuyên
        for (int i = 0; i < 8; i++)
        {
            SetPixelSafe(c, size, size - 8 - i, size / 2 + i, color);
            SetPixelSafe(c, size, size - 8 - i, size / 2 - i, color);
        }
    }

    private static void DrawMultiShotIcon(Color[] c, int size, Color color)
    {
        // 3 tia đạn rẽ quạt
        Vector2 start = new Vector2(10, size / 2);
        DrawLine(c, size, start, new Vector2(size - 12, size / 2), color);
        DrawLine(c, size, start, new Vector2(size - 14, size - 14), color);
        DrawLine(c, size, start, new Vector2(size - 14, 14), color);
    }

    private static void DrawBurstFireIcon(Color[] c, int size, Color color)
    {
        // 3 viên đạn nối tiếp
        DrawCircle(c, size, new Vector2(18, size / 2), 5f, color);
        DrawCircle(c, size, new Vector2(32, size / 2), 5f, color);
        DrawCircle(c, size, new Vector2(46, size / 2), 5f, color);
    }

    private static void DrawOrbitSwordsIcon(Color[] c, int size, Color color)
    {
        // 2 kiếm chéo nhau
        DrawSwordShape(c, size, color);
        for (int y = 14; y < size - 14; y++)
        {
            int x = size - y;
            SetPixelSafe(c, size, x, y, color);
            SetPixelSafe(c, size, x + 1, y, color);
        }
    }

    private static void DrawBirdIcon(Color[] c, int size, Color color)
    {
        // Vẽ cánh chim hải âu dang rộng
        for (int x = 12; x < size - 12; x++)
        {
            float norm = (x - 12) / (float)(size - 24);
            int y = (int)(size * 0.45f + Mathf.Sin(norm * Mathf.PI) * 14f);
            SetPixelSafe(c, size, x, y, color);
            SetPixelSafe(c, size, x, y + 1, color);
        }
        // Thân chim
        DrawCircle(c, size, new Vector2(size * 0.5f, size * 0.5f), 6f, color);
        // Mỏ vàng
        DrawCircle(c, size, new Vector2(size * 0.5f + 8, size * 0.5f), 3f, Color.yellow);
    }

    private static void DrawGrenadeIcon(Color[] c, int size, Color color)
    {
        DrawCircle(c, size, new Vector2(size * 0.5f, size * 0.45f), 14f, color);
        // Chốt nổ
        for (int y = (int)(size * 0.65f); y < size - 12; y++)
        {
            SetPixelSafe(c, size, size / 2, y, Color.yellow);
            SetPixelSafe(c, size, size / 2 - 1, y, Color.yellow);
        }
    }

    private static void DrawShieldIcon(Color[] c, int size, Color color)
    {
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        for (int y = 10; y < size - 10; y++)
        {
            for (int x = 14; x < size - 14; x++)
            {
                float dx = Mathf.Abs(x - center.x) / (size * 0.4f);
                float dy = (y - 10) / (float)(size - 20);
                if (dx * dx + (1f - dy) * 0.5f < 0.8f && dy > 0.1f)
                {
                    SetPixelSafe(c, size, x, y, color);
                }
            }
        }
    }

    private static void DrawBoomerangIcon(Color[] c, int size, Color color)
    {
        Vector2 corner = new Vector2(16, 16);
        DrawLine(c, size, corner, new Vector2(size - 16, 24), color, 3);
        DrawLine(c, size, corner, new Vector2(24, size - 16), color, 3);
    }

    private static void DrawCoinIcon(Color[] c, int size, Color color)
    {
        DrawCircle(c, size, new Vector2(size * 0.5f, size * 0.5f), 16f, color);
        DrawCircle(c, size, new Vector2(size * 0.5f, size * 0.5f), 12f, new Color(0.9f, 0.7f, 0.05f, 1f));
    }

    private static void DrawCrossIcon(Color[] c, int size, Color color)
    {
        // Chữ thập đỏ cứu thương
        int half = size / 2;
        int barW = 6;
        for (int y = 14; y < size - 14; y++)
        {
            for (int x = half - barW; x <= half + barW; x++)
            {
                SetPixelSafe(c, size, x, y, color);
            }
        }
        for (int x = 14; x < size - 14; x++)
        {
            for (int y = half - barW; y <= half + barW; y++)
            {
                SetPixelSafe(c, size, x, y, color);
            }
        }
    }

    private static void DrawSwordBlade(Color[] c, int w, int h)
    {
        Color steel = new Color(0.9f, 0.95f, 1f, 1f);
        Color hilt = new Color(0.9f, 0.75f, 0.1f, 1f);
        Color edge = new Color(0.6f, 0.75f, 0.9f, 1f);

        int mid = w / 2;
        for (int y = 6; y < h - 4; y++)
        {
            SetPixelSafe(c, w, mid, y, steel);
            SetPixelSafe(c, w, mid - 1, y, edge);
            SetPixelSafe(c, w, mid + 1, y, edge);
        }
        // Mũi nhọn
        SetPixelSafe(c, w, mid, h - 3, steel);
        SetPixelSafe(c, w, mid, h - 2, steel);

        // Chuôi kiếm
        for (int x = mid - 4; x <= mid + 4; x++) SetPixelSafe(c, w, x, 6, hilt);
        for (int y = 1; y <= 5; y++) SetPixelSafe(c, w, mid, y, new Color(0.4f, 0.25f, 0.1f, 1f));
    }

    private static void DrawSeagullSprite(Color[] c, int w, int h)
    {
        Color white = new Color(0.96f, 0.96f, 1f, 1f);
        Color grey = new Color(0.7f, 0.75f, 0.85f, 1f);
        Color beak = new Color(1f, 0.8f, 0.1f, 1f);

        int midX = w / 2;
        int midY = h / 2;

        // Thân & Cánh
        for (int x = 4; x < w - 4; x++)
        {
            float norm = (x - 4) / (float)(w - 8);
            int y = (int)(midY + Mathf.Sin(norm * Mathf.PI) * 8f);
            SetPixelSafe(c, w, x, y, white);
            SetPixelSafe(c, w, x, y - 1, grey);
        }

        DrawCircle(c, w, new Vector2(midX, midY), 4f, white);
        // Mỏ chim
        SetPixelSafe(c, w, midX + 5, midY, beak);
        SetPixelSafe(c, w, midX + 6, midY, beak);
    }

    private static void DrawSunflowerSeed(Color[] c, int w, int h)
    {
        Color seed = new Color(0.95f, 0.8f, 0.2f, 1f);
        Color border = new Color(0.5f, 0.35f, 0.1f, 1f);

        int midX = w / 2;
        int midY = h / 2;

        for (int y = midY - 6; y <= midY + 6; y++)
        {
            int span = (int)((1f - Mathf.Abs(y - midY) / 6f) * 4f);
            for (int x = midX - span; x <= midX + span; x++)
            {
                if (x == midX - span || x == midX + span || y == midY - 6 || y == midY + 6)
                    SetPixelSafe(c, w, x, y, border);
                else
                    SetPixelSafe(c, w, x, y, seed);
            }
        }
    }

    private static void DrawGrenadeSprite(Color[] c, int w, int h)
    {
        Color green = new Color(0.2f, 0.45f, 0.2f, 1f);
        Color pin = new Color(0.85f, 0.85f, 0.85f, 1f);

        DrawCircle(c, w, new Vector2(w * 0.5f, h * 0.45f), 7f, green);
        for (int y = (int)(h * 0.65f); y < h - 4; y++)
        {
            SetPixelSafe(c, w, w / 2, y, pin);
            SetPixelSafe(c, w, w / 2 + 1, y, pin);
        }
    }

    private static void DrawBoomerangSprite(Color[] c, int w, int h)
    {
        Color wood = new Color(0.85f, 0.55f, 0.25f, 1f);
        Vector2 pivot = new Vector2(8, 8);
        DrawLine(c, w, pivot, new Vector2(w - 6, 14), wood, 3);
        DrawLine(c, w, pivot, new Vector2(14, h - 6), wood, 3);
    }

    private static void DrawCircle(Color[] c, int size, Vector2 center, float radius, Color color)
    {
        int minX = Mathf.Max(0, (int)(center.x - radius));
        int maxX = Mathf.Min(size - 1, (int)(center.x + radius));
        int minY = Mathf.Max(0, (int)(center.y - radius));
        int maxY = Mathf.Min(size - 1, (int)(center.y + radius));

        float rSqr = radius * radius;
        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                if ((new Vector2(x, y) - center).sqrMagnitude <= rSqr)
                {
                    c[y * size + x] = color;
                }
            }
        }
    }

    private static void DrawLine(Color[] c, int size, Vector2 p1, Vector2 p2, Color color, int thickness = 2)
    {
        int steps = Mathf.Max((int)Vector2.Distance(p1, p2) * 2, 1);
        for (int i = 0; i <= steps; i++)
        {
            Vector2 p = Vector2.Lerp(p1, p2, i / (float)steps);
            int px = Mathf.RoundToInt(p.x);
            int py = Mathf.RoundToInt(p.y);
            for (int ox = -thickness / 2; ox <= thickness / 2; ox++)
            {
                for (int oy = -thickness / 2; oy <= thickness / 2; oy++)
                {
                    SetPixelSafe(c, size, px + ox, py + oy, color);
                }
            }
        }
    }

    private static void SetPixelSafe(Color[] c, int size, int x, int y, Color color)
    {
        if (x >= 0 && x < size && y >= 0 && y < size)
        {
            c[y * size + x] = color;
        }
    }
}
