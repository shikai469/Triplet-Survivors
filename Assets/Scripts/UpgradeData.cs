using UnityEngine;

// Dòng này giúp bạn click chuột phải trong Unity để tạo ra một file Thẻ kỹ năng mới
[CreateAssetMenu(fileName = "NewUpgrade", menuName = "TripleT/Upgrade Card")]
public class UpgradeData : ScriptableObject
{
    public Sprite icon;
    public string title;
    public string description;
    public int maxLevel = 5;

    // Danh sách các loại kỹ năng giống với cách dự án mẫu phân loại
    public enum UpgradeType { MoveSpeed, MagnetRange, FireRate, Heal }
    public UpgradeType upgradeType;
}