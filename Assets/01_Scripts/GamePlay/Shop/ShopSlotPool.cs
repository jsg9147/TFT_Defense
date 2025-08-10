using System.Collections.Generic;
using UnityEngine;

public class ShopSlotPool : MonoBehaviour
{
    [Header("Ǯ�� ����")]
    [SerializeField] private ShopSlotUI slotPrefab;
    [SerializeField] private int initialCount = 10; // �ʱ� ���� ��

    private readonly Queue<ShopSlotUI> pool = new Queue<ShopSlotUI>();

    protected void Awake()
    {
        // �ʱ� ���� ����
        for (int i = 0; i < initialCount; i++)
        {
            CreateSlot();
        }
    }

    /// <summary> ���� �ϳ� �����ؼ� Ǯ�� ���� </summary>
    private void CreateSlot()
    {
        var slot = Instantiate(slotPrefab, transform);
        slot.gameObject.SetActive(false);
        pool.Enqueue(slot);
    }

    /// <summary> Ǯ���� ���� �ϳ� �������� </summary>
    public ShopSlotUI GetSlot(Transform parent)
    {
        if (pool.Count == 0)
            CreateSlot();

        var slot = pool.Dequeue();
        slot.transform.SetParent(parent, false);
        slot.gameObject.SetActive(true);
        return slot;
    }

    /// <summary> ������ Ǯ�� ��ȯ </summary>
    public void ReturnSlot(ShopSlotUI slot)
    {
        slot.gameObject.SetActive(false);
        slot.transform.SetParent(transform, false); // Ǯ�� �ڽ�����
        pool.Enqueue(slot);
    }
}
