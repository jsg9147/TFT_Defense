using UnityEngine;

public class UnitPlacementManager : MonoBehaviour
{
    public static UnitPlacementManager Instance;

    [Header("설정")]
    public GameObject unitPrefab;

    private UnitData selectedUnitData = null;
    private bool isPlacing = false;
    public bool IsPlacing => isPlacing;

    void Awake() => Instance = this;

    public void SetSelectedUnit(UnitData data)
    {
        selectedUnitData = data;
        isPlacing = true;
        Debug.Log($"{data.unitName} 배치 준비됨");
    }

    public void CancelPlacement()
    {
        isPlacing = false;
        selectedUnitData = null;
    }

    /// <summary>
    /// 월드 좌표 기준 배치. 성공 시 true, 실패 시 false.
    /// 성공하면 배치 모드 해제, 실패하면 선택 유지(다른 칸 시도 가능).
    /// </summary>
    public bool TryPlaceUnit(Vector3 worldPos)
    {
        if (!isPlacing || selectedUnitData == null)
            return false;

        var gm = GridManager.Instance;
        if (gm == null) return false;

        Vector3Int cellPos = gm.WorldToCell(worldPos);

        if (!gm.IsPlaceable(cellPos))
        {
            Debug.Log("해당 위치는 배치 불가");
            return false;
        }

        // 유닛 생성
        GameObject go = Instantiate(unitPrefab, gm.CellToWorldCenter(cellPos), Quaternion.identity);
        Unit unit = go.GetComponent<Unit>();
        unit.Init(selectedUnitData);

        SynergyManager.Instance.RegisterUnit(unit);

        if (gm.TryPlaceUnit(unit, cellPos))
        {
            Debug.Log($"유닛 {unit.name}이 셀 {cellPos}에 배치됨");
            // 성공 시 배치 모드 해제
            isPlacing = false;
            selectedUnitData = null;
            return true;
        }
        else
        {
            Debug.LogWarning("배치 실패: 이미 점유된 칸");
            Destroy(go);
            // 실패 시 선택 유지
            return false;
        }
    }

    /// <summary>
    /// 셀 기준 배치. 내부에서 월드 변환 후 TryPlaceUnit 호출.
    /// </summary>
    public bool TryPlaceAtCell(Vector3Int cell)
    {
        var gm = GridManager.Instance;
        if (gm == null) return false;
        return TryPlaceUnit(gm.CellToWorldCenter(cell));
    }
}
