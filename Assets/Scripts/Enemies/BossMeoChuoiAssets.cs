using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quản lý và tự động sinh Sprite Procedural cho toàn bộ kỹ năng của Boss Mèo Chuối:
/// Giọt nước mắt, Bẫy vỏ chuối, Quả chuối rơi từ trên trời, Ngôi sao choáng, Vòng cảnh báo AoE.
/// Đảm bảo chạy độc lập 100%, không bị phụ thuộc vào asset bên ngoài.
/// </summary>
public static class BossMeoChuoiAssets
{
    private static readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

    public static Sprite GetTearSprite()
    {
        if (spriteCache.TryGetValue("Tear", out Sprite sp) && sp != null) return sp;
        sp = GenerateTearSprite();
        spriteCache["Tear"] = sp;
        return sp;
    }

    public static Sprite GetBananaPeelSprite()
    {
        if (spriteCache.TryGetValue("BananaPeel", out Sprite sp) && sp != null) return sp;
        sp = GenerateBananaPeelSprite();
        spriteCache["BananaPeel"] = sp;
        return sp;
    }

    public static Sprite GetBananaMissileSprite()
    {
        if (spriteCache.TryGetValue("BananaMissile", out Sprite sp) && sp != null) return sp;
        sp = GenerateBananaMissileSprite();
        spriteCache["BananaMissile"] = sp;
        return sp;
    }

    public static Sprite GetDizzyStarSprite()
    {
        if (spriteCache.TryGetValue("DizzyStar", out Sprite sp) && sp != null) return sp;
        sp = GenerateDizzyStarSprite();
        spriteCache["DizzyStar"] = sp;
        return sp;
    }

    public static Sprite GetWarningCircleSprite()
    {
        if (spriteCache.TryGetValue("WarningCircle", out Sprite sp) && sp != null) return sp;
        sp = GenerateWarningCircleSprite();
        spriteCache["WarningCircle"] = sp;
        return sp;
    }

    // 1. Tạo Sprite Giọt Nước Mắt (Xanh lơ phát sáng)
    private static Sprite GenerateTearSprite()
    {
        int w = 24;
        int h = 32;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color[] colors = new Color[w * h];
        Vector2 center = new Vector2(w * 0.5f, h * 0.35f);
        float radius = w * 0.42f;

        Color blueCore = new Color(0.2f, 0.75f, 1f, 1f);
        Color blueBorder = new Color(0.05f, 0.4f, 0.9f, 1f);
        Color whiteShine = new Color(0.9f, 0.98f, 1f, 0.9f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int idx = y * w + x;
                colors[idx] = Color.clear;

                float distToCenter = Vector2.Distance(new Vector2(x, y), center);
                bool inDrop = false;

                // Phần đáy giọt nước hình tròn
                if (y <= center.y)
                {
                    if (distToCenter <= radius) inDrop = true;
                }
                else // Phần ngọn giọt nước vuốt nhọn lên trên
                {
                    float t = (y - center.y) / (h - center.y);
                    float currentMaxX = (radius * (1f - t));
                    if (Mathf.Abs(x - center.x) <= currentMaxX) inDrop = true;
                }

                if (inDrop)
                {
                    // Đốm sáng phản chiếu ở góc trên bên trái giọt nước
                    if (x >= center.x - 4 && x <= center.x - 1 && y >= center.y - 2 && y <= center.y + 5)
                    {
                        colors[idx] = whiteShine;
                    }
                    else if (distToCenter > radius - 1.5f || y >= h - 3)
                    {
                        colors[idx] = blueBorder;
                    }
                    else
                    {
                        colors[idx] = blueCore;
                    }
                }
            }
        }

        tex.SetPixels(colors);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32);
    }

    // 2. Tạo Sprite Vỏ Chuối Bẫy Vàng Nằm Dưới Đất
    private static Sprite GenerateBananaPeelSprite()
    {
        int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color[] colors = new Color[size * size];

        Color yellowCore = new Color(1f, 0.88f, 0.15f, 1f);
        Color yellowDark = new Color(0.85f, 0.65f, 0.05f, 1f);
        Color brownTip = new Color(0.35f, 0.18f, 0.05f, 1f);

        for (int i = 0; i < colors.Length; i++) colors[i] = Color.clear;

        // Vẽ 3 cánh vỏ chuối xòe ra các góc
        void DrawPeelFlap(Vector2 start, Vector2 ctrl, Vector2 end, float width)
        {
            int steps = 18;
            for (int s = 0; s <= steps; s++)
            {
                float t = s / (float)steps;
                // Bezier bậc 2
                Vector2 p = (1f - t) * (1f - t) * start + 2f * (1f - t) * t * ctrl + t * t * end;
                int px = Mathf.RoundToInt(p.x);
                int py = Mathf.RoundToInt(p.y);
                float curW = width * (1f - t * 0.5f);

                for (int dx = -Mathf.CeilToInt(curW); dx <= Mathf.CeilToInt(curW); dx++)
                {
                    for (int dy = -Mathf.CeilToInt(curW); dy <= Mathf.CeilToInt(curW); dy++)
                    {
                        int cx = px + dx;
                        int cy = py + dy;
                        if (cx >= 0 && cx < size && cy >= 0 && cy < size)
                        {
                            int idx = cy * size + cx;
                            if (t > 0.85f) colors[idx] = brownTip;
                            else if (dx == 0 && dy == 0) colors[idx] = yellowCore;
                            else if (colors[idx] != yellowCore) colors[idx] = yellowDark;
                        }
                    }
                }
            }
        }

        Vector2 mid = new Vector2(16, 12);
        // Cánh trái
        DrawPeelFlap(mid, new Vector2(8, 20), new Vector2(3, 16), 2.2f);
        // Cánh phải
        DrawPeelFlap(mid, new Vector2(24, 20), new Vector2(29, 16), 2.2f);
        // Cánh giữa cong xuống
        DrawPeelFlap(mid, new Vector2(16, 5), new Vector2(16, 2), 2.5f);

        tex.SetPixels(colors);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32);
    }

    // 3. Tạo Sprite Quả Chuối Lớn Rơi Từ Trên Trời
    private static Sprite GenerateBananaMissileSprite()
    {
        int w = 28;
        int h = 48;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color[] colors = new Color[w * h];

        Color yellow = new Color(1f, 0.86f, 0.1f, 1f);
        Color yellowShadow = new Color(0.85f, 0.65f, 0.05f, 1f);
        Color stemGreen = new Color(0.35f, 0.65f, 0.15f, 1f);
        Color brownBottom = new Color(0.3f, 0.15f, 0.05f, 1f);

        for (int i = 0; i < colors.Length; i++) colors[i] = Color.clear;

        for (int y = 0; y < h; y++)
        {
            float t = y / (float)h;
            // Thân chuối hơi cong cong nhẹ
            float curveX = Mathf.Sin(t * Mathf.PI) * 4f;
            float centerX = (w * 0.5f) + curveX;
            float width = Mathf.Sin(t * Mathf.PI) * 9f + 2f;

            for (int x = 0; x < w; x++)
            {
                if (Mathf.Abs(x - centerX) <= width)
                {
                    int idx = y * w + x;
                    if (y >= h - 6) colors[idx] = stemGreen; // Cuống xanh ở trên
                    else if (y <= 3) colors[idx] = brownBottom; // Đáy chuối màu sẫm
                    else if (x < centerX) colors[idx] = yellow;
                    else colors[idx] = yellowShadow;
                }
            }
        }

        tex.SetPixels(colors);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32);
    }

    // 4. Tạo Sprite Ngôi Sao Choáng Khi Đâm Vào Tường Lửa
    private static Sprite GenerateDizzyStarSprite()
    {
        int size = 24;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color[] colors = new Color[size * size];

        Color starGold = new Color(1f, 0.9f, 0.2f, 1f);
        Color starEdge = new Color(0.9f, 0.6f, 0.05f, 1f);

        for (int i = 0; i < colors.Length; i++) colors[i] = Color.clear;

        Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
        float rOuter = size * 0.45f;
        float rInner = rOuter * 0.42f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 pt = new Vector2(x + 0.5f, y + 0.5f) - c;
                float angle = Mathf.Atan2(pt.y, pt.x) * Mathf.Rad2Deg + 90f;
                if (angle < 0) angle += 360f;

                float step = 360f / 10f; // 5 cánh (5 đỉnh nhọn, 5 đáy lõm)
                int section = Mathf.FloorToInt(angle / step);
                float subT = (angle - section * step) / step;

                float r1 = (section % 2 == 0) ? rOuter : rInner;
                float r2 = (section % 2 == 0) ? rInner : rOuter;
                float maxR = Mathf.Lerp(r1, r2, subT);

                if (pt.magnitude <= maxR)
                {
                    int idx = y * size + x;
                    if (pt.magnitude > maxR - 1.2f) colors[idx] = starEdge;
                    else colors[idx] = starGold;
                }
            }
        }

        tex.SetPixels(colors);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32);
    }

    // 5. Tạo Sprite Vòng Tròn Cảnh Báo AoE Màu Đỏ
    private static Sprite GenerateWarningCircleSprite()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color[] colors = new Color[size * size];

        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float maxR = size * 0.48f;

        Color redOutline = new Color(1f, 0.15f, 0.15f, 0.9f);
        Color redFill = new Color(1f, 0.1f, 0.1f, 0.22f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int idx = y * size + x;
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist <= maxR)
                {
                    if (dist >= maxR - 2.5f) colors[idx] = redOutline;
                    else colors[idx] = redFill;
                }
                else
                {
                    colors[idx] = Color.clear;
                }
            }
        }

        tex.SetPixels(colors);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32);
    }
}

