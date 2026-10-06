using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeCardUI : MonoBehaviour
{
    [Header("UI References")]
    public Image skillIcon;
    public TextMeshProUGUI skillName;
    public TextMeshProUGUI skillDescription;
    public Button selectButton;

    // Hàm nhận dữ liệu hiển thị từ bộ bốc thăm kỹ năng
    public void SetCardData(Sprite icon, string name, string desc, System.Action onClickAction)
    {
        if (skillIcon != null) skillIcon.sprite = icon;
        if (skillName != null) skillName.text = name;
        if (skillDescription != null) skillDescription.text = desc;

        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(() => onClickAction?.Invoke());
        }
    }
}