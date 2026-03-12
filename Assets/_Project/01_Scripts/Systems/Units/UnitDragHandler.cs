using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class UnitDragHandler : MonoBehaviour
{
    public static UnitDragHandler Instance;

    private Unit draggingUnit;
    private Vector3 offset;

    void Awake() => Instance = this;

    /// <summary> 드래그 시작 (유닛 클릭 시) </summary>
    public void StartDragging(Unit unit)
    {
        if (unit == null) return;

        draggingUnit = unit;
        Vector3 mouseWorld = GetMouseWorldPos();
        offset = unit.transform.position - mouseWorld;
        draggingUnit.SetHighlight(true);
    }

    /// <summary> 드래그 중: 마우스를 따라 이동 (칸 스냅) </summary>
    public void UpdateDragging()
    {
        if (draggingUnit == null) return;

        Vector3 worldPos = GetMouseWorldPos();

        if (GridManager.Instance != null)
        {
            Vector3Int cell = GridManager.Instance.WorldToCell(worldPos);
            worldPos = GridManager.Instance.CellToWorldCenter(cell);
        }
        else
        {
            worldPos += offset;
        }

        worldPos.z = 0f;
        draggingUnit.transform.position = worldPos;
    }

    /// <summary>
    /// 드래그 종료 — 드롭 위치로 유닛을 이동하거나 (싱글),
    /// 서버에 이동을 요청하고 원위치로 되돌립니다 (멀티).
    /// GridManager.occupiedBy도 함께 업데이트합니다.
    /// </summary>
    public void StopDragging()
    {
        if (draggingUnit == null) return;

        draggingUnit.SetHighlight(false);

        var gm = GridManager.Instance;
        if (gm != null)
        {
            var toCell = gm.WorldToCell(draggingUnit.transform.position);

            if (!IsNetworkMode())
            {
                // 싱글: GridManager 직접 이동 (occupiedBy 업데이트 포함)
                if (gm.TryGetCellOfUnit(draggingUnit, out var fromCell))
                    gm.TryMoveUnit(fromCell, toCell);
            }
            else
            {
                // 멀티: 원위치로 되돌린 뒤 서버에 이동 요청
                // 서버 승인 시 OnCellChanged가 발화해 최종 위치 설정됨
                var netObj = draggingUnit.GetComponent<NetworkObject>();
                if (netObj != null)
                {
                    RestoreUnitToNetCell(draggingUnit);
                    var player = GetLocalNetworkPlayer();
                    player?.RequestMoveUnitServerRpc(netObj.NetworkObjectId, toCell.x, toCell.y);
                }
            }
        }

        draggingUnit = null;
    }

    public bool IsDragging() => draggingUnit != null;
    public Unit GetDraggingUnit() => draggingUnit;

    private Vector3 GetMouseWorldPos()
    {
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        worldPos.z = 0f;
        return worldPos;
    }

    private bool IsNetworkMode()
    {
        return NetworkGameManager.Instance != null && NetworkGameManager.Instance.IsNetworkMode();
    }

    private NetworkPlayer GetLocalNetworkPlayer()
    {
        if (NetworkManager.Singleton == null) return null;
        var playerObj = NetworkManager.Singleton.SpawnManager?.GetLocalPlayerObject();
        return playerObj != null ? playerObj.GetComponent<NetworkPlayer>() : null;
    }

    private void RestoreUnitToNetCell(Unit unit)
    {
        var gm = GridManager.Instance;
        if (gm == null) return;
        var cell = unit.NetCell;
        if (cell.x >= 0 && cell.y >= 0)
            unit.transform.position = gm.CellToWorldCenter(cell);
    }
}
