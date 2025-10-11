using UnityEngine;
using System.Collections.Generic;

public class GridCellManager : MonoBehaviour
{
    public GameObject cellPrefab;
    public Transform cellParent;
    public int width = 8;
    public int height = 5;
    public float spacing = 1f;

    private Dictionary<Vector3Int, GameObject> cells = new();

    void Start() => GenerateGrid();

    void GenerateGrid()
    {
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                Vector3Int cellPos = new(x, y, 0);
                Vector3 worldPos = GridManager.Instance.CellToWorldCenter(cellPos);

                GameObject tile = Instantiate(cellPrefab, worldPos, Quaternion.identity, cellParent);
                tile.name = $"Cell_{x}_{y}";

                cells[cellPos] = tile;
            }
    }

    public GameObject GetCellObject(Vector3Int pos) =>
        cells.TryGetValue(pos, out var tile) ? tile : null;
}
