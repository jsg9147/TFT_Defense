using System.Collections.Generic;
using UnityEngine;

public class MonsterPathManager : MonoBehaviour
{
    public static MonsterPathManager Instance { get; private set; }

    [Header("경로를 이루는 웨이포인트들 (순서 중요)")]
    [SerializeField] private List<Transform> waypoints = new List<Transform>();

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogWarning("MonsterPathManager 중복 인스턴스 발견");
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public Transform GetWaypoint(int index)
    {
        if (index < 0 || index >= waypoints.Count) return null;
        return waypoints[index];
    }

    public int GetWaypointCount()
    {
        return waypoints.Count;
    }

    public Transform GetStartPoint()
    {
        return waypoints.Count > 0 ? waypoints[0] : null;
    }

    public Transform GetEndPoint()
    {
        return waypoints.Count > 0 ? waypoints[^1] : null;
    }

    public List<Transform> GetAllWaypoints()
    {
        return waypoints;
    }
}
