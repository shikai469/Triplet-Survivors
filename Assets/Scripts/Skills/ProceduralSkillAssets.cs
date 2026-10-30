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

#if UNITY_EDITOR
        string assetName = MapTypeToAssetName(type);
        if (!string.IsNullOrEmpty(assetName))
        {
            Sprite loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Sprites/UI/Icons/{assetName}.png");
            if (loaded != null)
            {
                cachedSprites[key] = loaded;
                return loaded;
            }
        }
#endif

        Sprite generated = GenerateIcon(type);
        cachedSprites[key] = generated;
        return generated;
    }

    private static string MapTypeToAssetName(UpgradeData.UpgradeType type)
    {
        switch (type)
        {
            case UpgradeData.UpgradeType.DamageUp:          return "Icon_DamageUp";
            case UpgradeData.UpgradeType.PiercingShot:      return "Icon_PiercingShot";
            case UpgradeData.UpgradeType.Multishot:         return "Icon_Shotgun";
            case UpgradeData.UpgradeType.BurstFire:         return "Icon_MachineGun";
            case UpgradeData.UpgradeType.OrbitSwords:       return "Icon_Bat";
            case UpgradeData.UpgradeType.SeagullAlly:       return "Icon_Seagull";
            case UpgradeData.UpgradeType.GrenadeToss:       return "Icon_Grenade";
            case UpgradeData.UpgradeType.BouncingBoomerang: return "Icon_Boomerang";
            default: return null;
        }
    }

    /// <summary>
    /// Lấy icon theo upgradeKey string (dùng cho UpgradeScene slots).
    /// Ánh xạ: Dmg->DamageUp, Defense->EnergyShield, Health->HealPack, Healing->Heal,
    /// Extender->OrbitSwords, Speed->MoveSpeed, Magnet->MagnetRange, Luck->Coin,
    /// Growth->FireRate, Greed->Coin, CritRate->PiercingShot, CritDmg->BurstFire
    /// </summary>
    public static Sprite GetSkillIconByKey(string upgradeKey)
    {
        switch (upgradeKey)
        {
            case "Dmg":      return GetSkillIcon(UpgradeData.UpgradeType.DamageUp);
            case "Defense":  return GetSkillIcon(UpgradeData.UpgradeType.EnergyShield);
            case "Health":   return GetSkillIcon(UpgradeData.UpgradeType.SelectItem_HealPack);
            case "Healing":  return GetSkillIcon(UpgradeData.UpgradeType.Heal);
            case "Extender": return GetSkillIcon(UpgradeData.UpgradeType.OrbitSwords);
            case "Speed":    return GetSkillIcon(UpgradeData.UpgradeType.MoveSpeed);
            case "Magnet":   return GetSkillIcon(UpgradeData.UpgradeType.MagnetRange);
            case "Luck":     return GetSkillIcon(UpgradeData.UpgradeType.SelectItem_Coin);
            case "Growth":   return GetSkillIcon(UpgradeData.UpgradeType.FireRate);
            case "Greed":    return GetSkillIcon(UpgradeData.UpgradeType.SelectItem_Coin);
            case "CritRate": return GetSkillIcon(UpgradeData.UpgradeType.PiercingShot);
            case "CritDmg":  return GetSkillIcon(UpgradeData.UpgradeType.BurstFire);
            default:         return GetSkillIcon(UpgradeData.UpgradeType.DamageUp);
        }
    }

    public static Sprite GetSprite(string name)
    {
        if (cachedSprites.TryGetValue(name, out Sprite sp) && sp != null) return sp;

#if UNITY_EDITOR
        if (name == "SwordBlade" || name == "BaseballBat")
        {
            Sprite loaded = null;
            var subAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/weaponTrail.png");
            if (subAssets != null)
            {
                foreach (var obj in subAssets)
                {
                    if (obj is Sprite s) { loaded = s; break; }
                }
            }
            if (loaded == null)
            {
                loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Icons/Icon_Bat_Raw.png");
            }
            if (loaded != null)
            {
                cachedSprites[name] = loaded;
                return loaded;
            }
        }
#endif

        Sprite generated = GenerateCustomSprite(name);
        cachedSprites[name] = generated;
        return generated;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ICON GENERATOR — mỗi slot có nền màu huy hiệu riêng + biểu tượng rõ nét
    // ─────────────────────────────────────────────────────────────────────────

    private static Sprite GenerateIcon(UpgradeData.UpgradeType type)
    {
        const int size = 80;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color[] c = new Color[size * size];

        // ── Chọn màu nền theo loại ──────────────────────────────────────────
        Color bgTop, bgBot;
        switch (type)
        {
            // Slot_Dmg — đỏ đậm
            case UpgradeData.UpgradeType.DamageUp:
                bgTop = new Color(0.85f, 0.12f, 0.12f, 1f);
                bgBot = new Color(0.40f, 0.04f, 0.06f, 1f);
                break;
            // Slot_Defense — xanh dương
            case UpgradeData.UpgradeType.EnergyShield:
                bgTop = new Color(0.08f, 0.30f, 0.82f, 1f);
                bgBot = new Color(0.03f, 0.10f, 0.40f, 1f);
                break;
            // Slot_Health — đỏ tươi
            case UpgradeData.UpgradeType.SelectItem_HealPack:
                bgTop = new Color(0.90f, 0.15f, 0.20f, 1f);
                bgBot = new Color(0.45f, 0.04f, 0.08f, 1f);
                break;
            // Slot_Healing — xanh lá
            case UpgradeData.UpgradeType.Heal:
                bgTop = new Color(0.10f, 0.72f, 0.22f, 1f);
                bgBot = new Color(0.03f, 0.30f, 0.08f, 1f);
                break;
            // Slot_Extender / Vung Gậy — tím hoàng gia
            case UpgradeData.UpgradeType.OrbitSwords:
                bgTop = new Color(0.60f, 0.18f, 0.92f, 1f);
                bgBot = new Color(0.25f, 0.05f, 0.45f, 1f);
                break;
            // Slot_Speed — xanh lam điện
            case UpgradeData.UpgradeType.MoveSpeed:
                bgTop = new Color(0.10f, 0.55f, 0.95f, 1f);
                bgBot = new Color(0.03f, 0.18f, 0.50f, 1f);
                break;
            // Slot_Magnet — xanh đen navy
            case UpgradeData.UpgradeType.MagnetRange:
                bgTop = new Color(0.06f, 0.15f, 0.30f, 1f);
                bgBot = new Color(0.02f, 0.05f, 0.12f, 1f);
                break;
            // Slot_Luck — xanh lục tươi
            case UpgradeData.UpgradeType.SelectItem_Coin:
                bgTop = new Color(0.08f, 0.65f, 0.30f, 1f);
                bgBot = new Color(0.02f, 0.25f, 0.10f, 1f);
                break;
            // Slot_Growth / FireRate — tím than
            case UpgradeData.UpgradeType.FireRate:
                bgTop = new Color(0.35f, 0.08f, 0.55f, 1f);
                bgBot = new Color(0.12f, 0.02f, 0.22f, 1f);
                break;
            // Slot_Greed / Lựu Đạn — nâu vàng / olive
            case UpgradeData.UpgradeType.GrenadeToss:
                bgTop = new Color(0.75f, 0.55f, 0.08f, 1f);
                bgBot = new Color(0.35f, 0.24f, 0.02f, 1f);
                break;
            // Slot_CritRate / Đạn Xuyên Thấu — cam đậm
            case UpgradeData.UpgradeType.PiercingShot:
                bgTop = new Color(0.95f, 0.48f, 0.06f, 1f);
                bgBot = new Color(0.50f, 0.18f, 0.02f, 1f);
                break;
            // Slot_CritDmg / Súng Máy — đỏ sẫm cherry
            case UpgradeData.UpgradeType.BurstFire:
                bgTop = new Color(0.75f, 0.10f, 0.24f, 1f);
                bgBot = new Color(0.35f, 0.03f, 0.10f, 1f);
                break;
            // Multishot / Shotgun — xanh thép/teal
            case UpgradeData.UpgradeType.Multishot:
                bgTop = new Color(0.18f, 0.55f, 0.72f, 1f);
                bgBot = new Color(0.06f, 0.22f, 0.35f, 1f);
                break;
            // SeagullAlly — xanh da trời
            case UpgradeData.UpgradeType.SeagullAlly:
                bgTop = new Color(0.12f, 0.65f, 0.90f, 1f);
                bgBot = new Color(0.04f, 0.28f, 0.50f, 1f);
                break;
            // BouncingBoomerang — xanh ngọc lục bảo
            case UpgradeData.UpgradeType.BouncingBoomerang:
                bgTop = new Color(0.14f, 0.70f, 0.32f, 1f);
                bgBot = new Color(0.04f, 0.30f, 0.12f, 1f);
                break;
            default:
                bgTop = new Color(0.20f, 0.20f, 0.28f, 1f);
                bgBot = new Color(0.08f, 0.08f, 0.12f, 1f);
                break;
        }

        // ── Vẽ nền gradient từ dưới lên ────────────────────────────────────
        FillGradient(c, size, bgBot, bgTop);

        // ── Vẽ viền bo góc nhẹ ─────────────────────────────────────────────
        DrawRoundBorder(c, size, new Color(1f, 1f, 1f, 0.22f), 6);

        // ── Vẽ biểu tượng tương ứng ────────────────────────────────────────
        switch (type)
        {
            case UpgradeData.UpgradeType.DamageUp:
                // Súng lục (pistol) kèm mũi tên chỉ lên
                DrawPistolUp(c, size, Color.white);
                break;
            case UpgradeData.UpgradeType.EnergyShield:
                // Khiên heraldic viền đôi hướng xuống
                DrawShield(c, size, new Color(0.35f, 0.70f, 0.98f, 1f), Color.white);
                break;
            case UpgradeData.UpgradeType.SelectItem_HealPack:
                // Trái tim cân xứng viền trắng
                DrawHeart(c, size, new Color(1.00f, 0.30f, 0.42f, 1f), new Color(1f, 0.85f, 0.90f, 0.95f));
                break;
            case UpgradeData.UpgradeType.Heal:
                // Chữ thập y tế trắng to
                DrawMedicalCross(c, size, Color.white);
                break;
            case UpgradeData.UpgradeType.OrbitSwords:
                // Gậy bóng chày (Vung Gậy)
                DrawBaseballBat(c, size, new Color(0.95f, 0.72f, 0.45f, 1f));
                break;
            case UpgradeData.UpgradeType.MoveSpeed:
                // Giày sneaker / tia sét tốc độ
                DrawLightningBolt(c, size, Color.white);
                break;
            case UpgradeData.UpgradeType.MagnetRange:
                // Nam châm chữ U đỏ-trắng
                DrawMagnetU(c, size, new Color(1f, 0.25f, 0.25f, 1f), Color.white);
                break;
            case UpgradeData.UpgradeType.SelectItem_Coin:
                // Cỏ 4 lá xanh
                DrawFourLeafClover(c, size, new Color(0.50f, 1.00f, 0.40f, 1f));
                break;
            case UpgradeData.UpgradeType.FireRate:
                // Ngôi sao 5 cánh vàng
                DrawStar5(c, size, new Color(1.00f, 0.88f, 0.20f, 1f));
                break;
            case UpgradeData.UpgradeType.GrenadeToss:
                // Quả lựu đạn
                DrawGrenadeIcon(c, size, Color.white);
                break;
            case UpgradeData.UpgradeType.PiercingShot:
                // Súng ngắm (sniper rifle)
                DrawSniperRifle(c, size, Color.white);
                break;
            case UpgradeData.UpgradeType.BurstFire:
                // Súng máy / súng trường
                DrawAssaultRifle(c, size, Color.white);
                break;
            case UpgradeData.UpgradeType.Multishot:
                // Shotgun
                DrawShotgun(c, size, Color.white);
                break;
            case UpgradeData.UpgradeType.SeagullAlly:
                // Chim hải âu
                DrawSeagullBird(c, size, Color.white);
                break;
            case UpgradeData.UpgradeType.BouncingBoomerang:
                // Boomerang
                DrawBoomerangIcon(c, size, Color.white);
                break;
            default:
                FillCircle(c, size, new Vector2(size * .5f, size * .5f), size * 0.28f, new Color(0.9f, 0.7f, 0.2f, 1f));
                break;
        }

        tex.SetPixels(c);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ── DRAWING PRIMITIVES ────────────────────────────────────────────────────
    // ─────────────────────────────────────────────────────────────────────────

    static void FillGradient(Color[] c, int s, Color bot, Color top)
    {
        for (int y = 0; y < s; y++)
        {
            float t = y / (float)(s - 1);
            Color row = Color.Lerp(bot, top, t);
            for (int x = 0; x < s; x++)
                c[y * s + x] = row;
        }
    }

    static void DrawRoundBorder(Color[] c, int s, Color col, int radius)
    {
        for (int y = 0; y < s; y++)
        for (int x = 0; x < s; x++)
        {
            float cx = Mathf.Abs(x - s * .5f) - (s * .5f - radius);
            float cy = Mathf.Abs(y - s * .5f) - (s * .5f - radius);
            float sdf = Mathf.Sqrt(Mathf.Max(cx, 0) * Mathf.Max(cx, 0) + Mathf.Max(cy, 0) * Mathf.Max(cy, 0)) - radius;
            float a = Mathf.Clamp01(1f - sdf * 0.5f) * col.a;
            if (a > 0.01f)
                BlendPixel(c, s, x, y, new Color(col.r, col.g, col.b, a));
        }
    }

    static void FillCircle(Color[] c, int s, Vector2 center, float r, Color col)
    {
        int x0 = Mathf.Max(0, (int)(center.x - r));
        int x1 = Mathf.Min(s - 1, (int)(center.x + r));
        int y0 = Mathf.Max(0, (int)(center.y - r));
        int y1 = Mathf.Min(s - 1, (int)(center.y + r));
        float r2 = r * r;
        for (int y = y0; y <= y1; y++)
        for (int x = x0; x <= x1; x++)
        {
            float dx = x - center.x, dy = y - center.y;
            if (dx * dx + dy * dy <= r2)
                BlendPixel(c, s, x, y, col);
        }
    }

    static void FillRing(Color[] c, int s, Vector2 center, float r, float t, Color col)
    {
        float rOuter = r, rInner = r - t;
        for (int y = 0; y < s; y++)
        for (int x = 0; x < s; x++)
        {
            float dx = x - center.x, dy = y - center.y;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            if (dist <= rOuter && dist >= rInner)
            {
                float aa = Mathf.Clamp01(Mathf.Min(rOuter - dist, dist - rInner) * 1.5f);
                BlendPixel(c, s, x, y, new Color(col.r, col.g, col.b, col.a * aa));
            }
        }
    }

    static void ThickLine(Color[] c, int s, Vector2 p1, Vector2 p2, Color col, int thick = 5)
    {
        int steps = Mathf.Max((int)Vector2.Distance(p1, p2) * 3, 1);
        float half = thick * 0.5f;
        for (int i = 0; i <= steps; i++)
        {
            Vector2 p = Vector2.Lerp(p1, p2, i / (float)steps);
            FillCircle(c, s, p, half, col);
        }
    }

    static void BlendPixel(Color[] c, int s, int x, int y, Color col)
    {
        if (x < 0 || x >= s || y < 0 || y >= s) return;
        Color dst = c[y * s + x];
        float a = col.a;
        c[y * s + x] = new Color(
            dst.r * (1 - a) + col.r * a,
            dst.g * (1 - a) + col.g * a,
            dst.b * (1 - a) + col.b * a,
            Mathf.Max(dst.a, a));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ── SYMBOL DRAWING ────────────────────────────────────────────────────────
    // ─────────────────────────────────────────────────────────────────────────

    // 1. Súng lục kèm mũi tên chỉ lên (Tăng Sát Thương)
    static void DrawPistolUp(Color[] c, int s, Color col)
    {
        // Thân súng lục
        ThickLine(c, s, new Vector2(22, 46), new Vector2(54, 46), col, 8); // nòng
        ThickLine(c, s, new Vector2(28, 46), new Vector2(24, 26), col, 7); // báng
        ThickLine(c, s, new Vector2(32, 36), new Vector2(38, 36), col, 3); // cò
        // Mũi tên chỉ lên bên phải
        ThickLine(c, s, new Vector2(64, 28), new Vector2(64, 56), col, 4);
        ThickLine(c, s, new Vector2(56, 48), new Vector2(64, 58), col, 4);
        ThickLine(c, s, new Vector2(72, 48), new Vector2(64, 58), col, 4);
    }

    // 2. Súng ngắm (Đạn Xuyên Thấu)
    static void DrawSniperRifle(Color[] c, int s, Color col)
    {
        ThickLine(c, s, new Vector2(18, 20), new Vector2(64, 62), col, 5); // nòng chính
        ThickLine(c, s, new Vector2(16, 18), new Vector2(26, 18), col, 6); // báng
        // Ống ngắm (scope)
        ThickLine(c, s, new Vector2(34, 42), new Vector2(48, 54), col, 4);
        ThickLine(c, s, new Vector2(33, 40), new Vector2(37, 44), col, 2);
        ThickLine(c, s, new Vector2(45, 52), new Vector2(49, 56), col, 2);
    }

    // 3A. Shotgun
    static void DrawShotgun(Color[] c, int s, Color col)
    {
        ThickLine(c, s, new Vector2(18, 22), new Vector2(62, 60), col, 6);
        ThickLine(c, s, new Vector2(16, 20), new Vector2(24, 20), col, 7);
        ThickLine(c, s, new Vector2(36, 38), new Vector2(46, 46), col, 8); // pump
    }

    // 3B. Súng trường / Súng máy (Súng Máy)
    static void DrawAssaultRifle(Color[] c, int s, Color col)
    {
        ThickLine(c, s, new Vector2(18, 22), new Vector2(62, 60), col, 5);
        ThickLine(c, s, new Vector2(16, 20), new Vector2(24, 18), col, 7);
        ThickLine(c, s, new Vector2(28, 28), new Vector2(25, 20), col, 4);
        ThickLine(c, s, new Vector2(38, 38), new Vector2(44, 28), col, 4); // magazine
    }

    // 4. Gậy bóng chày (Vung Gậy)
    static void DrawBaseballBat(Color[] c, int s, Color wood)
    {
        Color grip = new Color(0.2f, 0.2f, 0.2f, 1f);
        ThickLine(c, s, new Vector2(20, 20), new Vector2(32, 32), grip, 5);
        FillCircle(c, s, new Vector2(19, 19), 4f, grip);
        ThickLine(c, s, new Vector2(32, 32), new Vector2(48, 48), wood, 7);
        ThickLine(c, s, new Vector2(48, 48), new Vector2(62, 62), wood, 9);
        FillCircle(c, s, new Vector2(62, 62), 5f, wood);
    }

    // 5. Chim hải âu (Hải Âu Trợ Chiến)
    static void DrawSeagullBird(Color[] c, int s, Color col)
    {
        Color beak = new Color(1f, 0.85f, 0.2f, 1f);
        Color tips = new Color(0.2f, 0.2f, 0.2f, 1f);
        Vector2 center = new Vector2(s * 0.5f, s * 0.48f);
        ThickLine(c, s, center - new Vector2(0, 10), center + new Vector2(0, 10), col, 6);
        FillCircle(c, s, center + new Vector2(0, 12), 2.5f, beak);
        ThickLine(c, s, center + new Vector2(0, 4), center + new Vector2(-24, 10), col, 5);
        ThickLine(c, s, center + new Vector2(-20, 9), center + new Vector2(-26, 11), tips, 4);
        ThickLine(c, s, center + new Vector2(0, 4), center + new Vector2(24, 10), col, 5);
        ThickLine(c, s, center + new Vector2(20, 9), center + new Vector2(26, 11), tips, 4);
    }

    // 6. Quả lựu đạn (Lựu Đạn)
    static void DrawGrenadeIcon(Color[] c, int s, Color col)
    {
        Vector2 center = new Vector2(s * 0.5f, s * 0.45f);
        FillCircle(c, s, center, 17f, col);
        ThickLine(c, s, center + new Vector2(0, 14), center + new Vector2(0, 22), col, 7);
        FillRing(c, s, center + new Vector2(5, 23), 5f, 2f, col);
    }

    // 7. Boomerang
    static void DrawBoomerangIcon(Color[] c, int s, Color col)
    {
        Vector2 elbow = new Vector2(30, 30);
        ThickLine(c, s, elbow, new Vector2(22, 58), col, 8);
        ThickLine(c, s, elbow, new Vector2(58, 22), col, 8);
        FillCircle(c, s, elbow, 4f, col);
        FillCircle(c, s, new Vector2(22, 58), 4f, col);
        FillCircle(c, s, new Vector2(58, 22), 4f, col);
    }

    // Slot_Dmg — kiếm thẳng đứng lớn
    static void DrawSword(Color[] c, int s, Color blade, Color edge)
    {
        int m = s / 2;
        // Thân kiếm (cột dọc dày)
        for (int y = 14; y < s - 8; y++)
        {
            for (int dx = -4; dx <= 4; dx++)
                BlendPixel(c, s, m + dx, y, Mathf.Abs(dx) <= 2 ? blade : edge);
        }
        // Đầu nhọn
        for (int i = 0; i < 8; i++)
            for (int dx = -(3 - i / 3); dx <= (3 - i / 3); dx++)
                BlendPixel(c, s, m + dx, s - 8 + i, blade);
        // Chuôi kiếm (thanh ngang)
        for (int dx = -14; dx <= 14; dx++)
            for (int dy = -3; dy <= 3; dy++)
                BlendPixel(c, s, m + dx, 22 + dy, edge);
        // Tay cầm
        for (int dy = 0; dy < 10; dy++)
            for (int dx = -3; dx <= 3; dx++)
                BlendPixel(c, s, m + dx, 8 + dy, new Color(0.45f, 0.30f, 0.10f, 1f));
    }

    // Slot_Defense — khiên heraldic có viền đôi, chóp nhọn hướng xuống dưới (chuẩn ảnh tham khảo)
    static void DrawShield(Color[] c, int s, Color fill, Color highlight)
    {
        int m = s / 2;
        Color innerLine = Color.Lerp(fill, highlight, 0.65f);
        Color core = Color.Lerp(fill, new Color(0.05f, 0.15f, 0.35f, 1f), 0.5f);

        bool IsInShield(int x, int y, float inset)
        {
            float wMax = 26f - inset;
            if (wMax <= 0f) return false;
            float yBot = 12f + inset * 1.5f;
            float yTopBase = 65f - inset * 0.5f;
            if (y < yBot) return false;

            float dx = Mathf.Abs(x - m);
            float u = Mathf.Clamp01(dx / wMax);
            // Đỉnh khiên: giữa có chóp nhô lên, võng nhẹ ở 2 bên, vểnh ở 2 góc
            float topY = yTopBase + 3f * (1f - u) - 1.5f * Mathf.Sin(u * Mathf.PI);
            if (y > topY) return false;

            float hw;
            if (y < 48f)
            {
                float t = (y - yBot) / (48f - yBot);
                if (t < 0f) return false;
                hw = wMax * Mathf.Pow(t, 0.62f);
            }
            else
            {
                hw = wMax;
            }
            return dx <= hw;
        }

        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                if (IsInShield(x, y, 0f))
                {
                    if (!IsInShield(x, y, 3.5f))
                        BlendPixel(c, s, x, y, highlight);
                    else if (!IsInShield(x, y, 6.0f))
                        BlendPixel(c, s, x, y, innerLine);
                    else if (!IsInShield(x, y, 9.0f))
                        BlendPixel(c, s, x, y, fill);
                    else
                        BlendPixel(c, s, x, y, core);
                }
            }
        }
    }

    // Slot_Health — trái tim cân xứng, 2 múi tròn ở trên, đỉnh nhọn hướng xuống dưới
    static void DrawHeart(Color[] c, int s, Color fill, Color shine)
    {
        int m = s / 2;
        float circR = 13.5f;
        float leftCx = m - 12f, rightCx = m + 12f;
        float cy = 50f;
        float botY = 12f;
        Color border = Color.white;

        bool InHeart(int x, int y, float inset)
        {
            float r = circR - inset;
            if (r <= 0f) return false;
            float dLeft = Mathf.Sqrt((x - leftCx) * (x - leftCx) + (y - cy) * (y - cy));
            float dRight = Mathf.Sqrt((x - rightCx) * (x - rightCx) + (y - cy) * (y - cy));
            if (dLeft <= r || dRight <= r) return true;

            float curBot = botY + inset * 1.5f;
            if (y >= curBot && y <= cy)
            {
                float t = (y - curBot) / (cy - curBot);
                float hw = (25.5f - inset) * Mathf.Pow(t, 0.82f);
                if (Mathf.Abs(x - m) <= hw) return true;
            }
            return false;
        }

        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                if (InHeart(x, y, 0f))
                {
                    if (!InHeart(x, y, 3f))
                        BlendPixel(c, s, x, y, border);
                    else
                        BlendPixel(c, s, x, y, fill);
                }
            }
        }

        // Vệt sáng nhỏ trên múi trái
        FillCircle(c, s, new Vector2(leftCx - 4f, cy + 5f), 3.5f, shine);
    }

    // Slot_Healing — chữ thập y tế trắng
    static void DrawMedicalCross(Color[] c, int s, Color col)
    {
        int m = s / 2, bar = 9;
        // Dọc
        for (int y = 14; y <= s - 14; y++)
            for (int dx = -bar; dx <= bar; dx++)
                BlendPixel(c, s, m + dx, y, col);
        // Ngang
        for (int x = 14; x <= s - 14; x++)
            for (int dy = -bar; dy <= bar; dy++)
                BlendPixel(c, s, x, m + dy, col);
    }

    // Slot_Extender — 4 mũi tên tỏa 4 hướng
    static void DrawFourArrows(Color[] c, int s, Color col)
    {
        int m = s / 2;
        // 4 hướng: trên, dưới, trái, phải
        Vector2[][] arrows =
        {
            new[] { new Vector2(m, m), new Vector2(m, s - 10) },  // lên
            new[] { new Vector2(m, m), new Vector2(m, 10) },      // xuống
            new[] { new Vector2(m, m), new Vector2(10, m) },      // trái
            new[] { new Vector2(m, m), new Vector2(s - 10, m) },  // phải
        };
        foreach (var a in arrows)
        {
            ThickLine(c, s, a[0], a[1], col, 4);
            // Đầu mũi tên
            Vector2 dir = (a[1] - a[0]).normalized;
            Vector2 tip = a[1];
            Vector2 perp = new Vector2(-dir.y, dir.x);
            ThickLine(c, s, tip, tip - dir * 10 + perp * 7, col, 4);
            ThickLine(c, s, tip, tip - dir * 10 - perp * 7, col, 4);
        }
    }

    // Slot_Speed — giày sneaker nhấc lên khi chạy + vạch tốc độ (chuẩn ảnh tham khảo)
    static void DrawLightningBolt(Color[] c, int s, Color col)
    {
        Color lineCol = Color.white;
        Color bodyFill = Color.white;
        Color accentDark = new Color(0.04f, 0.16f, 0.45f, 1f); // xanh navy cắt khối chi tiết

        // 1. Đường mặt đất ngang bên dưới mũi giày
        ThickLine(c, s, new Vector2(16, 11), new Vector2(62, 11), lineCol, 4);

        // 2. 3 vạch tốc độ ngang sau gót chân nhấc lên
        ThickLine(c, s, new Vector2(12, 57), new Vector2(22, 57), lineCol, 3);
        ThickLine(c, s, new Vector2(8, 44), new Vector2(20, 44), lineCol, 3);
        ThickLine(c, s, new Vector2(12, 31), new Vector2(25, 31), lineCol, 3);

        // 3. Khối thân giày sneaker nhấc nghiêng ~40 độ
        Vector2[] polyFull =
        {
            new Vector2(26, 44),
            new Vector2(50, 16),
            new Vector2(62, 16),
            new Vector2(67, 19),
            new Vector2(67, 24),
            new Vector2(56, 29),
            new Vector2(48, 56),
            new Vector2(42, 52),
            new Vector2(35, 61)
        };
        FillPolygon(c, s, polyFull, bodyFill);

        // Viền ngoài giày mượt mà
        for (int i = 0; i < polyFull.Length; i++)
        {
            Vector2 p1 = polyFull[i];
            Vector2 p2 = polyFull[(i + 1) % polyFull.Length];
            ThickLine(c, s, p1, p2, lineCol, 3);
        }

        // 4. Các đường cắt chi tiết phong cách vector
        // Rãnh phân tách đế giày
        ThickLine(c, s, new Vector2(29, 47), new Vector2(50, 21), accentDark, 2);
        ThickLine(c, s, new Vector2(50, 21), new Vector2(65, 21), accentDark, 2);

        // Vòm gót chân (Heel counter)
        ThickLine(c, s, new Vector2(30, 52), new Vector2(36, 45), accentDark, 2);

        // 3 vạch dây giày (Laces)
        ThickLine(c, s, new Vector2(46, 49), new Vector2(50, 52), accentDark, 2);
        ThickLine(c, s, new Vector2(49, 44), new Vector2(53, 47), accentDark, 2);
        ThickLine(c, s, new Vector2(52, 39), new Vector2(56, 42), accentDark, 2);
    }

    // Slot_Magnet — radar quét phạm vi (vòng tròn đồng tâm + kim quét) theo ảnh tham khảo
    static void DrawMagnetU(Color[] c, int s, Color red, Color white)
    {
        int m = s / 2;
        Color ring   = white;                                           // vòng tròn chính
        Color sweep  = new Color(red.r, red.g, red.b, 0.85f);          // kim quét
        Color center = new Color(0.25f, 0.90f, 0.90f, 1f);             // tâm xanh cyan

        // Vẽ 5 vòng tròn đồng tâm dạng đứt quãng (mỗi vòng ~ 270°, thiếu 90° góc trên phải)
        float[] radii = { 8f, 14f, 20f, 26f, 32f };
        foreach (float r in radii)
        {
            float thickness = 3.2f;
            for (int a = 5; a <= 320; a++)       // 315° cung, hở góc trên phải
            {
                float rad = a * Mathf.Deg2Rad;
                float cx = m + Mathf.Cos(rad) * r;
                float cy = m + Mathf.Sin(rad) * r;
                FillCircle(c, s, new Vector2(cx, cy), thickness * .5f, ring);
            }
        }

        // Kim quét: đường thẳng từ tâm ra góc ~45° (hướng trên phải)
        float sweepAngle = 45f * Mathf.Deg2Rad;
        Vector2 sweepTip = new Vector2(
            m + Mathf.Cos(sweepAngle) * 34f,
            m + Mathf.Sin(sweepAngle) * 34f);
        ThickLine(c, s, new Vector2(m, m), sweepTip, sweep, 3);

        // Tâm điểm xanh cyan
        FillCircle(c, s, new Vector2(m, m), 6f, center);
        FillCircle(c, s, new Vector2(m, m), 3f, ring);     // lỗ nhỏ giữa tâm
    }

    // Slot_Luck — cỏ 4 lá
    static void DrawFourLeafClover(Color[] c, int s, Color col)
    {
        int m = s / 2;
        float leafR = 14f;
        // 4 lá theo 4 hướng
        FillCircle(c, s, new Vector2(m,           m + leafR), leafR, col);  // trên
        FillCircle(c, s, new Vector2(m,           m - leafR), leafR, col);  // dưới
        FillCircle(c, s, new Vector2(m + leafR,   m),         leafR, col);  // phải
        FillCircle(c, s, new Vector2(m - leafR,   m),         leafR, col);  // trái
        // Gân lá (tối hơn)
        Color vein = new Color(col.r * .5f, col.g * .5f, col.b * .5f, .8f);
        ThickLine(c, s, new Vector2(m, m), new Vector2(m, m + leafR * 1.8f), vein, 2);
        ThickLine(c, s, new Vector2(m, m), new Vector2(m, m - leafR * 1.8f), vein, 2);
        ThickLine(c, s, new Vector2(m, m), new Vector2(m + leafR * 1.8f, m), vein, 2);
        ThickLine(c, s, new Vector2(m, m), new Vector2(m - leafR * 1.8f, m), vein, 2);
        // Cuống
        ThickLine(c, s, new Vector2(m, m), new Vector2(m, 6), new Color(0.3f, 0.6f, 0.1f, 1f), 3);
    }

    // Slot_Growth — ngôi sao 5 cánh
    static void DrawStar5(Color[] c, int s, Color col)
    {
        int m = s / 2;
        float outerR = 28f, innerR = 12f;
        Vector2 center = new Vector2(m, m);

        // Tạo 10 điểm xen kẽ (outer / inner)
        Vector2[] pts = new Vector2[10];
        for (int i = 0; i < 10; i++)
        {
            float angle = (i * 36 - 90) * Mathf.Deg2Rad;
            float r     = (i % 2 == 0) ? outerR : innerR;
            pts[i] = center + new Vector2(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r);
        }

        // Fill bằng scanline cho polygon ngôi sao
        FillPolygon(c, s, pts, col);
    }

    // Fill convex/star polygon bằng scanline
    static void FillPolygon(Color[] c, int s, Vector2[] poly, Color col)
    {
        int minY = s, maxY = 0;
        foreach (var p in poly) { minY = Mathf.Min(minY, (int)p.y); maxY = Mathf.Max(maxY, (int)p.y); }
        minY = Mathf.Max(0, minY - 1);
        maxY = Mathf.Min(s - 1, maxY + 1);

        for (int y = minY; y <= maxY; y++)
        {
            List<int> xs = new List<int>();
            for (int i = 0; i < poly.Length; i++)
            {
                Vector2 a = poly[i], b = poly[(i + 1) % poly.Length];
                if ((a.y <= y && b.y > y) || (b.y <= y && a.y > y))
                {
                    float t = (y - a.y) / (b.y - a.y);
                    xs.Add((int)(a.x + t * (b.x - a.x)));
                }
            }
            xs.Sort();
            for (int k = 0; k + 1 < xs.Count; k += 2)
                for (int x = xs[k]; x <= xs[k + 1]; x++)
                    BlendPixel(c, s, x, y, col);
        }
    }

    // Slot_Greed — đồng xu vàng tròn
    static void DrawCoin(Color[] c, int s, Color gold, Color dark)
    {
        int m = s / 2;
        // Vòng ngoài vàng sáng
        FillCircle(c, s, new Vector2(m, m), 26f, gold);
        // Vòng trong tối hơn
        FillCircle(c, s, new Vector2(m, m), 20f, dark);
        // Ký hiệu $ ở giữa
        Color sym = new Color(1f, 0.95f, 0.55f, 1f);
        ThickLine(c, s, new Vector2(m, m - 12), new Vector2(m, m + 12), sym, 3); // sọc dọc
        ThickLine(c, s, new Vector2(m - 8, m - 4), new Vector2(m + 8, m - 4), sym, 3); // ngang trên
        ThickLine(c, s, new Vector2(m - 8, m + 4), new Vector2(m + 8, m + 4), sym, 3); // ngang dưới
        FillRing(c, s, new Vector2(m, m), 8f, 2.5f, sym); // vòng tròn $
    }

    // Slot_CritRate — crosshair tâm ngắm
    static void DrawCrosshair(Color[] c, int s, Color col)
    {
        int m = s / 2;
        float outerR = 22f, innerR = 8f;
        // Vòng ngoài
        FillRing(c, s, new Vector2(m, m), outerR, 4f, col);
        // Vòng trong
        FillRing(c, s, new Vector2(m, m), innerR, 2.5f, col);
        // 4 gạch thẳng
        int gap = 10;
        ThickLine(c, s, new Vector2(m - (int)outerR - 4, m), new Vector2(m - gap, m), col, 3);  // trái
        ThickLine(c, s, new Vector2(m + gap, m), new Vector2(m + (int)outerR + 4, m), col, 3);  // phải
        ThickLine(c, s, new Vector2(m, m - (int)outerR - 4), new Vector2(m, m - gap), col, 3);  // trên
        ThickLine(c, s, new Vector2(m, m + gap), new Vector2(m, m + (int)outerR + 4), col, 3);  // dưới
        // Tâm
        FillCircle(c, s, new Vector2(m, m), 3f, col);
    }

    // Slot_CritDmg — vụ nổ nhiều cánh
    static void DrawExplosion(Color[] c, int s, Color inner, Color outer)
    {
        int m = s / 2;
        // Vòng lõi vàng
        FillCircle(c, s, new Vector2(m, m), 14f, inner);
        // 8 tia nổ
        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f * Mathf.Deg2Rad;
            Vector2 tip = new Vector2(m + Mathf.Cos(angle) * 30f, m + Mathf.Sin(angle) * 30f);
            ThickLine(c, s, new Vector2(m, m), tip, outer, 6);
        }
        // Điểm sáng trung tâm
        FillCircle(c, s, new Vector2(m, m), 8f, new Color(1f, 0.98f, 0.85f, 1f));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ── CUSTOM SPRITES (Gameplay visuals) ─────────────────────────────────────
    // ─────────────────────────────────────────────────────────────────────────

    private static Sprite GenerateCustomSprite(string name)
    {
        int w = 32, h = 32;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color[] colors = new Color[w * h];
        for (int i = 0; i < colors.Length; i++) colors[i] = Color.clear;

        switch (name)
        {
            case "SwordBlade":   DrawSwordBlade(colors, w, h);    break;
            case "Seagull":      DrawSeagullSprite(colors, w, h); break;
            case "SunflowerSeed":DrawSunflowerSeed(colors, w, h); break;
            case "Grenade":      DrawGrenadeSprite(colors, w, h); break;
            case "Boomerang":    DrawBoomerangSprite(colors, w, h);break;
            case "EnergyShieldCircle": return GenerateShieldRing(64);
            default:
                FillCircle(colors, w, new Vector2(w * .5f, h * .5f), 10f, Color.white);
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
        Vector2 c2 = new Vector2(size * .5f, size * .5f);
        float radius = size * .44f, thickness = size * .08f;

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dist = Vector2.Distance(new Vector2(x, y), c2);
            float diff = Mathf.Abs(dist - radius);
            if (diff < thickness)
                colors[y * size + x] = new Color(0.2f, 0.85f, 1f, 1f - diff / thickness);
            else if (dist < radius)
                colors[y * size + x] = new Color(0.1f, 0.6f, 1f, 0.04f);
        }

        tex.SetPixels(colors);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    // ── Legacy sprite helpers (dùng trong GenerateCustomSprite) ───────────────

    static void SetPx(Color[] c, int w, int x, int y, Color col)
    {
        if (x >= 0 && x < w && y >= 0 && y < w) c[y * w + x] = col;
    }

    static void DrawSwordBlade(Color[] c, int w, int h)
    {
        // Gậy bóng chày (Baseball Bat)
        Color wood = new Color(0.78f, 0.55f, 0.32f, 1f);
        Color woodDark = new Color(0.55f, 0.35f, 0.18f, 1f);
        Color grip = new Color(0.2f, 0.2f, 0.2f, 1f);
        Color knob = new Color(0.45f, 0.3f, 0.15f, 1f);
        int mid = w / 2;
        // Núm gậy & tay cầm
        for (int y = 2; y <= 4; y++) SetPx(c, w, mid, y, knob);
        for (int y = 5; y <= 11; y++) { SetPx(c, w, mid, y, grip); SetPx(c, w, mid - 1, y, grip); }
        // Thân gậy
        for (int y = 12; y <= 18; y++) { SetPx(c, w, mid, y, wood); SetPx(c, w, mid - 1, y, wood); SetPx(c, w, mid + 1, y, woodDark); }
        // Đầu gậy to tròn
        for (int y = 19; y < h - 4; y++)
        {
            for (int x = mid - 2; x <= mid + 1; x++) SetPx(c, w, x, y, wood);
            SetPx(c, w, mid + 2, y, woodDark);
        }
    }

    static void DrawSeagullSprite(Color[] c, int w, int h)
    {
        Color white = new Color(0.96f, 0.96f, 1f, 1f), grey = new Color(0.7f, 0.75f, 0.85f, 1f),
              beak  = new Color(1f, 0.8f, 0.1f, 1f);
        int midX = w / 2, midY = h / 2;
        for (int x = 4; x < w - 4; x++)
        {
            int y = (int)(midY + Mathf.Sin((x - 4) / (float)(w - 8) * Mathf.PI) * 8f);
            SetPx(c, w, x, y, white); SetPx(c, w, x, y - 1, grey);
        }
        FillCircle(c, w, new Vector2(midX, midY), 4f, white);
        SetPx(c, w, midX + 5, midY, beak); SetPx(c, w, midX + 6, midY, beak);
    }

    static void DrawSunflowerSeed(Color[] c, int w, int h)
    {
        Color seed = new Color(0.95f, 0.8f, 0.2f, 1f), border = new Color(0.5f, 0.35f, 0.1f, 1f);
        int midX = w / 2, midY = h / 2;
        for (int y = midY - 6; y <= midY + 6; y++)
        {
            int span = (int)((1f - Mathf.Abs(y - midY) / 6f) * 4f);
            for (int x = midX - span; x <= midX + span; x++)
                SetPx(c, w, x, y, (x == midX - span || x == midX + span || y == midY - 6 || y == midY + 6) ? border : seed);
        }
    }

    static void DrawGrenadeSprite(Color[] c, int w, int h)
    {
        FillCircle(c, w, new Vector2(w * .5f, h * .45f), 7f, new Color(0.2f, 0.45f, 0.2f, 1f));
        for (int y = (int)(h * .65f); y < h - 4; y++) { SetPx(c, w, w / 2, y, Color.gray); SetPx(c, w, w / 2 + 1, y, Color.gray); }
    }

    static void DrawBoomerangSprite(Color[] c, int w, int h)
    {
        Color wood = new Color(0.85f, 0.55f, 0.25f, 1f);
        ThickLine(c, w, new Vector2(8, 8), new Vector2(w - 6, 14), wood, 3);
        ThickLine(c, w, new Vector2(8, 8), new Vector2(14, h - 6), wood, 3);
    }
}
