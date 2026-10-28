using UnityEngine;
using UnityEngine.UI;

public class LevelNode : MonoBehaviour
{
    [Header("Difficulty & Level Settings")]
    public DifficultyLevel difficulty = DifficultyLevel.Normal;
    public bool autoSyncSavedRecords = true; // Tự động đọc dữ liệu Clear/Death/Survival từ GameDataManager

    [Header("Level Display Information")]
    public string levelName; // Tên màn chơi
    public Sprite mapPreviewImage; // Ảnh nền preview tại Menu Chọn Màn

    [Header("In-Game Background (Map Gameplay)")]
    [Tooltip("Ảnh nền trôi vô tận trong GameplayScene. Nếu để trống, hệ thống sẽ tự động dùng ảnh mapPreviewImage.")]
    public Sprite inGameBackgroundSprite;
    [Tooltip("(Tùy chọn) Material riêng nếu màn chơi này dùng Shader/Material khác.")]
    public Material inGameBackgroundMaterial;

    [TextArea(3, 5)]
    public string levelDetail; // Mô tả chi tiết màn chơi (cho phép gõ nhiều dòng)
    public string bossTime = "--:--"; // Thời gian xuất hiện Boss
    public int clearCount = 0; // Số lần hoàn thành
    public int deathCount = 0; // Số lần chết
    public string survivalTime = "--:--"; // Kỷ lục sống sót

    private Button btn;
    private LevelSelectManager manager;

    void Awake()
    {
        RefreshRecords();
    }

    void Start()
    {
        btn = GetComponent<Button>();
        manager = FindAnyObjectByType<LevelSelectManager>();

        if (btn != null && manager != null)
        {
            btn.onClick.AddListener(() => manager.OnLevelSelected(this));
        }
    }

    // Làm mới số liệu hiển thị thực tế từ file lưu trữ
    public void RefreshRecords()
    {
        if (autoSyncSavedRecords)
        {
            clearCount = GameDataManager.GetClearCount(difficulty);
            deathCount = GameDataManager.GetDeathCount(difficulty);
            survivalTime = GameDataManager.GetFormattedBestSurvivalTime(difficulty);

            // Thiết lập giá trị mặc định nếu người dùng chưa điền trên Inspector
            DifficultyConfig defaultConfig = DifficultyConfig.GetDefault(difficulty);

            if (string.IsNullOrEmpty(levelName))
            {
                levelName = defaultConfig.difficultyName;
            }

            if (string.IsNullOrEmpty(levelDetail))
            {
                levelDetail = defaultConfig.levelDescription;
            }

            if (bossTime == "--:--" || string.IsNullOrEmpty(bossTime))
            {
                if (difficulty == DifficultyLevel.Endless)
                {
                    bossTime = "Mỗi 02:00 (Định kỳ)";
                }
                else
                {
                    int m = Mathf.FloorToInt(defaultConfig.stageDuration / 60f);
                    int s = Mathf.FloorToInt(defaultConfig.stageDuration % 60f);
                    bossTime = string.Format("{0:00}:{1:00}", m, s);
                }
            }
        }
    }
}