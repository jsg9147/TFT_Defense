using UnityEngine;
using UnityEngine.InputSystem;

public class MouseClickDetector : MonoBehaviour
{
    [Header("Layers")]
    public LayerMask unitLayer;  // ���� ����
    public LayerMask slotLayer;  // ���� ����(�ݶ��̴� ���� ��)

    [Header("�Ǹ� �Է�")]
    [Tooltip("��Ŭ������ Ŀ�� �Ʒ� ���� �Ǹ�")]
    public bool rightClickSell = true;

    [Tooltip("���õ� ���� Delete Ű�� �Ǹ�")]
    public bool deleteKeySellSelected = true;

    private Vector3 originUnitPos;
    private BoardSlot lastHovered; // ���� ������ ����

    void Update()
    {
        MouseOverSlot();

        if (Mouse.current.leftButton.wasPressedThisFrame)
            BeginDragIfUnit();

        if (Mouse.current.leftButton.isPressed)
            UpdateDragFollow();

        if (Mouse.current.leftButton.wasReleasedThisFrame)
            EndDragTryPlace();

        // --- �߰�: ��Ŭ�� �Ǹ� ---
        if (rightClickSell && Mouse.current.rightButton.wasPressedThisFrame)
            TrySellUnderCursor();

        // --- �߰�: Delete Ű�� '���� ����' �Ǹ� ---
        if (deleteKeySellSelected && Keyboard.current.deleteKey.wasPressedThisFrame)
            TrySellSelectedUnit();
    }

    // --- ���� ���� ���� ---
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
            UnitDragHandler.Instance.StartDragging(unit);
            return;
        }
    }

    // --- �巡�� �� �̵�(�ð���) ---
    void UpdateDragFollow()
    {
        if (!UnitDragHandler.Instance.IsDragging()) return;
        var unit = UnitDragHandler.Instance.GetDraggingUnit();
        var world = GetMouseWorld();
        unit.transform.position = world;
    }

    // --- �巡�� ����: ���� �� ��ġ �õ� ---
    void EndDragTryPlace()
    {
        if (!UnitDragHandler.Instance.IsDragging()) return;

        var unit = UnitDragHandler.Instance.GetDraggingUnit();
        var world = GetMouseWorld();
        var gm = GridManager.Instance;

        var cell = gm.WorldToCell(world);

        if (gm.IsPlaceable(cell))
        {
            if (!gm.TryPlaceUnit(unit, cell))
                unit.transform.position = originUnitPos;
            else
                Debug.Log($"���� {unit.name} �� Cell {cell}");
        }
        else
        {
            unit.transform.position = originUnitPos;
        }

        UnitDragHandler.Instance.StopDragging();
    }

    // --- ���� ���̶���Ʈ ---
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
                bool can = gm.IsPlaceable(slot.Cell);
                slot.SetHighlight(can);
                lastHovered = slot;
                return;
            }
        }
        lastHovered = null;
    }

    // --- �߰�: Ŀ�� �Ʒ� ���� ��Ŭ�� �Ǹ� ---
    // --- �߰�: Ŀ�� �Ʒ� ���� ��Ŭ�� �Ǹ� ---
    void TrySellUnderCursor()
    {
        // �巡�� ���̸� �Ǹ����� ����(���۵� ����)
        if (UnitDragHandler.Instance.IsDragging()) return;

        Vector3 world = GetMouseWorld();

        // 1) ����Ʈ ��� ���� �ݰ����� ���뵵 �� (ī�޶�/��Ʈ �����Ͽ� ���� 0.2~0.4 ����)
        const float pickRadius = 0.1f;
        var hits = Physics2D.OverlapCircleAll(world, pickRadius, unitLayer);

        Unit target = null;

        // 2) ��� ��Ʈ�� ��ȸ�ϸ鼭 Unit�� ��Ȯ�� ã�� (trigger/��trigger ��� ���)
        foreach (var h in hits)
        {
            if (!h) continue;
            var u = h.GetComponent<Unit>();
            if (u != null) { target = u; break; }
        }

        if (target == null) return;

        if (UnitSellManager.Instance.SellUnit(target, out var refunded))
        {
            // ���� ����(�ִٸ�)
            if (UnitSelectionManager.Instance != null &&
                UnitSelectionManager.Instance.GetSelectedUnit() == target)
                UnitSelectionManager.Instance.Deselect();

            Debug.Log($"��Ŭ�� �Ǹ�: +{refunded} Gold");
        }
    }


    // --- �߰�: ���� ���� Delete �Ǹ� ---
    void TrySellSelectedUnit()
    {
        var sel = UnitSelectionManager.Instance != null ? UnitSelectionManager.Instance.GetSelectedUnit() : null;
        if (sel == null) return;

        if (UnitSellManager.Instance.SellUnit(sel, out var refunded))
        {
            UnitSelectionManager.Instance.Deselect();
            Debug.Log($"���� ���� �Ǹ�: +{refunded} Gold");
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
