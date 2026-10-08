using UnityEngine;
using UnityEngine.UI;

public class LevelNode : MonoBehaviour
{
    [Header("Level Information")]
    public string levelName; // Tên màn chơi
    public Sprite mapPreviewImage; // Ảnh nền

    [TextArea(3, 5)]
    public string levelDetail; // Mô tả chi tiết màn chơi (cho phép gõ nhiều dòng)
    public string bossTime = "--:--"; // Thời gian xuất hiện Boss
    public int clearCount = 0; // Số lần hoàn thành
    public int deathCount = 0; // Số lần chết
    public string survivalTime = "--:--"; // Kỷ lục sống sót

    private Button btn;
    private LevelSelectManager manager;

    void Start()
    {
        btn = GetComponent<Button>();
        manager = FindObjectOfType<LevelSelectManager>();

        if (btn != null && manager != null)
        {
            btn.onClick.AddListener(() => manager.OnLevelSelected(this));
        }
    }
}