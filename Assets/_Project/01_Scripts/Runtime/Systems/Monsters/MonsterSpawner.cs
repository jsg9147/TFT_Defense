using System.Collections.Generic;
using UnityEngine;

public class MonsterSpawner : MonoBehaviour
{
    [Header("몬스터 설정")]
    public Monster prefab;
    public MonsterData[] spawnList;    // 웨이브에 등장할 몬스터 목록
    public Transform spawnPoint;
    public Transform poolParent;

    [Header("스폰 제어")]
    public float spawnInterval = 2f;
    public int maxSpawnCount = 10; // 한 웨이브 최대 스폰 수

    private int currentSpawnCount = 0;
    private float timer = 0f;
    private bool spawning = false;
    private int spawnIndex = 0;

    private List<Monster> aliveMonsters = new();
    private MonsterPool pool;
    private IMonsterFieldService field;

    private void Start()
    {
        pool = new MonsterPool(prefab, maxSpawnCount, poolParent);
        field = MonsterFieldManager.Instance;
    }

    private void Update()
    {
        if (!spawning) return;

        // 최대 수량 모두 소환하면 대기
        if (currentSpawnCount >= maxSpawnCount) return;

        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            timer = 0f;
            SpawnMonster(spawnIndex, spawnPoint.position);
            spawnIndex = (spawnIndex + 1) % spawnList.Length;
        }
    }

    public void SpawnMonster(int index, Vector3 spawnPos)
    {
        if (index < 0 || index >= spawnList.Length) return;

        // 한도 체크: 넘치면 스폰 안 함
        if (field != null && field.CurrentCount >= field.FieldLimit) return;

        Monster monster = pool.Get();
        monster.transform.position = spawnPos;
        monster.data = spawnList[index];
        monster.Init();                               // 체력/웨이포인트 리셋 권장 
        monster.OnMonsterDie += HandleMonsterDeath;

        // 필드 카운트 등록
        field?.Register(monster);                     // 카운트 +1, 이벤트 발행 

        aliveMonsters.Add(monster);
        currentSpawnCount++;
    }

    /// <summary>몬스터 사망 처리</summary>
    private void HandleMonsterDeath(Monster deadMonster)
    {
        // 필드 카운트 해제
        MonsterFieldManager.Instance?.Unregister(deadMonster);  // 카운트 -1 

        if (aliveMonsters.Contains(deadMonster))
            aliveMonsters.Remove(deadMonster);

        deadMonster.OnMonsterDie -= HandleMonsterDeath;
        pool.Return(deadMonster);

        if (IsWaveFinished())
        {
            Debug.Log("웨이브 종료: 모든 몬스터 처치 완료");
            GameManager.Instance.SetGameState(GameManager.GameState.Shop);
        }
    }


    /// <summary>웨이브 시작</summary>
    public void StartWave(int waveCount)
    {
        spawning = true;
        timer = 0f;
        spawnIndex = 0;
        currentSpawnCount = 0;
        aliveMonsters.Clear();

        Debug.Log($"웨이브 {waveCount} 시작");
    }

    /// <summary>웨이브 중단</summary>
    public void StopSpawning()
    {
        spawning = false;
    }

    /// <summary>현재 웨이브 종료 여부</summary>
    public bool IsWaveFinished()
    {
        // 필요시 스폰만 끝났는지 확인용으로 계속 둘 수는 있음
        return currentSpawnCount >= maxSpawnCount;
    }

    public int GetAliveCount() => aliveMonsters.Count;
}
