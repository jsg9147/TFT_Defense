using UnityEngine;
using System.Collections.Generic;

public class GridCellManager : MonoBehaviour
{
    public GameObject cellPrefab;
    public Transform cellParent;

    private readonly Dictionary<Vector3Int, BoardSlot> slots = new();

    void Start() => GenerateGrid();

    void GenerateGrid()
    {
        var gm = GridManager.Instance;
        if (gm == null || gm.grid == null) { Debug.LogWarning("GridManager/grid 없음"); return; }

        // 재생성 시 정리
        for (int i = cellParent.childCount - 1; i >= 0; --i)
            Destroy(cellParent.GetChild(i).gameObject);

        for (int y = 0; y < gm.height; y++)
            for (int x = 0; x < gm.width; x++)
            {
                var cell = new Vector3Int(gm.origin.x + x, gm.origin.y + y, 0);
                var world = gm.CellToWorldCenter(cell);

                var go = Instantiate(cellPrefab, world, Quaternion.identity, cellParent);
                go.name = $"Cell_{cell.x}_{cell.y}";

                var slot = go.GetComponent<BoardSlot>();
                if (slot == null) slot = go.AddComponent<BoardSlot>();
                slot.Init(cell);

                slots[cell] = slot;
            }
    }

    public BoardSlot GetSlot(Vector3Int cell)
        => slots.TryGetValue(cell, out var s) ? s : null;
}
