using UnityEngine;
using System.Collections.Generic;

public class BenchManager : MonoBehaviour
{
    public List<BenchSlot> benchSlots;
    public GameObject unitInstancePrefab; // UnitInstance가 붙은 프리팹

    public bool TryAddHero(UnitData data)
    {
        foreach (var slot in benchSlots)
        {
            if (!slot.HasHero)
            {
                GameObject go = Instantiate(unitInstancePrefab, slot.transform);
                Unit instance = go.GetComponent<Unit>();
                instance.Init(data);

                slot.PlaceHero(go); // GameObject 기준으로 넘기도록 수정
                return true;
            }
        }

        Debug.Log("벤치가 가득 찼습니다.");
        return false;
    }

    public void SwapHero(BenchSlot fromSlot, BenchSlot toSlot)
    {
        GameObject temp = toSlot.CurrentHero;
        toSlot.PlaceHero(fromSlot.CurrentHero);
        fromSlot.PlaceHero(temp);
    }
}
