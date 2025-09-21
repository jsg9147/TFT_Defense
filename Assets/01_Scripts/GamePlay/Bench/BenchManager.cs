using UnityEngine;
using System.Collections.Generic;

public class BenchManager : MonoBehaviour
{
    public List<BenchSlot> benchSlots;
    public GameObject unitInstancePrefab; // Unit 컴포넌트가 붙은 프리팹

    public bool TryAddHero(UnitData data)
    {
        foreach (var slot in benchSlots)
        {
            if (!slot.HasHero)
            {
                GameObject go = Instantiate(unitInstancePrefab, slot.transform);
                Unit unit = go.GetComponent<Unit>();
                unit.Init(data);

                slot.PlaceHero(go);
                return true;
            }
        }

        Debug.Log("벤치가 가득 찼습니다.");
        return false;
    }

    public void SwapHero(BenchSlot fromSlot, BenchSlot toSlot)
    {
        // 슬롯 간 단순 이동은 등록/해제 불필요 (이미 등록된 유닛)
        GameObject temp = toSlot.CurrentHero;
        toSlot.PlaceHero(fromSlot.CurrentHero);
        fromSlot.PlaceHero(temp);
    }

    // 선택: 벤치에서 유닛을 판매/삭제할 때 호출
    public void RemoveFromBench(BenchSlot slot, bool destroy = true)
    {
        var hero = slot.CurrentHero;
        if (!hero) return;

        var unit = hero.GetComponent<Unit>();
        if (unit) SynergyManager.Instance.UnregisterUnit(unit);

        slot.Clear();

        if (destroy) Destroy(hero);
    }
}
