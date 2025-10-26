// UnitFactory.cs
using UnityEngine;

public static class UnitFactory
{
    /// <summary>
    /// UnitData.unitPrefab을 Instantiate하고 Unit.Init(data)까지 세팅.
    /// </summary>
    public static Unit Spawn(UnitData data, Vector3 worldPos, Transform parent = null)
    {
        if (data == null || data.unitPrefab == null)
        {
            Debug.LogError("[UnitFactory] UnitData 또는 unitPrefab이 비어있습니다.");
            return null;
        }

        var go = Object.Instantiate(data.unitPrefab, worldPos, Quaternion.identity, parent);
        var unit = go.GetComponent<Unit>();
        if (unit == null)
        {
            Debug.LogError($"[UnitFactory] 프리팹 {go.name}에 Unit 컴포넌트가 없습니다.");
            return null;
        }

        unit.Init(data); // ← 당신의 Unit.cs가 이미 이 경로를 지원
        return unit;
    }
}
