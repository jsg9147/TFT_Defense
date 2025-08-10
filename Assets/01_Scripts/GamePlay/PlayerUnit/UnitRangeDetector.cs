using System.Collections.Generic;
using UnityEngine;

public class UnitRangeDetector : MonoBehaviour
{
    private Unit unit;

    private void Awake()
    {
        unit = GetComponentInParent<Unit>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Monster"))
        {
            Monster monster = other.GetComponent<Monster>();
            if (monster != null)
            {
                unit.AddMonsterInRange(monster);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Monster"))
        {
            Monster monster = other.GetComponent<Monster>();
            if (monster != null)
            {
                unit.RemoveMonsterInRange(monster);
            }
        }
    }
}
