using System.Collections.Generic;
using UnityEngine;

public class MonsterPool
{
    private Monster prefab;
    private Transform poolParent;
    private Queue<Monster> pool = new();

    public MonsterPool(Monster prefab, int initialSize, Transform parent = null)
    {
        this.prefab = prefab;
        this.poolParent = parent;

        for (int i = 0; i < initialSize; i++)
        {
            Monster monster = Object.Instantiate(prefab, parent);
            monster.gameObject.SetActive(false);
            pool.Enqueue(monster);
        }
    }

    public Monster Get()
    {
        if (pool.Count > 0)
        {
            Monster m = pool.Dequeue();
            m.gameObject.SetActive(true);
            return m;
        }

        // 풀에 없으면 새로 생성 (필요 시 제한 가능)
        Monster newM = Object.Instantiate(prefab, poolParent);
        return newM;
    }

    public void Return(Monster monster)
    {
        monster.gameObject.SetActive(false);
        pool.Enqueue(monster);
    }
}
