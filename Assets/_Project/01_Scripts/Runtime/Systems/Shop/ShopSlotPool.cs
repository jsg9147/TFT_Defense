using System.Collections.Generic;
using UnityEngine;

public class ShopSlotPool : MonoBehaviour
{
    [Header("풀링 설정")]
    [SerializeField] private ShopSlotUI slotPrefab;
    [SerializeField] private int initialCount = 10; // 초기 생성 수

    private readonly Queue<ShopSlotUI> pool = new Queue<ShopSlotUI>();

    protected void Awake()
    {
        // 초기 슬롯 생성
        for (int i = 0; i < initialCount; i++)
        {
            CreateSlot();
        }
    }

    /// <summary> 슬롯 하나 생성해서 풀에 보관 </summary>
    private void CreateSlot()
    {
        var slot = Instantiate(slotPrefab, transform);
        slot.gameObject.SetActive(false);
        pool.Enqueue(slot);
    }

    /// <summary> 풀에서 슬롯 하나 가져오기 </summary>
    public ShopSlotUI GetSlot(Transform parent)
    {
        if (pool.Count == 0)
            CreateSlot();

        var slot = pool.Dequeue();
        slot.transform.SetParent(parent, false);
        slot.gameObject.SetActive(true);
        return slot;
    }

    /// <summary> 슬롯을 풀에 반환 </summary>
    public void ReturnSlot(ShopSlotUI slot)
    {
        slot.gameObject.SetActive(false);
        slot.transform.SetParent(transform, false); // 풀의 자식으로
        pool.Enqueue(slot);
    }
}
