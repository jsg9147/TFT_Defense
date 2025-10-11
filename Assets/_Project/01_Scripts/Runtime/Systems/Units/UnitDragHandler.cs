using UnityEngine;
using UnityEngine.InputSystem;

public class UnitDragHandler : MonoBehaviour
{
    public static UnitDragHandler Instance;

    private Unit draggingUnit;
    private Vector3 offset;

    void Awake()
    {
        Instance = this;
    }

    public void StartDragging(Unit unit)
    {
        draggingUnit = unit;
        offset = unit.transform.position - GetMouseWorldPos();
        draggingUnit.SetHighlight(true);
    }

    public void StopDragging()
    {
        if (draggingUnit != null)
        {
            draggingUnit.SetHighlight(false);
            draggingUnit = null;
        }
    }

    public void UpdateDragging()
    {
        if (draggingUnit != null)
        {
            draggingUnit.transform.position = GetMouseWorldPos() + offset;
        }
    }

    public bool IsDragging() => draggingUnit != null;

    public Unit GetDraggingUnit() => draggingUnit;

    private Vector3 GetMouseWorldPos()
    {
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        return Camera.main.ScreenToWorldPoint(mouseScreenPos);
    }
}
