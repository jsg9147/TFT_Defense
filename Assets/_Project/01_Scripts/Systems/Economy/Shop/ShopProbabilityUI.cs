using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ShopProbabilityUI : MonoBehaviour
{
    [Header("Ȯ�� ǥ�ÿ� �ؽ�Ʈ (1~5�� ����)")]
    public List<TextMeshProUGUI> rateTexts = new List<TextMeshProUGUI>();

    /// <summary>
    /// Ȯ�� ���̺� ������ �޾� UI�� ������Ʈ
    /// </summary>
    public void UpdateRates(float[] table)
    {
        float total = 0f;
        foreach (var val in table) total += val;

        int count = Mathf.Min(rateTexts.Count, table.Length);

        for (int i = 0; i < count; i++)
        {
            float normalized = (total > 0f) ? (table[i] / total * 100f) : 0f;
            rateTexts[i].text = $"{normalized:F0}%";
        }
    }

}
