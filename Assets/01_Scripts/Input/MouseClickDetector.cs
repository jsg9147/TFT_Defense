using UnityEngine;
using UnityEngine.InputSystem;

public class MouseClickDetector2D : MonoBehaviour
{
    public LayerMask unitLayer;       // ���� Ŭ��
    public LayerMask tileLayer;       // Ÿ�� �����

    private GameObject lastHovered;

    private Vector2 originUnitPos;

    void Update()
    {
        MouseOverEvent();

        if (Mouse.current.leftButton.wasPressedThisFrame)
            LeftClickStart();

        if (Mouse.current.leftButton.isPressed)
            DragUpdate();

        if (Mouse.current.leftButton.wasReleasedThisFrame)
            LeftClickEnd();
    }

    void LeftClickStart()
    {
        Vector2 worldPos = GetMouseWorldPos();
        RaycastHit2D[] hits = Physics2D.RaycastAll(worldPos, Vector2.zero, 0.1f, unitLayer);

        foreach (var hit in hits)
        {
            if (!hit.collider.isTrigger)
            {
                Unit unit = hit.collider.GetComponent<Unit>();
                if (unit == null)
                {
                    Debug.Log($"{hit.transform.name} �� ������ �ƴմϴ�!");
                    return;
                }

                originUnitPos = unit.transform.position;
                UnitDragHandler.Instance.StartDragging(unit);
                break;
            }
        }
    }


    void DragUpdate()
    {
        if (UnitDragHandler.Instance.IsDragging())
        {
            UnitDragHandler.Instance.UpdateDragging();
        }
    }

    void LeftClickEnd()
    {
        if (UnitDragHandler.Instance.IsDragging())
        {
            Vector2 worldPos = GetMouseWorldPos();
            RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero, 0.1f, tileLayer);

            if (hit.collider != null)
            {
                Transform tile = hit.collider.transform;
                Unit draggingUnit = UnitDragHandler.Instance.GetDraggingUnit();
                draggingUnit.transform.position = tile.position;
                draggingUnit.isPlaced = true;

                Debug.Log($"������ Ÿ�� {tile.name}�� ��ġ��");
            }
            else
            {
                Debug.Log("Ÿ���� �ƴ�! ���� ��ġ ��� �Ǵ� ����ġ ����");
                Unit draggingUnit = UnitDragHandler.Instance.GetDraggingUnit();
                draggingUnit.transform.position = originUnitPos; // ���� ��ġ�� �ǵ�����
            }

            UnitDragHandler.Instance.StopDragging();
        }
    }

    void MouseOverEvent()
    {
        Vector2 worldPos = GetMouseWorldPos();
        RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero, 0.1f, tileLayer);

        if (hit.collider != null)
        {
            GameObject hoveredObj = hit.collider.gameObject;
            if (hoveredObj != lastHovered)
            {
                if (lastHovered != null && lastHovered.TryGetComponent<SpriteRenderer>(out var lastRenderer))
                {
                    lastRenderer.color = new(1,1,1,0.3f);
                }

                if (hoveredObj.TryGetComponent<SpriteRenderer>(out var renderer))
                {
                    renderer.color = new(0, 1, 0, 0.3f);
                }

                lastHovered = hoveredObj;
            }
        }
        else
        {
            if (lastHovered != null && lastHovered.TryGetComponent<SpriteRenderer>(out var renderer))
                renderer.color = Color.white;

            lastHovered = null;
        }
    }

    Vector3 GetMouseWorldPos()
    {
        Vector2 screenPos = Mouse.current.position.ReadValue();
        return Camera.main.ScreenToWorldPoint(screenPos);
    }
}
