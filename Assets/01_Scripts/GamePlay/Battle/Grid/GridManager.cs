using UnityEngine;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;
    public Grid grid;
    private HashSet<Vector3Int> occupiedCells = new HashSet<Vector3Int>();

    void Awake() => Instance = this;

    public Vector3Int WorldToCell(Vector3 pos) => grid.WorldToCell(pos);
    public Vector3 CellToWorldCenter(Vector3Int cellPos) => grid.GetCellCenterWorld(cellPos);

    public bool IsPlaceable(Vector3Int cellPos) => !occupiedCells.Contains(cellPos);
    public void Occupy(Vector3Int cellPos) => occupiedCells.Add(cellPos);

    public bool TryPlace(Vector3 pos, out Vector3Int cellPos)
    {
        cellPos = grid.WorldToCell(pos);
        if (IsPlaceable(cellPos))
        {
            Occupy(cellPos);
            return true;
        }
        return false;
    }

}
