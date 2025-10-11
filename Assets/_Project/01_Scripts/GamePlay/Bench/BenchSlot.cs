using UnityEngine;
using UnityEngine.EventSystems;

public class BenchSlot : MonoBehaviour, IDropHandler
{
    public GameObject CurrentHero { get; private set; }
    public bool HasHero => CurrentHero != null;

    public void PlaceHero(GameObject heroObj)
    {
        if (heroObj != null)
        {
            heroObj.transform.SetParent(transform);
            heroObj.transform.localPosition = Vector3.zero;
        }
        CurrentHero = heroObj;
    }

    public void Clear()
    {
        CurrentHero = null;
    }

    public void OnDrop(PointerEventData eventData)
    {
        Unit dropped = eventData.pointerDrag?.GetComponent<Unit>();
        if (dropped != null)
        {
            BenchManager manager = GetComponentInParent<BenchManager>();
            BenchSlot fromSlot = dropped.GetComponentInParent<BenchSlot>();
            manager.SwapHero(fromSlot, this);
        }
    }
}
