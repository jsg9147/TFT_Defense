using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterSpawner : MonoBehaviour
{
    [Header("웨이브 설정")]
    [SerializeField] private WaveSet waveSet;
    [SerializeField] private Transform[] spawnPoints; // pathId와 매칭 

    [Header("풀링")]
    [SerializeField] private Monster prefab;   // 기본 프리팹 (데모용)
    [SerializeField] private Transform poolParent;
    private MonsterPool pool;

    private IMonsterFieldService field;

    // 상태
    private Coroutine spawnCo;
    private readonly List<Monster> aliveMonsters = new();
    private int plannedThisWave;   // 이번 웨이브에 스폰 예정 총합
    private int spawnedThisWave;   // 실제 스폰된 수

    public int AliveCount => aliveMonsters.Count;
    public bool AllPlannedSpawned => spawnedThisWave >= plannedThisWave;

    private void Start()
    {
        pool = new MonsterPool(prefab, 32, poolParent); // 초기치 임의
        field = MonsterFieldManager.Instance;
    }

    public bool IsLastWave(int waveIndex)
        => waveSet != null && waveSet.waves != null && waveIndex >= waveSet.waves.Length - 1;

    /// <summary>웨이브 시작</summary>
    public void StartWave(int waveIndex)
    {
        StopSpawning();

        aliveMonsters.Clear();
        spawnedThisWave = 0;
        plannedThisWave = CountPlanned(waveIndex);

        spawnCo = StartCoroutine(CoSpawnWave(waveIndex));
        Debug.Log($"[Spawner] 웨이브 {waveIndex} 시작 | planned={plannedThisWave}");
    }

    /// <summary>웨이브 중단(추가 스폰만 중단, 이미 나온 몬스터는 게임매니저 규칙에 따름)</summary>
    public void StopSpawning()
    {
        if (spawnCo != null)
        {
            StopCoroutine(spawnCo);
            spawnCo = null;
        }
    }

    /// <summary>이번 웨이브에 계획된 몬스터 수 합계</summary>
    private int CountPlanned(int waveIndex)
    {
        if (waveSet == null || waveSet.waves == null) return 0;
        if (waveIndex < 0 || waveIndex >= waveSet.waves.Length) return 0;

        int sum = 0;
        foreach (var g in waveSet.waves[waveIndex].groups)
            sum += Mathf.Max(0, g.count);
        return sum;
    }

    private IEnumerator CoSpawnWave(int waveIndex)
    {
        if (waveSet == null || waveSet.waves == null) yield break;
        if (waveIndex < 0 || waveIndex >= waveSet.waves.Length) yield break;

        var wave = waveSet.waves[waveIndex];

        foreach (var g in wave.groups)
        {
            for (int i = 0; i < g.count; i++)
            {
                // 필드 한도 체크: 넘치면 잠깐 대기
                while (field != null && field.CurrentCount >= field.FieldLimit)
                    yield return null;

                SpawnOne(g.monster, g.pathId);
                spawnedThisWave++;

                if (g.spawnInterval > 0f)
                    yield return new WaitForSeconds(g.spawnInterval);
                else
                    yield return null; // 한 프레임 텀
            }
        }

        // 모든 계획 스폰 완료
        Debug.Log($"[Spawner] 웨이브 {waveIndex} 계획 스폰 완료 (spawned={spawnedThisWave}/{plannedThisWave})");
        spawnCo = null;
    }

    private void SpawnOne(MonsterData data, int pathId)
    {
        var m = pool.Get();
        m.transform.position = GetSpawnPoint(pathId).position;
        m.data = data;
        m.Init();

        // 등록 + 생존 리스트
        field?.Register(m);
        m.OnMonsterDie += OnMonsterDie;
        aliveMonsters.Add(m);
    }

    private void OnMonsterDie(Monster m)
    {
        m.OnMonsterDie -= OnMonsterDie;
        CurrencyManager.Instance.AddGold(m.data.goldReward);

        // 필드 해제
        field?.Unregister(m);

        // 생존 리스트에서 제거
        aliveMonsters.Remove(m);

        // 풀 반납
        pool.Return(m);
    }

    public Transform GetSpawnPoint(int pathId)
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return transform;
        int idx = Mathf.Clamp(pathId, 0, spawnPoints.Length - 1);
        return spawnPoints[idx];
    }
}
