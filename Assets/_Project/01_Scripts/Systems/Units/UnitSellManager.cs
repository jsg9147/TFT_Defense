using UnityEngine;

/// <summary>
/// ���� �Ǹ� ���� �Ŵ���.
/// - ȯ�޷�/���±� ����/���� �� �Ǹ� ��� ���� ��å�� �� ������ ����
/// - Grid, Currency, Synergy���� ������ ĸ��ȭ
/// </summary>
public class UnitSellManager : MonoSingleton<UnitSellManager>
{
    [Header("�Ǹ� ��å")]
    [Range(0f, 1f)]
    [SerializeField] private float refundRate = 0.8f;          // �⺻ ȯ�޷�(80%)
    [SerializeField] private bool allowSellDuringBattle = true; // ���� �� �Ǹ� ���
    [Tooltip("���Ű� ���: cost * pricePerCost. (��: �ڽ�Ʈ 1 �� 2���)")]
    [SerializeField] private int pricePerCost = 2;

    [Header("�� �±� ȯ�� ����ġ (1~5�� �ε��� ���, �����ϸ� ������ �� ���)")]
    [SerializeField] private float[] starRefundMultipliers = new float[] { 1.0f, 1.8f, 3.5f, 6.0f, 9.0f };

    public float RefundRate
    {
        get => refundRate;
        set => refundRate = Mathf.Clamp01(value);
    }

    public bool AllowSellDuringBattle
    {
        get => allowSellDuringBattle;
        set => allowSellDuringBattle = value;
    }

    /// <summary> �ش� ������ '���Ű�' ����ġ (���̺�/���� ���� �� ��ü ����) </summary>
    public int GetPurchasePrice(Unit unit)
    {
        if (unit == null || unit.data == null) return 0;
        int basePrice = Mathf.Max(0, unit.data.cost) * Mathf.Max(1, pricePerCost);
        // �ʿ� �� ��͵�/�ó��� �����̾� ���� ���� �� ����
        return basePrice;
    }

    /// <summary> ȯ�޾� ��� (ȯ�޷� * �� ����ġ * ���Ű�) </summary>
    public int GetRefundAmount(Unit unit)
    {
        int buy = GetPurchasePrice(unit);
        if (buy <= 0) return 0;

        int starIndex = Mathf.Clamp(unit.starLevel - 1, 0, starRefundMultipliers.Length - 1);
        float starMul = starRefundMultipliers[starIndex];
        float raw = buy * refundRate * starMul;
        return Mathf.Max(0, Mathf.RoundToInt(raw));
    }

    /// <summary> ���� ��ǥ�� ������ ã�� �Ǹ� (���콺 ��Ŭ�� ��) </summary>
    public bool SellUnitAtWorld(Vector3 world, LayerMask unitLayer, out int refunded)
    {
        refunded = 0;

        // ���� �� �Ǹ� ��� ����
        if (!allowSellDuringBattle && GameManager.Instance != null && GameManager.Instance.IsBattlePhase())
            return false;

        var hit = Physics2D.OverlapPoint(world, unitLayer);
        if (hit == null) return false;

        var unit = hit.GetComponent<Unit>();
        if (unit == null) return false;

        return SellUnit(unit, out refunded);
    }

    /// <summary> ���� ������ �Ѱ� �Ǹ� </summary>
    public bool SellUnit(Unit unit, out int refunded)
    {
        refunded = 0;
        if (unit == null) return false;

        // ���� �� �Ǹ� ��� ����
        if (!allowSellDuringBattle && GameManager.Instance != null && GameManager.Instance.IsBattlePhase())
            return false;

        // ȯ�� ��� ���
        int amount = GetRefundAmount(unit);

        // ���� ���� ����
        var gm = GridManager.Instance;
        if (gm != null && gm.grid != null)
        {
            Vector3Int cell = gm.WorldToCell(unit.transform.position);
            gm.TryRemoveUnit(cell, out _);
        }

        SynergyManager.Instance.UnregisterUnit(unit);

        // ��� ����
        if (CurrencyManager.Instance != null && amount > 0)
            CurrencyManager.Instance.AddGold(amount);

        // ���� �ı�
        Object.Destroy(unit.gameObject);

        refunded = amount;
        Debug.Log($"[Sell] {unit.name} �Ǹ� �� +{amount} Gold");
        return true;
    }
}
