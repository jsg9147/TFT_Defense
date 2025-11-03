using UnityEngine;
using TMPro;

public class EssenceUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI essenceUIText;

    private void OnEnable()
    {
        // CurrencyManager 이벤트 구독
        CurrencyManager.Instance.OnEssenceChanged += UpdateEssenceUI;
        // 초기 값 표시
        UpdateEssenceUI(CurrencyManager.Instance.Essence);
    }

    private void OnDisable()
    {
        CurrencyManager.Instance.OnEssenceChanged -= UpdateEssenceUI;
    }

    private void UpdateEssenceUI(int gold)
    {
        essenceUIText.text = gold.ToString();
    }
}
