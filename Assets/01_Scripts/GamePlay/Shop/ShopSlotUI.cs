using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopSlotUI : MonoBehaviour
{
    [Header("UI ���")]
    public Image unitIconImage;
    public TextMeshProUGUI unitNameText;
    public TextMeshProUGUI starText;
    public Button buyButton;

    [HideInInspector]
    public UnitData unitData;

    private System.Action<UnitData> onBuyCallback;

    /// <summary>
    /// ���� UI�� �ʱ�ȭ�ϴ� �޼���
    /// </summary>
    public void Init(UnitData data, System.Action<UnitData> onBuy)
    {
        unitData = data;
        onBuyCallback = onBuy;

        unitIconImage.sprite = data.icon;
        unitNameText.text = data.unitName;
        starText.text = $"�� {data.cost}";  // ������ �׻� 1�� ����

        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(OnBuyClicked);
    }

    private void OnBuyClicked()
    {
        onBuyCallback?.Invoke(unitData);
    }
}
