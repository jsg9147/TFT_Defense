using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SynergyTagUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI labelText;

    public void Set(Sprite icon, string label)
    {
        if (iconImage)
        {
            if (icon != null)
            {
                iconImage.gameObject.SetActive(true);
                iconImage.sprite = icon;
            }
            else
            {
                iconImage.gameObject.SetActive(false);
            }
        }

        if (labelText) labelText.text = label;
    }
}
