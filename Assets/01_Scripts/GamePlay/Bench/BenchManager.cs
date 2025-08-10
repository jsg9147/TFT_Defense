using UnityEngine;
using System.Collections.Generic;

public class BenchManager : MonoBehaviour
{
    public List<BenchSlot> benchSlots;
    public GameObject unitInstancePrefab; // UnitInstance�� ���� ������

    public bool TryAddHero(UnitData data)
    {
        foreach (var slot in benchSlots)
        {
            if (!slot.HasHero)
            {
                GameObject go = Instantiate(unitInstancePrefab, slot.transform);
                Unit instance = go.GetComponent<Unit>();
                instance.Init(data);

                slot.PlaceHero(go); // GameObject �������� �ѱ⵵�� ����
                return true;
            }
        }

        Debug.Log("��ġ�� ���� á���ϴ�.");
        return false;
    }

    public void SwapHero(BenchSlot fromSlot, BenchSlot toSlot)
    {
        GameObject temp = toSlot.CurrentHero;
        toSlot.PlaceHero(fromSlot.CurrentHero);
        fromSlot.PlaceHero(temp);
    }
}
