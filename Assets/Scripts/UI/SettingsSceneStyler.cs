using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Tự động áp dụng style đồng bộ cho SettingsScene:
/// - Giữ nguyên màu chữ gốc cho các nút & nhãn quan trọng: "< Quay lại", "Cài Đặt", "Âm Lượng Tổng", "Âm Nhạc"
/// - Hiển thị Icon Loa linh hoạt (Full / Nửa / Mute) cho ô âm lượng tổng
/// - Hiển thị Icon Nốt Nhạc cho ô âm nhạc
/// - Hỗ trợ chạm/click vào icon để Mute nhanh
/// - Bo góc panel, slider, nút bấm với theme dark glassmorphism
/// Gắn script này vào SettingsScene Canvas / Root GameObject.
/// </summary>
public class SettingsSceneStyler : MonoBehaviour
{
    [Header("Panel Settings")]
    public Image settingsPanelBg;
    public Color panelBgColor    = new Color(0.08f, 0.10f, 0.18f, 0.92f);
    public Color panelBorderColor = new Color(0.30f, 0.45f, 0.80f, 0.50f);

    [Header("Slider Tint")]
    public Color sliderFillColor       = new Color(0.30f, 0.65f, 1.00f, 1f);
    public Color sliderBackgroundColor = new Color(0.12f, 0.15f, 0.25f, 1f);
    public Color sliderHandleColor     = new Color(1.00f, 1.00f, 1.00f, 1f);

    [Header("Button Tint")]
    public Color buttonNormalColor  = new Color(0.18f, 0.28f, 0.55f, 1f);
    public Color buttonHoverColor   = new Color(0.28f, 0.45f, 0.85f, 1f);
    public Color buttonPressColor   = new Color(0.10f, 0.18f, 0.40f, 1f);
    public Color buttonTextColor    = new Color(0.95f, 0.97f, 1.00f, 1f);

    [Header("Text Preservation")]
    [Tooltip("Giữ nguyên màu chữ gốc cho: < Quay lại, Cài Đặt, Âm Lượng Tổng, Âm Nhạc")]
    public bool preserveImportantTextColors = true;

    [Header("Label Style")]
    [Tooltip("Màu chữ các nhãn phụ (Phiên Bản...). Trắng thuần để tương phản tốt.")]
    public Color labelColor = Color.white;

    [Header("Audio Icon References (Tự động tìm nếu để trống)")]
    public Image masterAudioImage;
    public Image musicAudioImage;
    public Slider masterSlider;
    public Slider musicSlider;

    [Header("Audio Icon Sprites (Tự động tải hoặc sinh procedural)")]
    public Sprite speakerFullSprite;
    public Sprite speakerHalfSprite;
    public Sprite speakerMuteSprite;
    public Sprite musicNoteSprite;

    [Header("Audio Icon Behavior")]
    [Tooltip("Tự động đổi icon loa theo mức âm lượng (Mute / Nửa / Full)")]
    public bool dynamicVolumeIcon = true;
    [Tooltip("Chạm/click vào icon loa hoặc nốt nhạc để bật/tắt Mute nhanh")]
    public bool tapIconToMute = true;

    [Header("Auto-find and style")]
    public bool autoStyleOnStart = true;

    private float prevMasterVolume = 1f;
    private float prevMusicVolume = 1f;

    void Start()
    {
        if (autoStyleOnStart)
        {
            StyleAll();
        }
    }

    [ContextMenu("Áp Dụng Toàn Bộ Style")]
    public void StyleAll()
    {
        StylePanels();
        StyleSliders();
        StyleButtons();
        StyleLabels();
        StyleAudioIcons();
    }

    // ---- 1. Style Panel ----
    private void StylePanels()
    {
        if (settingsPanelBg != null)
        {
            settingsPanelBg.color = panelBgColor;
        }

        Image[] allImages = GetComponentsInChildren<Image>(true);
        foreach (var img in allImages)
        {
            if (img == null) continue;

            string name = img.gameObject.name.ToLower();

            // Không tô đè các ô Icon âm lượng
            if (name.Contains("audiomasterimg") || name.Contains("audiomusicimg"))
                continue;

            // Không tô đè khung tiêu đề đỏ "KhungSetting"
            if (name.Contains("khungsetting"))
                continue;

            // Không tô đè hình nền Canvas Background (ảnh núi Tung Asthetic)
            if (name.Equals("background"))
                continue;

            // Panel background chính: Khung cài đặt
            if (name.Equals("khung") || name.Contains("panel") || name.Contains("frame"))
            {
                img.color = panelBgColor;
            }
            // Dải border
            else if (name.Contains("border") || name.Contains("outline") || name.Contains("line"))
            {
                img.color = panelBorderColor;
            }
        }
    }

    // ---- 2. Style Sliders ----
    private void StyleSliders()
    {
        Slider[] sliders = GetComponentsInChildren<Slider>(true);
        foreach (var slider in sliders)
        {
            if (slider == null) continue;

            // Background track
            Image bg = slider.GetComponentInChildren<Image>();
            if (bg != null) bg.color = sliderBackgroundColor;

            // Fill area
            Transform fillArea = slider.transform.Find("Fill Area");
            if (fillArea != null)
            {
                Image fill = fillArea.GetComponentInChildren<Image>();
                if (fill != null) fill.color = sliderFillColor;
            }

            // Handle
            if (slider.handleRect != null)
            {
                Image handle = slider.handleRect.GetComponent<Image>();
                if (handle != null)
                {
                    handle.color = sliderHandleColor;
                    handle.rectTransform.sizeDelta = new Vector2(28f, 28f);
                }
            }

            // Colors state
            ColorBlock cb = slider.colors;
            cb.normalColor      = sliderHandleColor;
            cb.highlightedColor = sliderFillColor;
            cb.pressedColor     = new Color(sliderFillColor.r * 0.7f, sliderFillColor.g * 0.7f, sliderFillColor.b * 0.7f);
            cb.selectedColor    = sliderFillColor;
            slider.colors = cb;
        }
    }

    // ---- 3. Style Buttons (Bảo toàn nút < Quay lại) ----
    private void StyleButtons()
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (var btn in buttons)
        {
            if (btn == null) continue;

            string btnName = btn.gameObject.name.ToLower();
            TextMeshProUGUI tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
            string tmpText = tmp != null ? tmp.text.ToLower() : "";

            // Nút "< Quay Lại": Giữ nguyên như cũ, không điều chỉnh màu
            if (preserveImportantTextColors && (btnName.Contains("back") || tmpText.Contains("quay")))
            {
                Image backImg = btn.GetComponent<Image>();
                if (backImg != null)
                {
                    backImg.color = Color.white;
                }
                if (tmp != null)
                {
                    // Màu gốc #323232
                    tmp.color = new Color(0.19607843f, 0.19607843f, 0.19607843f, 1f);
                }
                continue;
            }

            // Nút khác (Chính Sách, Phản Hồi...)
            Image btnImg = btn.GetComponent<Image>();
            if (btnImg != null) btnImg.color = buttonNormalColor;

            ColorBlock cb = btn.colors;
            cb.normalColor      = buttonNormalColor;
            cb.highlightedColor = buttonHoverColor;
            cb.pressedColor     = buttonPressColor;
            cb.selectedColor    = buttonNormalColor;
            cb.colorMultiplier  = 1f;
            btn.colors = cb;

            if (tmp != null)
            {
                tmp.color = buttonTextColor;
                tmp.fontStyle = FontStyles.Bold;
            }
        }
    }

    // ---- 4. Style Labels (Bảo toàn "Cài Đặt", "Âm Lượng Tổng", "Âm Nhạc") ----
    private void StyleLabels()
    {
        TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var txt in texts)
        {
            if (txt == null) continue;

            // Không tô đè text trong Button
            if (txt.GetComponentInParent<Button>() != null) continue;

            // Không tô đè Toggle
            if (txt.GetComponentInParent<Toggle>() != null) continue;

            string name = txt.gameObject.name.ToLower();
            string rawText = txt.text != null ? txt.text.Trim() : "";

            // "Cài Đặt", "Âm Lượng Tổng", "Âm Nhạc": Giữ nguyên như cũ không bị đổi màu
            if (preserveImportantTextColors && IsPreservedLabel(name, rawText))
            {
                txt.color = Color.white; // Màu gốc trong scene
                continue;
            }

            if (name.Contains("title") || name.Contains("header"))
            {
                txt.color = Color.white;
                txt.fontStyle = FontStyles.Bold;
            }
            else
            {
                txt.color = labelColor;
            }
        }
    }

    private bool IsPreservedLabel(string objName, string rawText)
    {
        // "Cài Đặt" (SettingText)
        if (objName.Contains("settingtext") || 
            rawText.Equals("CÀI ĐẶT", System.StringComparison.OrdinalIgnoreCase) || 
            rawText.Equals("Cài Đặt", System.StringComparison.OrdinalIgnoreCase))
            return true;

        // "Âm Lượng Tổng" (AudioMaster)
        if (objName.Contains("audiomaster") || 
            rawText.Contains("Âm Lượng Tổng") || 
            rawText.Contains("Am Luong Tong"))
            return true;

        // "Âm Nhạc" (AudioMusic)
        if (objName.Contains("audiomusic") || 
            rawText.Contains("Âm Nhạc") || 
            rawText.Contains("Am Nhac"))
            return true;

        return false;
    }

    // ---- 5. Icon Loa & Icon Nốt Nhạc ----
    [ContextMenu("Làm Mới Audio Icons")]
    public void StyleAudioIcons()
    {
        FindAudioComponents();
        LoadOrGenerateIconSprites();
        SetupSpeakerIcon();
        SetupMusicNoteIcon();
    }

    private void FindAudioComponents()
    {
        // 1. Ô Icon Loa
        if (masterAudioImage == null)
        {
            Transform t = transform.Find("Khung/AudioMasterIMG");
            if (t == null) t = transform.Find("AudioMasterIMG");
            if (t == null)
            {
                Image[] allImgs = GetComponentsInChildren<Image>(true);
                foreach (var img in allImgs)
                {
                    if (img != null && img.gameObject.name.Equals("AudioMasterIMG", System.StringComparison.OrdinalIgnoreCase))
                    {
                        masterAudioImage = img;
                        break;
                    }
                }
            }
            else
            {
                masterAudioImage = t.GetComponent<Image>();
            }
        }

        // 2. Ô Icon Nốt Nhạc
        if (musicAudioImage == null)
        {
            Transform t = transform.Find("Khung/AudioMusicIMG");
            if (t == null) t = transform.Find("AudioMusicIMG");
            if (t == null)
            {
                Image[] allImgs = GetComponentsInChildren<Image>(true);
                foreach (var img in allImgs)
                {
                    if (img != null && img.gameObject.name.Equals("AudioMusicIMG", System.StringComparison.OrdinalIgnoreCase))
                    {
                        musicAudioImage = img;
                        break;
                    }
                }
            }
            else
            {
                musicAudioImage = t.GetComponent<Image>();
            }
        }

        // 3. Slider liên kết
        if (masterSlider == null || musicSlider == null)
        {
            SettingsManager sm = FindAnyObjectByType<SettingsManager>();
            if (sm != null)
            {
                if (masterSlider == null) masterSlider = sm.masterVolumeSlider;
                if (musicSlider == null) musicSlider = sm.musicVolumeSlider;
            }
        }

        if (masterSlider == null)
        {
            Slider[] allSliders = GetComponentsInChildren<Slider>(true);
            foreach (var s in allSliders)
            {
                string sName = s.gameObject.name.ToLower();
                if (sName.Contains("tổng") || sName.Contains("master"))
                {
                    masterSlider = s;
                    break;
                }
            }
            if (masterSlider == null && allSliders.Length > 0) masterSlider = allSliders[0];
        }

        if (musicSlider == null)
        {
            Slider[] allSliders = GetComponentsInChildren<Slider>(true);
            foreach (var s in allSliders)
            {
                string sName = s.gameObject.name.ToLower();
                if (sName.Contains("nhạc") || sName.Contains("music"))
                {
                    musicSlider = s;
                    break;
                }
            }
            if (musicSlider == null && allSliders.Length > 1) musicSlider = allSliders[1];
        }
    }

    private void LoadOrGenerateIconSprites()
    {
        #if UNITY_EDITOR
        if (speakerFullSprite == null)
            speakerFullSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/icon_speaker_full.png");
        if (speakerHalfSprite == null)
            speakerHalfSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/icon_speaker_half.png");
        if (speakerMuteSprite == null)
            speakerMuteSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/icon_speaker_mute.png");
        if (musicNoteSprite == null)
            musicNoteSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/icon_music_note.png");
        #endif

        // Fallback procedural nếu chưa import asset
        if (speakerFullSprite == null) speakerFullSprite = GenerateProceduralSpeakerSprite("full");
        if (speakerHalfSprite == null) speakerHalfSprite = GenerateProceduralSpeakerSprite("half");
        if (speakerMuteSprite == null) speakerMuteSprite = GenerateProceduralSpeakerSprite("mute");
        if (musicNoteSprite == null)   musicNoteSprite   = GenerateProceduralMusicNoteSprite();
    }

    private void SetupSpeakerIcon()
    {
        if (masterAudioImage == null) return;

        masterAudioImage.color = Color.white;
        masterAudioImage.preserveAspect = true;

        if (masterSlider != null)
        {
            masterSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);
            masterSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            UpdateSpeakerIcon(masterSlider.value);
        }
        else
        {
            masterAudioImage.sprite = speakerFullSprite;
        }

        // Tap/click để mute nhanh
        if (tapIconToMute)
        {
            Button btn = masterAudioImage.GetComponent<Button>();
            if (btn == null) btn = masterAudioImage.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.RemoveListener(ToggleMasterMute);
            btn.onClick.AddListener(ToggleMasterMute);
        }
    }

    private void OnMasterVolumeChanged(float val)
    {
        UpdateSpeakerIcon(val);
    }

    private void UpdateSpeakerIcon(float volume)
    {
        if (masterAudioImage == null) return;

        if (!dynamicVolumeIcon)
        {
            masterAudioImage.sprite = speakerFullSprite;
            return;
        }

        // Đổi trạng thái: Mute / Nửa / Full
        if (volume <= 0.001f)
        {
            masterAudioImage.sprite = speakerMuteSprite;
        }
        else if (volume < 0.5f)
        {
            masterAudioImage.sprite = speakerHalfSprite;
        }
        else
        {
            masterAudioImage.sprite = speakerFullSprite;
        }
    }

    private void ToggleMasterMute()
    {
        if (masterSlider == null) return;

        if (masterSlider.value > 0.001f)
        {
            prevMasterVolume = masterSlider.value;
            masterSlider.value = 0f;
        }
        else
        {
            masterSlider.value = prevMasterVolume > 0.05f ? prevMasterVolume : 0.8f;
        }
    }

    private void SetupMusicNoteIcon()
    {
        if (musicAudioImage == null) return;

        musicAudioImage.color = Color.white;
        musicAudioImage.preserveAspect = true;
        musicAudioImage.sprite = musicNoteSprite;

        if (musicSlider != null)
        {
            musicSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);
            musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            UpdateMusicIcon(musicSlider.value);
        }

        if (tapIconToMute)
        {
            Button btn = musicAudioImage.GetComponent<Button>();
            if (btn == null) btn = musicAudioImage.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.RemoveListener(ToggleMusicMute);
            btn.onClick.AddListener(ToggleMusicMute);
        }
    }

    private void OnMusicVolumeChanged(float val)
    {
        UpdateMusicIcon(val);
    }

    private void UpdateMusicIcon(float volume)
    {
        if (musicAudioImage == null) return;

        if (volume <= 0.001f)
        {
            musicAudioImage.color = new Color(1f, 1f, 1f, 0.45f);
        }
        else
        {
            musicAudioImage.color = Color.white;
        }
    }

    private void ToggleMusicMute()
    {
        if (musicSlider == null) return;

        if (musicSlider.value > 0.001f)
        {
            prevMusicVolume = musicSlider.value;
            musicSlider.value = 0f;
        }
        else
        {
            musicSlider.value = prevMusicVolume > 0.05f ? prevMusicVolume : 0.8f;
        }
    }

    // =========================================================================
    // PROCEDURAL ICON GENERATOR (Fallback độc lập, không phụ thuộc tài nguyên ngoài)
    // =========================================================================

    public static Sprite GenerateProceduralSpeakerSprite(string mode, bool withBadge = true)
    {
        int size = 256;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color[] pixels = new Color[size * size];

        Color badgeBgTop = new Color(0.10f, 0.14f, 0.23f, 0.94f);
        Color badgeBgBot = new Color(0.05f, 0.07f, 0.12f, 0.96f);
        Color badgeBorder = new Color(0.32f, 0.52f, 0.92f, 0.70f);
        Color iconColor = new Color(0.98f, 0.99f, 1.00f, 1f);
        Color muteColor = new Color(0.96f, 0.29f, 0.29f, 1f);

        for (int y = 0; y < size; y++)
        {
            float py = y + 0.5f;
            float tY = (float)y / (size - 1);
            Color bgRow = Color.Lerp(badgeBgBot, badgeBgTop, tY);

            for (int x = 0; x < size; x++)
            {
                float px = x + 0.5f;
                Color pixel = Color.clear;

                if (withBadge)
                {
                    float sdfBadge = RoundedRectSDF(px - 128f, py - 128f, 108f, 108f, 42f);
                    if (sdfBadge <= 0f)
                    {
                        if (sdfBadge >= -4.5f)
                            pixel = Color.Lerp(bgRow, badgeBorder, (sdfBadge + 4.5f) / 4.5f);
                        else
                            pixel = bgRow;
                    }
                    else if (sdfBadge < 2f)
                    {
                        pixel = new Color(badgeBorder.r, badgeBorder.g, badgeBorder.b, badgeBorder.a * Mathf.Clamp01(1f - sdfBadge * 0.5f));
                    }
                }

                // Thân loa
                float spkAlpha = 0f;
                if (px >= 54f && px <= 92f && py >= 104f && py <= 152f)
                {
                    spkAlpha = 1f;
                    if (px <= 60f)
                    {
                        if (py < 110f) spkAlpha = Mathf.Clamp01(1f - (Vector2.Distance(new Vector2(px, py), new Vector2(60f, 110f)) - 6f));
                        else if (py > 146f) spkAlpha = Mathf.Clamp01(1f - (Vector2.Distance(new Vector2(px, py), new Vector2(60f, 146f)) - 6f));
                    }
                }
                else if (px > 92f && px <= 136f)
                {
                    float t = (px - 92f) / 44f;
                    float halfH = Mathf.Lerp(24f, 62f, t);
                    float dY = Mathf.Abs(py - 128f);
                    if (dY <= halfH)
                    {
                        spkAlpha = Mathf.Clamp01(halfH - dY + 0.5f);
                    }
                }

                if (spkAlpha > 0f)
                {
                    pixel = BlendColors(pixel, new Color(iconColor.r, iconColor.g, iconColor.b, iconColor.a * spkAlpha));
                }

                // Sóng âm thanh / Mute
                if (mode == "mute")
                {
                    float dX1 = DistToSegment(px, py, 162f, 104f, 206f, 152f);
                    float dX2 = DistToSegment(px, py, 162f, 152f, 206f, 104f);
                    float minD = Mathf.Min(dX1, dX2);
                    if (minD <= 8.5f)
                    {
                        float a = Mathf.Clamp01(8.5f - minD);
                        pixel = BlendColors(pixel, new Color(muteColor.r, muteColor.g, muteColor.b, a));
                    }
                }
                else if (mode == "half")
                {
                    float arcA = ArcSDF(px, py, 126f, 128f, 44f, 14f, 38f);
                    if (arcA > 0f)
                    {
                        pixel = BlendColors(pixel, new Color(iconColor.r, iconColor.g, iconColor.b, iconColor.a * arcA));
                    }
                }
                else if (mode == "full")
                {
                    float arcA1 = ArcSDF(px, py, 126f, 128f, 42f, 13f, 36f);
                    float arcA2 = ArcSDF(px, py, 126f, 128f, 74f, 13f, 44f);
                    float maxArc = Mathf.Max(arcA1, arcA2);
                    if (maxArc > 0f)
                    {
                        pixel = BlendColors(pixel, new Color(iconColor.r, iconColor.g, iconColor.b, iconColor.a * maxArc));
                    }
                }

                pixels[y * size + x] = pixel;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    public static Sprite GenerateProceduralMusicNoteSprite(bool withBadge = true)
    {
        int size = 256;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color[] pixels = new Color[size * size];

        Color badgeBgTop = new Color(0.10f, 0.14f, 0.23f, 0.94f);
        Color badgeBgBot = new Color(0.05f, 0.07f, 0.12f, 0.96f);
        Color badgeBorder = new Color(0.32f, 0.52f, 0.92f, 0.70f);
        Color iconColor = new Color(0.98f, 0.99f, 1.00f, 1f);

        for (int y = 0; y < size; y++)
        {
            float py = y + 0.5f;
            float tY = (float)y / (size - 1);
            Color bgRow = Color.Lerp(badgeBgBot, badgeBgTop, tY);

            for (int x = 0; x < size; x++)
            {
                float px = x + 0.5f;
                Color pixel = Color.clear;

                if (withBadge)
                {
                    float sdfBadge = RoundedRectSDF(px - 128f, py - 128f, 108f, 108f, 42f);
                    if (sdfBadge <= 0f)
                    {
                        if (sdfBadge >= -4.5f)
                            pixel = Color.Lerp(bgRow, badgeBorder, (sdfBadge + 4.5f) / 4.5f);
                        else
                            pixel = bgRow;
                    }
                    else if (sdfBadge < 2f)
                    {
                        pixel = new Color(badgeBorder.r, badgeBorder.g, badgeBorder.b, badgeBorder.a * Mathf.Clamp01(1f - sdfBadge * 0.5f));
                    }
                }

                // 2 nốt nhạc elip nghiêng
                float head1 = TiltedEllipseSDF(px - 78f, py - 84f, 24f, 17f, 25f);
                float head2 = TiltedEllipseSDF(px - 166f, py - 112f, 24f, 17f, 25f);

                // 2 thân dọc
                float stem1 = DistToSegment(px, py, 97f, 88f, 97f, 188f) - 6.5f;
                float stem2 = DistToSegment(px, py, 185f, 116f, 185f, 214f) - 6.5f;

                // Cầu nối beam trên
                float beam = BeamSDF(px, py, 91f, 180f, 191f, 206f, 24f);

                float minD = Mathf.Min(Mathf.Min(head1, head2), Mathf.Min(Mathf.Min(stem1, stem2), beam));
                if (minD <= 0f)
                {
                    pixel = BlendColors(pixel, iconColor);
                }
                else if (minD < 1.5f)
                {
                    float a = Mathf.Clamp01(1f - minD / 1.5f);
                    pixel = BlendColors(pixel, new Color(iconColor.r, iconColor.g, iconColor.b, iconColor.a * a));
                }

                pixels[y * size + x] = pixel;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private static float RoundedRectSDF(float px, float py, float halfW, float halfH, float r)
    {
        float qx = Mathf.Abs(px) - (halfW - r);
        float qy = Mathf.Abs(py) - (halfH - r);
        return Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) + Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f))
             + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
    }

    private static float DistToSegment(float px, float py, float x1, float y1, float x2, float y2)
    {
        float dx = x2 - x1;
        float dy = y2 - y1;
        float lenSq = dx * dx + dy * dy;
        if (lenSq < 0.0001f) return Vector2.Distance(new Vector2(px, py), new Vector2(x1, y1));

        float t = Mathf.Clamp01(((px - x1) * dx + (py - y1) * dy) / lenSq);
        float projX = x1 + t * dx;
        float projY = y1 + t * dy;
        return Mathf.Sqrt((px - projX) * (px - projX) + (py - projY) * (py - projY));
    }

    private static float ArcSDF(float px, float py, float cx, float cy, float radius, float thickness, float maxAngleDeg)
    {
        float dx = px - cx;
        float dy = py - cy;
        float r = Mathf.Sqrt(dx * dx + dy * dy);
        float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;

        float dist;
        if (Mathf.Abs(angle) <= maxAngleDeg)
        {
            dist = Mathf.Abs(r - radius);
        }
        else
        {
            float rad1 = maxAngleDeg * Mathf.Deg2Rad;
            float cap1X = cx + radius * Mathf.Cos(rad1);
            float cap1Y = cy + radius * Mathf.Sin(rad1);

            float rad2 = -maxAngleDeg * Mathf.Deg2Rad;
            float cap2X = cx + radius * Mathf.Cos(rad2);
            float cap2Y = cy + radius * Mathf.Sin(rad2);

            float d1 = Mathf.Sqrt((px - cap1X) * (px - cap1X) + (py - cap1Y) * (py - cap1Y));
            float d2 = Mathf.Sqrt((px - cap2X) * (px - cap2X) + (py - cap2Y) * (py - cap2Y));
            dist = Mathf.Min(d1, d2);
        }

        float halfThick = thickness * 0.5f;
        if (dist <= halfThick) return 1f;
        if (dist <= halfThick + 1.5f) return Mathf.Clamp01(1f - (dist - halfThick) / 1.5f);
        return 0f;
    }

    private static float TiltedEllipseSDF(float dx, float dy, float rx, float ry, float angleDeg)
    {
        float rad = -angleDeg * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        float rotX = dx * cos - dy * sin;
        float rotY = dx * sin + dy * cos;
        float d = (rotX * rotX) / (rx * rx) + (rotY * rotY) / (ry * ry);
        return (d - 1f) * Mathf.Min(rx, ry);
    }

    private static float BeamSDF(float px, float py, float x1, float y1, float x2, float y2, float thickness)
    {
        if (px < x1 || px > x2) return 100f;
        float t = (px - x1) / (x2 - x1);
        float midY = Mathf.Lerp(y1, y2, t);
        return Mathf.Abs(py - midY) - thickness * 0.5f;
    }

    private static Color BlendColors(Color dst, Color src)
    {
        float outA = src.a + dst.a * (1f - src.a);
        if (outA <= 0.001f) return Color.clear;
        float r = (src.r * src.a + dst.r * dst.a * (1f - src.a)) / outA;
        float g = (src.g * src.a + dst.g * dst.a * (1f - src.a)) / outA;
        float b = (src.b * src.a + dst.b * dst.a * (1f - src.a)) / outA;
        return new Color(r, g, b, outA);
    }
}
