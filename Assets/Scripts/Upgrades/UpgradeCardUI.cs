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
        if (skillIcon != null)
        {
            if (icon != null)
            {
                skillIcon.sprite = icon;
                skillIcon.color = Color.white;
                skillIcon.preserveAspect = true;
                skillIcon.enabled = true;
            }
            else
            {
                skillIcon.enabled = false;
            }
        }

        if (skillName != null)
        {
            skillName.text = name;
            skillName.textWrappingMode = TextWrappingModes.NoWrap; // Đảm bảo tên skill luôn gọn trên 1 dòng
        }
        if (skillDescription != null) skillDescription.text = desc;

        if (selectButton != null)
        {
            // Tắt triệt để mọi persistent event (ví dụ UpgradeMoveSpeed cũ được kéo trong Inspector)
            for (int i = 0; i < selectButton.onClick.GetPersistentEventCount(); i++)
            {
                selectButton.onClick.SetPersistentListenerState(i, UnityEngine.Events.UnityEventCallState.Off);
            }

            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(() => onClickAction?.Invoke());
        }
    }
}