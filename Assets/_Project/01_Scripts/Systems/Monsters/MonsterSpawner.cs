using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterSpawner : MonoBehaviour
{
    [Header("웨이브 설정")]
    [SerializeField] private WaveSet waveSet;
    [SerializeField] private Transform[] spawnPoints; // pathId와 매칭 

    [Header("풀링")]
    [SerializeField] private Transform poolParent;
    private readonly Dictionary<MonsterData, MonsterPool> _pools = new();

    private IMonsterFieldService field;

    // 상태
    private Coroutine spawnCo;
    private readonly List<Monster> aliveMonsters = new();

    public int AliveCount => aliveMonsters.Count;

    private void Start()
    {
        field = MonsterFieldManager.Instance;
    }

    public bool IsLastWave(int waveIndex)
        => waveSet != null && waveSet.waves != null && waveIndex >= waveSet.waves.Length - 1;

    public bool IsLastGroup(int waveIndex, int groupIndex)
    {
        if (waveSet == null || waveSet.waves == null) return true;
        if (waveIndex < 0 || waveIndex >= waveSet.waves.Length) return true;
        
        return groupIndex >= waveSet.waves[waveIndex].groups.Length - 1;
    }
    
    public WaveGroup GetWaveGroup(int waveIndex, int groupIndex)
    {
        return waveSet.waves[waveIndex].groups[groupIndex];
    }
    
    public int GetGroupCount(int waveIndex)
    {
        if (waveSet == null || waveSet.waves == null || waveIndex < 0 || waveIndex >= waveSet.waves.Length)
        {
            return 0;
        }
        return waveSet.waves[waveIndex].groups.Length;
    }

    /// <summary>웨이브 시작 시 각종 상태 초기화</summary>
    public void PrepareWave()
    {
        StopSpawning();
        aliveMonsters.Clear();
    }

    /// <summary>특정 그룹 스폰 시작</summary>
    public void SpawnWaveGroup(int waveIndex, int groupIndex)
    {
        StopSpawning();
        spawnCo = StartCoroutine(CoSpawnWaveGroup(waveIndex, groupIndex));
        Debug.Log($"[Spawner] 웨이브 {waveIndex} - 그룹 {groupIndex} 스폰 시작");
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
    
    private IEnumerator CoSpawnWaveGroup(int waveIndex, int groupIndex)
    {
        if (waveSet == null || waveSet.waves == null) yield break;
        if (waveIndex < 0 || waveIndex >= waveSet.waves.Length) yield break;

        var wave = waveSet.waves[waveIndex];
        if (groupIndex < 0 || groupIndex >= wave.groups.Length) yield break;
        
        var g = wave.groups[groupIndex];

        for (int i = 0; i < g.count; i++)
        {
            // 필드 한도 체크: 넘치면 잠깐 대기
            while (field != null && field.CurrentCount >= field.FieldLimit)
                yield return null;

            SpawnOne(g.monster, g.pathId);

            if (g.spawnInterval > 0f)
                yield return new WaitForSeconds(g.spawnInterval);
            else
                yield return null; // 한 프레임 텀
        }
        
        Debug.Log($"[Spawner] 웨이브 {waveIndex} - 그룹 {groupIndex} 스폰 완료");
        spawnCo = null;
    }

    private void SpawnOne(MonsterData data, int pathId)
    {
        // 데이터에 맞는 풀이 없으면 새로 생성
        if (!_pools.ContainsKey(data))
        {
            if (data.prefab == null)
            {
                Debug.LogError($"[MonsterSpawner] MonsterData '{data.name}'에 프리팹이 할당되지 않았습니다!");
                return;
            }
            var newPool = new MonsterPool(data.prefab, 10, poolParent); // 초기 사이즈 10
            _pools.Add(data, newPool);
        }

        var m = _pools[data].Get();
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

        // 자신의 데이터에 맞는 풀에 반납
        if (m.data != null && _pools.TryGetValue(m.data, out var pool))
        {
            pool.Return(m);
        }
        else
        {
            // 풀을 찾지 못하면 그냥 파괴
            Debug.LogWarning($"[MonsterSpawner] 몬스터 {m.name}의 풀을 찾지 못해 파괴합니다.");
            Destroy(m.gameObject);
        }
    }

    public Transform GetSpawnPoint(int pathId)
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return transform;
        int idx = Mathf.Clamp(pathId, 0, spawnPoints.Length - 1);
        return spawnPoints[idx];
    }
}
