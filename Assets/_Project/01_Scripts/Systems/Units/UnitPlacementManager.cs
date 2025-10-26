using UnityEngine;

public class UnitPlacementManager : MonoBehaviour
{
    public static UnitPlacementManager Instance;

    [Header("Fallback 프리팹(없으면 생성 거부)")]
    public GameObject defaultUnitPrefab;

    [Header("설정")]
    public GameObject unitPrefab;

    private UnitData selectedUnitData = null;
    private UnitData _selected;

    private bool isPlacing = false;
    public bool IsPlacing => isPlacing;

    void Awake() => Instance = this;

    public void SetSelectedUnit(UnitData data) => _selected = data;

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

    public bool TryPlaceAtCell(Vector3Int cell)
    {
        if (_selected == null) return false;

        var gm = GridManager.Instance;
        if (!gm.IsPlaceable(cell)) return false;

        // 1) 스폰 포지션
        Vector3 pos = gm.CellToWorldCenter(cell);

        // 2) 프리팹 선택: UnitData 우선, 없으면 fallback
        GameObject prefab = _selected.unitPrefab != null ? _selected.unitPrefab : defaultUnitPrefab;
        if (prefab == null)
        {
            Debug.LogError("[UnitPlacement] unitPrefab / defaultUnitPrefab 둘 다 비었습니다.");
            return false;
        }

        // 3) 생성 + Init
        var go = Instantiate(prefab, pos, Quaternion.identity);
        var unit = go.GetComponent<Unit>();
        if (unit == null)
        {
            Debug.LogError("[UnitPlacement] 생성된 객체에 Unit 컴포넌트가 없습니다.");
            Destroy(go);
            return false;
        }

        unit.Init(_selected);

        // 4) 그리드 점유 등록
        gm.TryPlaceUnit(unit, cell);
        return true;
    }

    public void CancelPlacement() => _selected = null;
}
