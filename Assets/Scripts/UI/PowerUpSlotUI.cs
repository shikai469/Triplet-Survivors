using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PowerUpSlotUI : MonoBehaviour
{
    [Header("UI Bindings")]
    public TextMeshProUGUI textName;
    public Image imageIcon;
    public Transform pipsContainer;
    [HideInInspector] public GameObject pipPrefab;

    public Color pipInactiveColor = new Color(0.25f, 0.25f, 0.25f, 0.8f);
    public Color pipActiveColor = new Color(1f, 0.6f, 0f, 1f);

    private Image[] pips;

    public void Setup(ShopUpgradeItem data, int currentLevel, System.Action onClickAction)
    {
        if (textName != null) textName.text = data.displayName;
        if (imageIcon != null && data.icon != null) imageIcon.sprite = data.icon;

        if (pipsContainer != null)
        {
            for (int i = pipsContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(pipsContainer.GetChild(i).gameObject);
            }

            pips = new Image[data.maxLevel];
            for (int i = 0; i < data.maxLevel; i++)
            {
                if (pipPrefab != null)
                {
                    GameObject pipObj = Instantiate(pipPrefab, pipsContainer);
                    pipObj.name = $"Pip_{i + 1}";

                    Image fillImg = pipObj.transform.Find("Fill") != null
                        ? pipObj.transform.Find("Fill").GetComponent<Image>()
                        : pipObj.GetComponent<Image>();

                    pips[i] = fillImg;
                }
            }

            UpdateLevel(currentLevel);
        }

        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => onClickAction?.Invoke());
        }
    }

    public void UpdateLevel(int currentLevel)
    {
        if (pips == null) return;

        for (int i = 0; i < pips.Length; i++)
        {
            if (pips[i] != null)
            {
                if (pips[i].gameObject != pips[i].transform.parent.gameObject && pips[i].transform.parent != pipsContainer)
                {
                    pips[i].gameObject.SetActive(i < currentLevel);
                }
                else
                {
                    pips[i].color = (i < currentLevel) ? pipActiveColor : pipInactiveColor;
                }
            }
        }
    }
}