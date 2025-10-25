using UnityEngine;
using UnityEngine.EventSystems;

public class BoardSlot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public Vector3Int Cell { get; private set; }
    [SerializeField] private GameObject highlight; // �ɼų�
    [SerializeField] private SpriteRenderer highlightRenderer; // ���� ��������Ʈ ��

    public void Init(Vector3Int cell) => Cell = cell;

    public void OnPointerClick(PointerEventData e)
    {
        var upm = UnitPlacementManager.Instance;
        var gm = GridManager.Instance;

        // 1) ���� ���ŷ� ������ "��ġ ���"��: ��� ����+��ġ
        if (upm != null && upm.IsPlacing)
        {
            upm.TryPlaceUnit(gm.CellToWorldCenter(Cell));
            return;
        }

        // 2) �ƴϸ� ����ó�� ���õ� ����(������Ʈ) �̵�/��ġ
        var selected = UnitSelectionManager.Instance?.GetSelectedUnit();
        if (selected == null || gm == null) return;

        var prevCell = gm.WorldToCell(selected.transform.position);
        if (gm.GetUnitAt(prevCell) == selected)
            gm.TryMoveUnit(prevCell, Cell);
        else
            gm.TryPlaceUnit(selected, Cell);
    }

    public void OnPointerEnter(PointerEventData e)
    {
        if (highlight) highlight.SetActive(GridManager.Instance.IsPlaceable(Cell));
    }

    public void OnPointerExit(PointerEventData e)
    {
        if (highlight) highlight.SetActive(false);
    }

    public void SetHighlight(bool on)
    {
        if (!highlightRenderer) return;
        highlightRenderer.enabled = on;
    }
}
