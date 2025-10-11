using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ShopProbabilityUI : MonoBehaviour
{
    [Header("확률 표시용 텍스트 (1~5성 순서)")]
    public List<TextMeshProUGUI> rateTexts = new List<TextMeshProUGUI>();

    /// <summary>
    /// 확률 테이블 정보를 받아 UI를 업데이트
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
