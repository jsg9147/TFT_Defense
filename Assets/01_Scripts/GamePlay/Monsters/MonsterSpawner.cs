using System.Collections.Generic;
using UnityEngine;

public class MonsterSpawner : MonoBehaviour
{
    [Header("���� ����")]
    public Monster prefab;
    public MonsterData[] spawnList;    // ���̺꿡 ������ ���� ���
    public Transform spawnPoint;
    public Transform poolParent;

    [Header("���� ����")]
    public float spawnInterval = 2f;
    public int maxSpawnCount = 10; // �� ���̺� �ִ� ���� ��

    private int currentSpawnCount = 0;
    private float timer = 0f;
    private bool spawning = false;
    private int spawnIndex = 0;

    private List<Monster> aliveMonsters = new();
    private MonsterPool pool;

    private void Start()
    {
        pool = new MonsterPool(prefab, maxSpawnCount, poolParent);
    }

    private void Update()
    {
        if (!spawning) return;

        // �ִ� ���� ��� ��ȯ�ϸ� ���
        if (currentSpawnCount >= maxSpawnCount) return;

        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            timer = 0f;
            SpawnMonster(spawnIndex, spawnPoint.position);
            spawnIndex = (spawnIndex + 1) % spawnList.Length;
        }
    }

    /// <summary>���� ����</summary>
    public void SpawnMonster(int index, Vector3 spawnPos)
    {
        if (index < 0 || index >= spawnList.Length) return;

        Monster monster = pool.Get();
        monster.transform.position = spawnPos;
        monster.data = spawnList[index];
        monster.Init();

        // �ʵ� ���� ���
        MonsterFieldManager.Instance.Register(monster);

        monster.OnMonsterDie += HandleMonsterDeath;
        aliveMonsters.Add(monster);
        currentSpawnCount++;
    }


    /// <summary>���� ��� ó��</summary>
    private void HandleMonsterDeath(Monster deadMonster)
    {
        if (aliveMonsters.Contains(deadMonster))
            aliveMonsters.Remove(deadMonster);

        deadMonster.OnMonsterDie -= HandleMonsterDeath;

        MonsterFieldManager.Instance.Unregister(deadMonster);

        pool.Return(deadMonster);
    }

    /// <summary>���̺� ����</summary>
    public void StartWave(int waveCount)
    {
        spawning = true;
        timer = 0f;
        spawnIndex = 0;
        currentSpawnCount = 0;
        aliveMonsters.Clear();

        Debug.Log($"���̺� {waveCount} ����");
    }

    /// <summary>���̺� �ߴ�</summary>
    public void StopSpawning()
    {
        spawning = false;
    }

    /// <summary>���� ���̺� ���� ����</summary>
    public bool IsWaveFinished()
    {
        // �ʿ�� ������ �������� Ȯ�ο����� ��� �� ���� ����
        return currentSpawnCount >= maxSpawnCount;
    }

    public int GetAliveCount() => aliveMonsters.Count;
}
