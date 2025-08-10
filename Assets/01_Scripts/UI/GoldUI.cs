using UnityEngine;
using TMPro;

public class GoldUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI goldText;

    private void OnEnable()
    {
        // CurrencyManager �̺�Ʈ ����
        CurrencyManager.Instance.OnGoldChanged += UpdateGoldUI;
        // �ʱ� �� ǥ��
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
