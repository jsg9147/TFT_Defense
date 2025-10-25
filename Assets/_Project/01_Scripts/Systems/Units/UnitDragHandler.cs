using UnityEngine;
using UnityEngine.InputSystem;

public class UnitDragHandler : MonoBehaviour
{
    public static UnitDragHandler Instance;

    private Unit draggingUnit;
    private Vector3 offset;

    void Awake() => Instance = this;

    /// <summary> 드래그 시작 (유닛 클릭 시) </summary>
    public void StartDragging(Unit unit)
    {
        if (unit == null) return;

        draggingUnit = unit;
        Vector3 mouseWorld = GetMouseWorldPos();
        offset = unit.transform.position - mouseWorld;
        draggingUnit.SetHighlight(true);
    }

    /// <summary> 드래그 중: 마우스를 따라 이동 (칸 스냅 가능) </summary>
    public void UpdateDragging()
    {
        if (draggingUnit == null) return;

        Vector3 worldPos = GetMouseWorldPos();

        // 👉 칸 스냅 모드 (GridManager가 있다면)
        if (GridManager.Instance != null)
        {
            Vector3Int cell = GridManager.Instance.WorldToCell(worldPos);
            worldPos = GridManager.Instance.CellToWorldCenter(cell);
        }
        else
        {
            // 아니면 그냥 마우스 따라가기
            worldPos += offset;
        }

        worldPos.z = 0f; // 2D에서 z고정
        draggingUnit.transform.position = worldPos;
    }

    /// <summary> 드래그 종료 </summary>
    public void StopDragging()
    {
        if (draggingUnit != null)
        {
            draggingUnit.SetHighlight(false);
            draggingUnit = null;
        }
    }

    public bool IsDragging() => draggingUnit != null;
    public Unit GetDraggingUnit() => draggingUnit;

    private Vector3 GetMouseWorldPos()
    {
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);

        // ⬇️ 중요: z값을 카메라에서의 거리로 강제 지정 (안 하면 월드 변환 오류)
        worldPos.z = 0f;
        return worldPos;
    }
}
