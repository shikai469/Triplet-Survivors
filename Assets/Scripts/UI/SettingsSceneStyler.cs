using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Tự động áp dụng style đồng bộ cho SettingsScene:
/// - Bo góc panel, slider, nút bấm
/// - Màu sắc nhất quán (dark glassmorphism theme)
/// - Layout gọn gàng
/// Gắn script này vào SettingsSceneRoot GameObject.
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

    [Header("Label Style")]
    [Tooltip("Màu chữ nhãn (Âm Lượng Tổng, Âm Nhạc, Phiên Bản...). Trắng thuần để tương phản tốt nhất với mọi nền.")]
    public Color labelColor = Color.white;

    [Header("Auto-find and style (leave unchecked to configure manually)")]
    public bool autoStyleOnStart = true;

    void Start()
    {
        if (autoStyleOnStart) StyleAll();
    }

    public void StyleAll()
    {
        StylePanels();
        StyleSliders();
        StyleButtons();
        StyleLabels();
    }

    // ---- Style tất cả Image (coi là panel background) ----
    private void StylePanels()
    {
        // Panel chính
        if (settingsPanelBg != null)
        {
            settingsPanelBg.color = panelBgColor;
        }

        // Tìm tất cả Image có tag "SettingsPanel" trong hierarchy
        Image[] allImages = GetComponentsInChildren<Image>(true);
        foreach (var img in allImages)
        {
            if (img == null) continue;

            string name = img.gameObject.name.ToLower();

            // Panel background chính
            if (name.Contains("panel") || name.Contains("frame") || name.Contains("background"))
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

    // ---- Style tất cả Slider ----
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
                    // Đặt kích thước handle lớn hơn cho dễ kéo
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

    // ---- Style tất cả Button ----
    private void StyleButtons()
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (var btn in buttons)
        {
            if (btn == null) continue;

            // Image nền nút
            Image btnImg = btn.GetComponent<Image>();
            if (btnImg != null) btnImg.color = buttonNormalColor;

            // Color states
            ColorBlock cb = btn.colors;
            cb.normalColor      = buttonNormalColor;
            cb.highlightedColor = buttonHoverColor;
            cb.pressedColor     = buttonPressColor;
            cb.selectedColor    = buttonNormalColor;
            cb.colorMultiplier  = 1f;
            btn.colors = cb;

            // Text trong nút
            TextMeshProUGUI tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.color = buttonTextColor;
                tmp.fontStyle = FontStyles.Bold;
            }
        }
    }

    // ---- Style tất cả TextMeshProUGUI (nhãn) ----
    private void StyleLabels()
    {
        TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var txt in texts)
        {
            if (txt == null) continue;

            // Không tô đè text trong Button (đã style riêng)
            if (txt.GetComponentInParent<Button>() != null) continue;

            // Không tô đè text Toggle label (thường là checkbox)
            if (txt.GetComponentInParent<Toggle>() != null) continue;

            string name = txt.gameObject.name.ToLower();
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

    // ---- Sinh sprite nền panel bo góc (dùng Runtime nếu không có ảnh) ----
    public static Sprite GenerateRoundedRectSprite(int w, int h, float cornerRadius, Color fillColor, Color borderColor, float borderWidth = 2f)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color[] pixels = new Color[w * h];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float px = x + 0.5f;
                float py = y + 0.5f;
                float sdf = RoundedRectSDF(px, py, w, h, cornerRadius);

                Color c;
                if (sdf > borderWidth)
                    c = Color.clear;
                else if (sdf > 0f)
                    c = Color.Lerp(borderColor, Color.clear, sdf / borderWidth);
                else
                    c = fillColor;

                pixels[y * w + x] = c;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f,
            0, SpriteMeshType.FullRect, new Vector4(cornerRadius, cornerRadius, cornerRadius, cornerRadius));
    }

    // Signed Distance Field của hình chữ nhật bo góc
    private static float RoundedRectSDF(float px, float py, int w, int h, float r)
    {
        float qx = Mathf.Abs(px - w * 0.5f) - (w * 0.5f - r);
        float qy = Mathf.Abs(py - h * 0.5f) - (h * 0.5f - r);
        return Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) + Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f)) - r;
    }
}
