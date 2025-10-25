using UnityEngine;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;

    [Header("Unity Grid")]
    public Grid grid;

    [Header("Board Bounds")]
    public int width = 8;
    public int height = 5;
    public Vector3Int origin = Vector3Int.zero; // 좌상단 기준이면 y는 -로 내려감

    [Header("Rules")]
    public bool lockPlacementDuringBattle = true;

    // 점유: 어떤 유닛이 어느 칸을 쓰는지
    private readonly Dictionary<Vector3Int, Unit> occupiedBy = new();
    // 금지 칸(지형/막힌칸)
    private readonly HashSet<Vector3Int> blocked = new();

    void Awake()
    {
        Instance = this;
    }

    bool _placementLocked;

    // === 좌표 변환 ===
    public Vector3Int WorldToCell(Vector3 world) => grid.WorldToCell(world);
    public Vector3 CellToWorldCenter(Vector3Int cell)
    {
        var worldMin = grid.CellToWorld(cell);
        var cellSize = grid.cellSize; // Grid 컴포넌트 기준
        return new Vector3(worldMin.x + cellSize.x * 0.5f, worldMin.y + cellSize.y * 0.5f, 0f);
    }

    // === 규칙 ===
    public bool IsInBounds(Vector3Int cell)
    {
        return cell.x >= origin.x && cell.x < origin.x + width
            && cell.y >= origin.y && cell.y < origin.y + height;
    }

    public void RegisterBlocked(Vector3Int cell, bool value = true)
    {
        if (value) blocked.Add(cell);
        else blocked.Remove(cell);
    }

    public bool IsBlocked(Vector3Int cell) => blocked.Contains(cell);
    public bool IsOccupied(Vector3Int cell) => occupiedBy.ContainsKey(cell);

    public bool IsPlaceable(Vector3Int cell)
    {
        if (_placementLocked) return false;
        if (!IsInBounds(cell)) return false;
        if (IsBlocked(cell)) return false;
        if (IsOccupied(cell)) return false;
        return true;
    }

    // === 배치/이동/해제 ===
    public bool TryPlaceUnit(Unit unit, Vector3Int cell)
    {
        if (unit == null) return false;
        if (!IsPlaceable(cell)) return false;

        occupiedBy[cell] = unit;
        unit.transform.position = CellToWorldCenter(cell);
        return true;
    }

    public bool TryMoveUnit(Vector3Int from, Vector3Int to)
    {
        if (!occupiedBy.TryGetValue(from, out var unit)) return false;
        if (!IsPlaceable(to)) return false;

        occupiedBy.Remove(from);
        occupiedBy[to] = unit;
        unit.transform.position = CellToWorldCenter(to);
        return true;
    }

    public bool TryRemoveUnit(Vector3Int cell, out Unit unit)
    {
        if (occupiedBy.TryGetValue(cell, out unit))
        {
            occupiedBy.Remove(cell);
            return true;
        }
        unit = null;
        return false;
    }

    public Unit GetUnitAt(Vector3Int cell)
        => occupiedBy.TryGetValue(cell, out var u) ? u : null;

    // 벤치 → 보드 드롭용 헬퍼: 스크린/월드 포지션에서 즉시 배치 시도
    public bool TryPlaceAtWorld(Unit unit, Vector3 world, out Vector3Int cell)
    {
        cell = WorldToCell(world);
        return TryPlaceUnit(unit, cell);
    }
    public bool TryFindFirstPlaceable(out Vector3Int cell)
    {
        for (int y = origin.y; y < origin.y + height; y++)
            for (int x = origin.x; x < origin.x + width; x++)
            {
                var c = new Vector3Int(x, y, 0);
                if (IsPlaceable(c))
                {
                    cell = c;
                    return true;
                }
            }
        cell = default;
        return false;
    }

    public bool HasPlaceableCell() => TryFindFirstPlaceable(out _);

}
