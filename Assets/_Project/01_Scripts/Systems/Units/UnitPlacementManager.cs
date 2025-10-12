using UnityEngine;

public class UnitPlacementManager : MonoBehaviour
{
    public static UnitPlacementManager Instance;

    [Header("설정")]
    public GameObject unitPrefab; // 인스턴스화할 유닛 본체 (빈 오브젝트에 SpriteRenderer + UnitInstance.cs 포함)

    private UnitData selectedUnitData = null;
    private bool isPlacing = false;

    void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// 상점에서 구매된 유닛을 배치 준비 상태로 설정
    /// </summary>
    public void SetSelectedUnit(UnitData data)
    {
        selectedUnitData = data;
        isPlacing = true;
        Debug.Log($"{data.unitName} 배치 준비됨");
    }

    /// <summary>
    /// 맵의 타일이 클릭되었을 때 호출
    /// </summary>
    public void TryPlaceUnit(Vector3 worldPos)
    {
        if (!isPlacing || selectedUnitData == null)
            return;

        Vector3Int gridPos = GridManager.Instance.WorldToCell(worldPos);
        if (!GridManager.Instance.IsPlaceable(gridPos))
        {
            Debug.Log("해당 위치는 배치 불가");
            return;
        }

        GameObject unit = Instantiate(unitPrefab, GridManager.Instance.CellToWorldCenter(gridPos), Quaternion.identity);
        unit.GetComponent<Unit>().Init(selectedUnitData);

        GridManager.Instance.Occupy(gridPos);
        isPlacing = false;
        selectedUnitData = null;
    }
}
