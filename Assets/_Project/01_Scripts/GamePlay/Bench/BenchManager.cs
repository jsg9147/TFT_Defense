using UnityEngine;
using System.Collections.Generic;

public class BenchManager : MonoBehaviour
{
    public List<BenchSlot> benchSlots;
    public GameObject unitInstancePrefab; // Unit ������Ʈ�� ���� ������

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

        Debug.Log("��ġ�� ���� á���ϴ�.");
        return false;
    }

    public void SwapHero(BenchSlot fromSlot, BenchSlot toSlot)
    {
        // ���� �� �ܼ� �̵��� ���/���� ���ʿ� (�̹� ��ϵ� ����)
        GameObject temp = toSlot.CurrentHero;
        toSlot.PlaceHero(fromSlot.CurrentHero);
        fromSlot.PlaceHero(temp);
    }

    // ����: ��ġ���� ������ �Ǹ�/������ �� ȣ��
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
