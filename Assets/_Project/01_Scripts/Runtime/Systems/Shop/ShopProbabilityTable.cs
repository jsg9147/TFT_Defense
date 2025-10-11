// Assets/Scripts/Shop/ShopProbabilityTable.cs
using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Probability Table", fileName = "ShopProbabilityTable_Lv1")]
public class ShopProbabilityTable : ScriptableObject
{
    [Header("이 테이블이 적용될 플레이어 레벨(표시용)")]
    public int level = 1;

    [Header("코스트별 가중치 (합이 100일 필요는 없음)")]
    public int cost1 = 60;
    public int cost2 = 30;
    public int cost3 = 10;
    public int cost4 = 0;
    public int cost5 = 0;

    /// <summary> 합이 0이면 [1,0,0,0,0]로 보정 </summary>
    public void Normalize()
    {
        int sum = Mathf.Max(0, cost1) + Mathf.Max(0, cost2) + Mathf.Max(0, cost3) + Mathf.Max(0, cost4) + Mathf.Max(0, cost5);
        if (sum <= 0)
        {
            cost1 = 1; cost2 = cost3 = cost4 = cost5 = 0;
            return;
        }
        // 정규화는 UI용으로만 float을 돌려줄 거라 정수는 그대로 둬도 OK
    }

    /// <summary> [0..1] 합=1 로 변환된 배열 반환 (UI/랜덤픽에 사용) </summary>
    public float[] GetProbabilities()
    {
        int[] w = { Mathf.Max(0, cost1), Mathf.Max(0, cost2), Mathf.Max(0, cost3), Mathf.Max(0, cost4), Mathf.Max(0, cost5) };
        int sum = 0; foreach (var x in w) sum += x;
        if (sum <= 0) return new float[] { 1f, 0f, 0f, 0f, 0f };

        return new float[]
        {
            (float)w[0]/sum,
            (float)w[1]/sum,
            (float)w[2]/sum,
            (float)w[3]/sum,
            (float)w[4]/sum,
        };
    }
}
