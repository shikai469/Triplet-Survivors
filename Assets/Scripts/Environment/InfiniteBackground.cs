using UnityEngine;

public class InfiniteBackground : MonoBehaviour
{
    private Material mat;
    private MeshRenderer meshRenderer;

    // Đặt là 1 để nền trôi khớp với bước chân, đặt 0.5 để nền trôi chậm hơn tạo cảm giác ở xa
    public float scrollFactor = 1f;

    [Header("Default Fallback Background (Tùy chọn)")]
    public Sprite defaultBackgroundSprite;

    // Cache ID shader properties để tránh GC và string lookup mỗi frame trong Update
    private static readonly int BaseMapPropId = Shader.PropertyToID("_BaseMap");
    private static readonly int MainTexPropId = Shader.PropertyToID("_MainTex");
    private bool hasBaseMap;
    private bool hasMainTex;

    void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            mat = meshRenderer.material; // Tạo bản sao Material instance của đối tượng
            UpdateShaderProperties();
        }
    }

    void Start()
    {
        ApplySelectedBackground();
    }

    // Tự động nhận diện Background tương ứng với Level/Độ khó người chơi vừa chọn
    public void ApplySelectedBackground()
    {
        // 1. Ưu tiên Material/Sprite được truyền trực tiếp từ LevelNode ngoài Menu
        if (GameDataManager.SelectedBackgroundMaterial != null)
        {
            SetBackgroundMaterial(GameDataManager.SelectedBackgroundMaterial);
            return;
        }

        if (GameDataManager.SelectedBackgroundSprite != null)
        {
            SetBackgroundTexture(GameDataManager.SelectedBackgroundSprite.texture);
            return;
        }

        // 2. Kiểm tra cấu hình override từ GameStageManager (nếu có cấu hình trong DifficultyConfig)
        if (GameStageManager.Instance != null && GameStageManager.Instance.activeConfig != null)
        {
            var config = GameStageManager.Instance.activeConfig;
            if (config.backgroundMaterialOverride != null)
            {
                SetBackgroundMaterial(config.backgroundMaterialOverride);
                return;
            }

            if (config.backgroundSpriteOverride != null)
            {
                SetBackgroundTexture(config.backgroundSpriteOverride.texture);
                return;
            }
        }

        // 3. Nếu có Sprite mặc định
        if (defaultBackgroundSprite != null)
        {
            SetBackgroundTexture(defaultBackgroundSprite.texture);
        }
    }

    public void SetBackgroundMaterial(Material newMat)
    {
        if (meshRenderer != null && newMat != null)
        {
            mat = new Material(newMat);
            meshRenderer.material = mat;
            UpdateShaderProperties();
        }
    }

    public void SetBackgroundTexture(Texture newTex)
    {
        if (mat == null && meshRenderer != null)
        {
            mat = meshRenderer.material;
        }

        if (mat != null && newTex != null)
        {
            mat.mainTexture = newTex;
            UpdateShaderProperties();
            if (hasBaseMap) mat.SetTexture(BaseMapPropId, newTex);
            if (hasMainTex) mat.SetTexture(MainTexPropId, newTex);
        }
    }

    private void UpdateShaderProperties()
    {
        if (mat == null) return;
        hasBaseMap = mat.HasProperty(BaseMapPropId);
        hasMainTex = mat.HasProperty(MainTexPropId);
    }

    void Update()
    {
        if (mat == null) return;

        // Tự động chia cho Scale để tốc độ trôi hoàn toàn chuẩn xác theo bước chân
        float scaleX = transform.localScale.x != 0f ? transform.localScale.x : 1f;
        float scaleY = transform.localScale.y != 0f ? transform.localScale.y : 1f;

        float x = (transform.position.x / scaleX) * scrollFactor;
        float y = (transform.position.y / scaleY) * scrollFactor;

        Vector2 offset = new Vector2(x, y);

        // Lệnh bao quát ép trượt ảnh cho cả Shader 2D Sprite-Unlit và 3D URP Lit (dùng cached property ID)
        mat.mainTextureOffset = offset;
        if (hasBaseMap) mat.SetTextureOffset(BaseMapPropId, offset);
        if (hasMainTex) mat.SetTextureOffset(MainTexPropId, offset);
    }
}