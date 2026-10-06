using UnityEngine;

public enum SkillType
{
    ActiveWeapon,   // Kỹ năng vũ khí (Gun, Sword, Drone,...)
    PassiveStat     // Kỹ năng bị động tăng chỉ số (Speed, Magnet,...)
}

public enum SkillID
{
    Gun,
    OrbitSwords,
    AssaultDrone,
    PulseShield,
    SpeedUp,
    MagnetUp,
    StrengthUp
}

[CreateAssetMenu(fileName = "NewSkill", menuName = "TripleT/Skill Data")]
public class SkillData : ScriptableObject
{
    [Header("General Info")]
    public SkillID skillID;
    public string skillName;
    public SkillType skillType;
    public Sprite icon;
    [TextArea(2, 4)]
    public string description;

    [Header("Level Settings")]
    public int maxLevel = 8; // Active: 8, Passive: 5 theo chuẩn IF
    public int currentLevel = 0;

    public bool IsMaxLevel()
    {
        return currentLevel >= maxLevel;
    }
}