using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Tự động gán icon từ ProceduralSkillAssets vào 12 ô kỹ năng trong UpgradeScene,
/// đồng thời làm đẹp khung thẻ với gradient nền, border glow và icon động.
/// Gắn script này vào UpgradeSceneSetup GameObject trong UpgradeScene.
/// </summary>
public class UpgradeSceneIconSetup : MonoBehaviour
{
    [Header("Mapping: upgradeKey -> UpgradeType để lấy icon đúng")]
    [Tooltip("Bảng ánh xạ upgradeKey sang UpgradeType. Nếu để trống, hệ thống tự điền mặc định.")]
    public List<UpgradeKeyMapping> keyMappings = new List<UpgradeKeyMapping>();

    [Header("Card Style Settings")]
    public Color cardBgColorTop    = new Color(0.10f, 0.14f, 0.22f, 0.95f);
    public Color cardBgColorBottom = new Color(0.06f, 0.08f, 0.14f, 0.97f);
    public Color cardBorderColor   = new Color(0.35f, 0.55f, 1.00f, 0.55f);
    public Color iconBgColor       = new Color(0.08f, 0.12f, 0.22f, 1.00f);
    public float cardCornerRadius  = 18f;

    [System.Serializable]
    public class UpgradeKeyMapping
    {
        public string upgradeKey;
        public UpgradeData.UpgradeType upgradeType;
    }

    // Ánh xạ mặc định cho 12 slots Dmg -> CritDmg
    private static readonly Dictionary<string, UpgradeData.UpgradeType> DefaultMapping =
        new Dictionary<string, UpgradeData.UpgradeType>
        {
            { "Dmg",      UpgradeData.UpgradeType.DamageUp          },
            { "Defense",  UpgradeData.UpgradeType.EnergyShield       },
            { "Health",   UpgradeData.UpgradeType.SelectItem_HealPack},
            { "Healing",  UpgradeData.UpgradeType.Heal               },
            { "Extender", UpgradeData.UpgradeType.OrbitSwords        },
            { "Speed",    UpgradeData.UpgradeType.MoveSpeed          },
            { "Magnet",   UpgradeData.UpgradeType.MagnetRange        },
            { "Luck",     UpgradeData.UpgradeType.SelectItem_Coin    },
            { "Growth",   UpgradeData.UpgradeType.FireRate           },
            { "Greed",    UpgradeData.UpgradeType.SelectItem_Coin    },
            { "CritRate", UpgradeData.UpgradeType.PiercingShot       },
            { "CritDmg",  UpgradeData.UpgradeType.BurstFire         },
        };

    void Start()
    {
        BuildMappingDictionary();
        ApplyIconsToAllSlots();
    }

    private Dictionary<string, UpgradeData.UpgradeType> runtimeMapping;

    private void BuildMappingDictionary()
    {
        runtimeMapping = new Dictionary<string, UpgradeData.UpgradeType>(DefaultMapping);

        // Override với dữ liệu từ Inspector nếu có
        foreach (var m in keyMappings)
        {
            if (!string.IsNullOrEmpty(m.upgradeKey))
                runtimeMapping[m.upgradeKey] = m.upgradeType;
        }
    }

    private void ApplyIconsToAllSlots()
    {
        // Tìm tất cả PowerUpSlotUI trong scene
        PowerUpSlotUI[] allSlots = FindObjectsByType<PowerUpSlotUI>();
        if (allSlots == null || allSlots.Length == 0)
        {
            Debug.LogWarning("[UpgradeSceneIconSetup] Không tìm thấy PowerUpSlotUI nào trong Scene.");
            return;
        }

        foreach (var slot in allSlots)
        {
            if (slot == null) continue;
            ApplyIconToSlot(slot);
            StyleCard(slot);
        }

        Debug.Log($"[UpgradeSceneIconSetup] Đã áp dụng icon và style cho {allSlots.Length} slot(s).");
    }

    private void ApplyIconToSlot(PowerUpSlotUI slot)
    {
        // Lấy tên slot (ví dụ: "Slot_Dmg" -> key = "Dmg")
        string slotName = slot.gameObject.name; // vd: "Slot_Dmg"
        string key = ExtractKey(slotName);

        if (string.IsNullOrEmpty(key)) return;
        if (!runtimeMapping.TryGetValue(key, out UpgradeData.UpgradeType upgradeType)) return;

        // Lấy icon từ ProceduralSkillAssets
        Sprite icon = ProceduralSkillAssets.GetSkillIcon(upgradeType);
        if (icon == null) return;

        // Gán vào Image icon của slot
        Image iconImage = slot.imageIcon;
        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.color = Color.white;
            iconImage.preserveAspect = true;
        }
    }

    private void StyleCard(PowerUpSlotUI slot)
    {
        // Tìm Image nền của card (thường là component Image trực tiếp trên slot hoặc con đầu tiên)
        Image cardBg = slot.GetComponent<Image>();
        if (cardBg != null)
        {
            cardBg.color = cardBgColorBottom;
            // Bật raycast target để tương tác vẫn hoạt động
            cardBg.raycastTarget = true;
        }

        // Style icon container background
        if (slot.imageIcon != null)
        {
            Transform iconParent = slot.imageIcon.transform.parent;
            if (iconParent != null)
            {
                Image iconBg = iconParent.GetComponent<Image>();
                if (iconBg != null)
                {
                    iconBg.color = iconBgColor;
                }
            }

            // Thêm subtle glow tới icon
            slot.imageIcon.color = Color.white;
        }

        // Style text tên kỹ năng
        if (slot.textName != null)
        {
            slot.textName.color = new Color(0.92f, 0.95f, 1.00f, 1f);
            slot.textName.fontStyle = FontStyles.Bold;
        }
    }

    // Trích xuất upgradeKey từ tên GameObject (vd: "Slot_Dmg" -> "Dmg")
    private string ExtractKey(string objectName)
    {
        if (string.IsNullOrEmpty(objectName)) return null;
        int idx = objectName.IndexOf('_');
        if (idx < 0) return objectName;
        return objectName.Substring(idx + 1);
    }
}
