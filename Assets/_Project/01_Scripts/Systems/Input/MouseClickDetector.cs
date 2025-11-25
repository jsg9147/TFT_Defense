using UnityEngine;
using UnityEngine.InputSystem;

public class MouseClickDetector : MonoBehaviour
{
    [Header("Layers")]
    public LayerMask unitLayer;  // 유닛 선택
    public LayerMask slotLayer;  // 보드 슬롯(콜라이더 가진 셀)

    [Header("판매 입력")]
    [Tooltip("우클릭으로 커서 아래 유닛 판매")]
    public bool rightClickSell = true;

    [Tooltip("선택된 유닛 Delete 키로 판매")]
    public bool deleteKeySellSelected = true;

    private Vector3 originUnitPos;
    private BoardSlot lastHovered; // 슬롯 단위로 추적

    private Vector3Int originCell;
    private bool draggedFromBoard;


    void Update()
    {
        MouseOverSlot();

        if (Mouse.current.leftButton.wasPressedThisFrame)
            BeginDragIfUnit();

        if (Mouse.current.leftButton.isPressed)
            UpdateDragFollow();

        if (Mouse.current.leftButton.wasReleasedThisFrame)
            EndDragTryPlace();

        // --- 추가: 우클릭 판매 ---
        //if (rightClickSell && Mouse.current.rightButton.wasPressedThisFrame)
        //    TrySellUnderCursor();

        // --- 추가: Delete 키로 '선택 유닛' 판매 ---
        if (deleteKeySellSelected && Keyboard.current.deleteKey.wasPressedThisFrame)
            TrySellSelectedUnit();
    }

    // --- 유닛 선택 시작 ---
    void BeginDragIfUnit()
    {
        var world = GetMouseWorld();
        var hits = Physics2D.OverlapPointAll(world, unitLayer);
        foreach (var h in hits)
        {
            if (h.isTrigger) continue;
            var unit = h.GetComponent<Unit>();
            if (unit == null) continue;

            originUnitPos = unit.transform.position;

            var gm = GridManager.Instance;
            var cell = gm.WorldToCell(originUnitPos);

            // 이 유닛이 보드 위에 있는지 확인
            if (gm.GetUnitAt(cell) == unit)
            {
                originCell = cell;
                draggedFromBoard = true;
            }
            else
            {
                draggedFromBoard = false; // 벤치/상점 유닛 같은 경우
            }

            UnitDragHandler.Instance.StartDragging(unit);
            return;
        }
    }


    // --- 드래그 중 이동(시각용) ---
    void UpdateDragFollow()
    {
        if (!UnitDragHandler.Instance.IsDragging()) return;
        var unit = UnitDragHandler.Instance.GetDraggingUnit();
        var world = GetMouseWorld();
        unit.transform.position = world;
    }

    // --- 드래그 종료: 보드 셀 배치 시도 ---
    void EndDragTryPlace()
    {
        if (!UnitDragHandler.Instance.IsDragging()) return;

        var unit = UnitDragHandler.Instance.GetDraggingUnit();
        var world = GetMouseWorld();
        var gm = GridManager.Instance;
        var targetCell = gm.WorldToCell(world);

        bool placed = false;

        if (draggedFromBoard)
        {
            // === 보드에서 보드로 이동 ===
            // 같은 칸이면 그냥 원래 자리로 스냅만
            if (targetCell == originCell)
            {
                unit.transform.position = gm.CellToWorldCenter(originCell);
                placed = true;
            }
            else
            {
                // 이동은 TryMoveUnit 사용 (IsPlaceable 말고 내부에서 체크)
                placed = gm.TryMoveUnit(originCell, targetCell);
            }
        }
        else
        {
            // === 벤치/상점 유닛 → 보드 첫 배치 ===
            if (gm.IsPlaceable(targetCell))
                placed = gm.TryPlaceUnit(unit, targetCell);
        }

        if (!placed)
        {
            // 실패하면 원래 자리로 복귀
            unit.transform.position = originUnitPos;
        }

        UnitDragHandler.Instance.StopDragging();
        draggedFromBoard = false;
    }

    // --- 슬롯 하이라이트 ---
    void MouseOverSlot()
    {
        var world = GetMouseWorld();
        var hit = Physics2D.OverlapPoint(world, slotLayer);
        var gm = GridManager.Instance;

        if (lastHovered != null) lastHovered.SetHighlight(false);

        if (hit != null)
        {
            var slot = hit.GetComponent<BoardSlot>();
            if (slot != null)
            {
                bool can;
                if (UnitDragHandler.Instance.IsDragging() && draggedFromBoard)
                {
                    var dragging = UnitDragHandler.Instance.GetDraggingUnit();
                    if (slot.Cell == originCell)
                        can = true; // 원래 자리로는 항상 가능
                    else
                        can = gm.IsInBounds(slot.Cell) && !gm.IsBlocked(slot.Cell) && !gm.IsOccupied(slot.Cell);
                }
                else
                {
                    can = gm.IsPlaceable(slot.Cell);
                }
                slot.SetHighlight(can);

                lastHovered = slot;
                return;
            }
        }
        lastHovered = null;
    }

    // --- 추가: 커서 아래 유닛 우클릭 판매 ---
    // --- 추가: 커서 아래 유닛 우클릭 판매 ---
    void TrySellUnderCursor()
    {
        // 드래그 중이면 판매하지 않음(오작동 방지)
        if (UnitDragHandler.Instance.IsDragging()) return;

        Vector3 world = GetMouseWorld();

        // 1) 포인트 대신 작은 반경으로 관용도 ↑ (카메라/아트 스케일에 따라 0.2~0.4 조절)
        const float pickRadius = 0.1f;
        var hits = Physics2D.OverlapCircleAll(world, pickRadius, unitLayer);

        Unit target = null;

        // 2) 모든 히트를 순회하면서 Unit을 정확히 찾기 (trigger/비trigger 모두 허용)
        foreach (var h in hits)
        {
            if (!h) continue;
            var u = h.GetComponent<Unit>();
            if (u != null) { target = u; break; }
        }

        if (target == null) return;

        if (UnitSellManager.Instance.SellUnit(target, out var refunded))
        {
            // 선택 해제(있다면)
            if (UnitSelectionManager.Instance != null &&
                UnitSelectionManager.Instance.GetSelectedUnit() == target)
                UnitSelectionManager.Instance.Deselect();

            Debug.Log($"우클릭 판매: +{refunded} Gold");
        }
    }


    // --- 추가: 선택 유닛 Delete 판매 ---
    void TrySellSelectedUnit()
    {
        var sel = UnitSelectionManager.Instance != null ? UnitSelectionManager.Instance.GetSelectedUnit() : null;
        if (sel == null) return;

        if (UnitSellManager.Instance.SellUnit(sel, out var refunded))
        {
            UnitSelectionManager.Instance.Deselect();
            Debug.Log($"선택 유닛 판매: +{refunded} Gold");
        }
    }

    Vector3 GetMouseWorld()
    {
        var p = Mouse.current.position.ReadValue();
        var w = Camera.main.ScreenToWorldPoint(p);
        w.z = 0f;
        return w;
    }
}
