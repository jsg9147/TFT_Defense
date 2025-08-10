using UnityEngine;
using TMPro;

public class GoldUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI goldText;

    private void OnEnable()
    {
        // CurrencyManager 이벤트 구독
        CurrencyManager.Instance.OnGoldChanged += UpdateGoldUI;
        // 초기 값 표시
        UpdateGoldUI(CurrencyManager.Instance.Gold);
    }

    private void OnDisable()
    {
        CurrencyManager.Instance.OnGoldChanged -= UpdateGoldUI;
    }

    private void UpdateGoldUI(int gold)
    {
        goldText.text = gold.ToString();
    }
}
