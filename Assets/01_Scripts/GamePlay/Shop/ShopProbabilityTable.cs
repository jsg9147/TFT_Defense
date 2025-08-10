// Assets/Scripts/Shop/ShopProbabilityTable.cs
using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Probability Table", fileName = "ShopProbabilityTable_Lv1")]
public class ShopProbabilityTable : ScriptableObject
{
    [Header("�� ���̺��� ����� �÷��̾� ����(ǥ�ÿ�)")]
    public int level = 1;

    [Header("�ڽ�Ʈ�� ����ġ (���� 100�� �ʿ�� ����)")]
    public int cost1 = 60;
    public int cost2 = 30;
    public int cost3 = 10;
    public int cost4 = 0;
    public int cost5 = 0;

    /// <summary> ���� 0�̸� [1,0,0,0,0]�� ���� </summary>
    public void Normalize()
    {
        int sum = Mathf.Max(0, cost1) + Mathf.Max(0, cost2) + Mathf.Max(0, cost3) + Mathf.Max(0, cost4) + Mathf.Max(0, cost5);
        if (sum <= 0)
        {
            cost1 = 1; cost2 = cost3 = cost4 = cost5 = 0;
            return;
        }
        // ����ȭ�� UI�����θ� float�� ������ �Ŷ� ������ �״�� �ֵ� OK
    }

    /// <summary> [0..1] ��=1 �� ��ȯ�� �迭 ��ȯ (UI/�����ȿ� ���) </summary>
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
