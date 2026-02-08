using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어별 몬스터 이동 경로(웨이포인트) 관리.
/// playerIndex(0/1)로 각 플레이어의 경로를 구분한다.
/// </summary>
public class MonsterPathManager : MonoBehaviour
{
    public static MonsterPathManager Instance { get; private set; }

    [Serializable]
    public class PathData
    {
        public List<Transform> waypoints = new();
    }

    [Header("플레이어별 경로 (index 0 = Player 0, index 1 = Player 1)")]
    [SerializeField] private PathData[] playerPaths = new PathData[2];

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogWarning("[MonsterPathManager] 중복 인스턴스 감지");
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public Transform GetWaypoint(int playerIndex, int waypointIndex)
    {
        var wps = GetWaypoints(playerIndex);
        if (waypointIndex < 0 || waypointIndex >= wps.Count) return null;
        return wps[waypointIndex];
    }

    public int GetWaypointCount(int playerIndex)
    {
        return GetWaypoints(playerIndex).Count;
    }

    public Transform GetStartPoint(int playerIndex)
    {
        var wps = GetWaypoints(playerIndex);
        return wps.Count > 0 ? wps[0] : null;
    }

    public Transform GetEndPoint(int playerIndex)
    {
        var wps = GetWaypoints(playerIndex);
        return wps.Count > 0 ? wps[^1] : null;
    }

    public List<Transform> GetAllWaypoints(int playerIndex)
    {
        return GetWaypoints(playerIndex);
    }

    private List<Transform> GetWaypoints(int playerIndex)
    {
        if (playerPaths == null || playerPaths.Length == 0)
            return new List<Transform>();

        int idx = Mathf.Clamp(playerIndex, 0, playerPaths.Length - 1);
        return playerPaths[idx]?.waypoints ?? new List<Transform>();
    }
}
