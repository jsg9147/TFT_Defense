using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopSlotUI : MonoBehaviour
{
    [Header("UI 요소")]
    public Image unitIconImage;
    public TextMeshProUGUI unitNameText;
    public TextMeshProUGUI starText;
    public Button buyButton;

    [HideInInspector]
    public UnitData unitData;

    private System.Action<UnitData> onBuyCallback;

    /// <summary>
    /// 슬롯 UI를 초기화하는 메서드
    /// </summary>
    public void Init(UnitData data, System.Action<UnitData> onBuy)
    {
        unitData = data;
        onBuyCallback = onBuy;

        unitIconImage.sprite = data.icon;
        unitNameText.text = data.unitName;
        starText.text = $"★ {data.cost}";  // 상점은 항상 1성 기준

        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(OnBuyClicked);
    }

    private void OnBuyClicked()
    {
        onBuyCallback?.Invoke(unitData);
    }
}
