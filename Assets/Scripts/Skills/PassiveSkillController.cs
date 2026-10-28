using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quản lý trạng thái và nâng cấp cho toàn bộ 8 Kỹ Năng Passive & Bổ Trợ trên Nhân Vật
/// </summary>
public class PassiveSkillController : MonoBehaviour
{
    public static PassiveSkillController Instance { get; private set; }

    [Header("Sub-skill Components (Tự động khởi tạo nếu thiếu)")]
    public PiercingShotSkill piercingShot;
    public OrbitSwordsSkill orbitSwords;
    public SeagullAllySkill seagullAlly;
    public GrenadeTossSkill grenadeToss;
    public EnergyShieldSkill energyShield;
    public BouncingBoomerangSkill bouncingBoomerang;

    // Lưu trữ cấp độ của tất cả các skill
    private readonly Dictionary<UpgradeData.UpgradeType, int> skillLevels = new Dictionary<UpgradeData.UpgradeType, int>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(this);

        EnsureComponentsExist();
    }

    private void EnsureComponentsExist()
    {
        if (piercingShot == null) piercingShot = GetComponent<PiercingShotSkill>() ?? gameObject.AddComponent<PiercingShotSkill>();
        if (orbitSwords == null) orbitSwords = GetComponent<OrbitSwordsSkill>() ?? gameObject.AddComponent<OrbitSwordsSkill>();
        if (seagullAlly == null) seagullAlly = GetComponent<SeagullAllySkill>() ?? gameObject.AddComponent<SeagullAllySkill>();
        if (grenadeToss == null) grenadeToss = GetComponent<GrenadeTossSkill>() ?? gameObject.AddComponent<GrenadeTossSkill>();
        if (energyShield == null) energyShield = GetComponent<EnergyShieldSkill>() ?? gameObject.AddComponent<EnergyShieldSkill>();
        if (bouncingBoomerang == null) bouncingBoomerang = GetComponent<BouncingBoomerangSkill>() ?? gameObject.AddComponent<BouncingBoomerangSkill>();

        // Đảm bảo OverdriveManager cũng tồn tại trong trận
        if (OverdriveManager.Instance == null)
        {
            GameObject odObj = new GameObject("OverdriveManager");
            odObj.AddComponent<OverdriveManager>();
        }
    }

    public int GetSkillLevel(UpgradeData.UpgradeType type)
    {
        return skillLevels.TryGetValue(type, out int lv) ? lv : 0;
    }

    public int GetMaxLevel(UpgradeData.UpgradeType type)
    {
        switch (type)
        {
            case UpgradeData.UpgradeType.DamageUp: return 5;
            case UpgradeData.UpgradeType.PiercingShot: return 3;
            case UpgradeData.UpgradeType.Multishot: return 5;
            case UpgradeData.UpgradeType.BurstFire: return 5;
            case UpgradeData.UpgradeType.OrbitSwords: return 3;
            case UpgradeData.UpgradeType.SeagullAlly: return 3;
            case UpgradeData.UpgradeType.GrenadeToss: return 3;
            case UpgradeData.UpgradeType.EnergyShield: return 3;
            case UpgradeData.UpgradeType.BouncingBoomerang: return 3;

            // Loại cũ
            case UpgradeData.UpgradeType.MoveSpeed: return 5;
            case UpgradeData.UpgradeType.MagnetRange: return 5;
            case UpgradeData.UpgradeType.FireRate: return 5;

            // Fallback items không giới hạn max level
            case UpgradeData.UpgradeType.SelectItem_Coin:
            case UpgradeData.UpgradeType.SelectItem_HealPack:
                return int.MaxValue;

            default: return 5;
        }
    }

    public bool IsMaxLevel(UpgradeData.UpgradeType type)
    {
        return GetSkillLevel(type) >= GetMaxLevel(type);
    }

    /// <summary>
    /// Nâng cấp kỹ năng được chọn từ LevelUpPanel
    /// </summary>
    public void ApplyUpgrade(UpgradeData.UpgradeType type)
    {
        EnsureComponentsExist();

        int currentLv = GetSkillLevel(type);
        int nextLv = currentLv + 1;
        skillLevels[type] = nextLv;

        Debug.Log($"<color=green>[UPGRADE APPLIED]</color> {type} lên cấp: {nextLv}/{GetMaxLevel(type)}");

        switch (type)
        {
            // 1. Tăng Sát Thương (Damage Up) [Max Lv 5]
            case UpgradeData.UpgradeType.DamageUp:
                if (PlayerAttack.Instance != null)
                {
                    PlayerAttack.Instance.damageUpLevel = nextLv;
                }
                break;

            // 2. Đạn Xuyên Thấu (Piercing Shot) [Max Lv 3]
            case UpgradeData.UpgradeType.PiercingShot:
                if (piercingShot != null) piercingShot.SetLevel(nextLv);
                break;

            // 3A. Đạn Tõe (Multishot) [Max Lv 5] - Độc quyền
            case UpgradeData.UpgradeType.Multishot:
                if (PlayerAttack.Instance != null)
                {
                    PlayerAttack.Instance.multishotLevel = nextLv;
                    PlayerAttack.Instance.burstFireLevel = 0; // Reset nếu chuyển nhánh
                    skillLevels[UpgradeData.UpgradeType.BurstFire] = 0;
                }
                break;

            // 3B. Đạn Liên Tục (Burst Fire) [Max Lv 5] - Độc quyền
            case UpgradeData.UpgradeType.BurstFire:
                if (PlayerAttack.Instance != null)
                {
                    PlayerAttack.Instance.burstFireLevel = nextLv;
                    PlayerAttack.Instance.multishotLevel = 0; // Reset nếu chuyển nhánh
                    skillLevels[UpgradeData.UpgradeType.Multishot] = 0;
                }
                break;

            // 4. Kiếm Bay (Orbit Swords) [Max Lv 3]
            case UpgradeData.UpgradeType.OrbitSwords:
                if (orbitSwords != null) orbitSwords.SetLevel(nextLv);
                break;

            // 5. Hải Âu Trợ Chiến (Seagull Ally) [Max Lv 3]
            case UpgradeData.UpgradeType.SeagullAlly:
                if (seagullAlly != null) seagullAlly.SetLevel(nextLv);
                break;

            // 6. Ném Lựu Đạn (Grenade Toss) [Max Lv 3]
            case UpgradeData.UpgradeType.GrenadeToss:
                if (grenadeToss != null) grenadeToss.SetLevel(nextLv);
                break;

            // 7. Giáp Hộ Thể (Energy Shield) [Max Lv 3]
            case UpgradeData.UpgradeType.EnergyShield:
                if (energyShield != null) energyShield.SetLevel(nextLv);
                break;

            // 8. Boomerang Dội Tường (Bouncing Boomerang) [Max Lv 3]
            case UpgradeData.UpgradeType.BouncingBoomerang:
                if (bouncingBoomerang != null) bouncingBoomerang.SetLevel(nextLv);
                break;

            // Thẻ dự phòng khi cạn pool (Fallback Items)
            case UpgradeData.UpgradeType.SelectItem_Coin:
                int rewardCoin = Random.Range(10, 26);
                if (PlayerStats.Instance != null)
                {
                    PlayerStats.Instance.AddGold(rewardCoin);
                }
                break;

            case UpgradeData.UpgradeType.SelectItem_HealPack:
                if (PlayerHealth.Instance != null)
                {
                    float healAmount = PlayerHealth.Instance.maxHealth * 0.30f; // 30% Max HP
                    PlayerHealth.Instance.Heal(Mathf.Max(100f, healAmount));
                }
                break;

            // Các kỹ năng cũ
            case UpgradeData.UpgradeType.MoveSpeed:
                if (PlayerStats.Instance != null) PlayerStats.Instance.UpgradeMoveSpeed();
                break;
            case UpgradeData.UpgradeType.MagnetRange:
                if (PlayerStats.Instance != null) PlayerStats.Instance.UpgradeMagnet();
                break;
            case UpgradeData.UpgradeType.FireRate:
                if (PlayerStats.Instance != null) PlayerStats.Instance.UpgradeFireRate();
                break;
        }
    }
}
