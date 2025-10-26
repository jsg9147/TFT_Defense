// UnitFactory.cs
using UnityEngine;

public static class UnitFactory
{
    /// <summary>
    /// UnitData.unitPrefab�� Instantiate�ϰ� Unit.Init(data)���� ����.
    /// </summary>
    public static Unit Spawn(UnitData data, Vector3 worldPos, Transform parent = null)
    {
        if (data == null || data.unitPrefab == null)
        {
            Debug.LogError("[UnitFactory] UnitData �Ǵ� unitPrefab�� ����ֽ��ϴ�.");
            return null;
        }

        var go = Object.Instantiate(data.unitPrefab, worldPos, Quaternion.identity, parent);
        var unit = go.GetComponent<Unit>();
        if (unit == null)
        {
            Debug.LogError($"[UnitFactory] ������ {go.name}�� Unit ������Ʈ�� �����ϴ�.");
            return null;
        }

        unit.Init(data); // �� ����� Unit.cs�� �̹� �� ��θ� ����
        return unit;
    }
}
