using UnityEngine;

/// <summary>
/// 유닛 판매 전담 매니저.
/// - 환급률/별승급 배율/전투 중 판매 허용 같은 정책을 한 곳에서 관리
/// - Grid, Currency, Synergy와의 연결을 캡슐화
/// </summary>
public class UnitSellManager : MonoSingleton<UnitSellManager>
{
    [Header("판매 정책")]
    [Range(0f, 1f)]
    [SerializeField] private float refundRate = 0.8f;          // 기본 환급률(80%)
    [SerializeField] private bool allowSellDuringBattle = true; // 전투 중 판매 허용
    [Tooltip("구매가 계산: cost * pricePerCost. (예: 코스트 1 → 2골드)")]
    [SerializeField] private int pricePerCost = 2;

    [Header("별 승급 환급 가중치 (1~5성 인덱스 사용, 부족하면 마지막 값 사용)")]
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

    /// <summary> 해당 유닛의 '구매가' 추정치 (테이블/상점 연동 시 교체 가능) </summary>
    public int GetPurchasePrice(Unit unit)
    {
        if (unit == null || unit.data == null) return 0;
        int basePrice = Mathf.Max(0, unit.data.cost) * Mathf.Max(1, pricePerCost);
        // 필요 시 희귀도/시너지 프리미엄 등을 더할 수 있음
        return basePrice;
    }

    /// <summary> 환급액 계산 (환급률 * 별 가중치 * 구매가) </summary>
    public int GetRefundAmount(Unit unit)
    {
        int buy = GetPurchasePrice(unit);
        if (buy <= 0) return 0;

        int starIndex = Mathf.Clamp(unit.starLevel - 1, 0, starRefundMultipliers.Length - 1);
        float starMul = starRefundMultipliers[starIndex];
        float raw = buy * refundRate * starMul;
        return Mathf.Max(0, Mathf.RoundToInt(raw));
    }

    /// <summary> 월드 좌표의 유닛을 찾아 판매 (마우스 우클릭 등) </summary>
    public bool SellUnitAtWorld(Vector3 world, LayerMask unitLayer, out int refunded)
    {
        refunded = 0;

        // 전투 중 판매 허용 여부
        if (!allowSellDuringBattle && GameManager.Instance != null && GameManager.Instance.IsBattlePhase())
            return false;

        var hit = Physics2D.OverlapPoint(world, unitLayer);
        if (hit == null) return false;

        var unit = hit.GetComponent<Unit>();
        if (unit == null) return false;

        return SellUnit(unit, out refunded);
    }

    /// <summary> 직접 유닛을 넘겨 판매 </summary>
    public bool SellUnit(Unit unit, out int refunded)
    {
        refunded = 0;
        if (unit == null) return false;

        // 전투 중 판매 허용 여부
        if (!allowSellDuringBattle && GameManager.Instance != null && GameManager.Instance.IsBattlePhase())
            return false;

        // 환급 골드 계산
        int amount = GetRefundAmount(unit);

        // 보드 점유 해제
        var gm = GridManager.Instance;
        if (gm != null && gm.grid != null)
        {
            Vector3Int cell = gm.WorldToCell(unit.transform.position);
            gm.TryRemoveUnit(cell, out _);
        }

        SynergyManager.Instance.UnregisterUnit(unit);

        // 골드 지급
        if (CurrencyManager.Instance != null && amount > 0)
            CurrencyManager.Instance.AddGold(amount);

        // 유닛 파괴
        Object.Destroy(unit.gameObject);

        refunded = amount;
        Debug.Log($"[Sell] {unit.name} 판매 → +{amount} Gold");
        return true;
    }
}
