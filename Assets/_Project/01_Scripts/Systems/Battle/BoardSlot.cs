using UnityEngine;
using UnityEngine.EventSystems;

public class BoardSlot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public Vector3Int Cell { get; private set; }
    [SerializeField] private GameObject highlight; // 옵셔널
    [SerializeField] private SpriteRenderer highlightRenderer; // 투명 스프라이트 등

    public void Init(Vector3Int cell) => Cell = cell;

    public void OnPointerClick(PointerEventData e)
    {
        var upm = UnitPlacementManager.Instance;
        var gm = GridManager.Instance;

        // 1) 상점 구매로 진입한 "배치 모드"면: 즉시 스폰+배치
        if (upm != null && upm.IsPlacing)
        {
            upm.TryPlaceUnit(gm.CellToWorldCenter(Cell));
            return;
        }

        // 2) 아니면 기존처럼 선택된 유닛(오브젝트) 이동/배치
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
