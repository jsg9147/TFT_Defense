using UnityEngine;
using System.Collections.Generic;

public class AuraService : MonoSingleton<AuraService>
{
    [Header("¼³Á¤(SO)")]
    public TierVisualConfig config;

    private readonly Dictionary<Unit, GameObject> map = new();

    public void Apply(Unit unit)
    {
        if (!unit) return;
        Clear(unit);

        if (config == null)
        {
            Debug.LogWarning("[AuraService] TierVisualConfig is null.");
            return;
        }

        GameObject prefab = null;
        if (unit.starLevel >= 4) prefab = config.auraStar4Prefab;
        else if (unit.starLevel == 3) prefab = config.auraStar3Prefab;

        if (!prefab) return;

        var go = Instantiate(prefab, unit.transform);
        go.transform.localPosition = config.localOffset;
        if (!config.inheritUnitScale) go.transform.localScale = Vector3.one;

        map[unit] = go;
    }

    public void Clear(Unit unit)
    {
        if (unit == null) return;
        if (map.TryGetValue(unit, out var go) && go) Destroy(go);
        map.Remove(unit);
    }

    public void ClearAll()
    {
        foreach (var kv in map)
            if (kv.Value) Destroy(kv.Value);
        map.Clear();
    }
}
