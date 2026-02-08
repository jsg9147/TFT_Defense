using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 웨이브 기반 몬스터 스폰 관리 (서버 전용).
/// 플레이어별 독립 코루틴으로 동시 스폰을 지원한다.
/// NetworkObject.Spawn()을 사용하여 모든 클라이언트에 몬스터를 동기화한다.
/// </summary>
public class MonsterSpawner : MonoBehaviour
{
    private const int MaxPlayers = 2;

    [Header("웨이브 설정")]
    [SerializeField] private WaveSet waveSet;

    [Header("데이터 레지스트리")]
    [SerializeField] private MonsterDataRegistry dataRegistry;

    [Serializable]
    public class SpawnPointSet
    {
        public Transform[] spawnPoints;
    }

    [Header("플레이어별 스폰 포인트 (index 0 = Player 0, index 1 = Player 1)")]
    [SerializeField] private SpawnPointSet[] playerSpawnSets = new SpawnPointSet[MaxPlayers];

    [Header("풀링")]
    [SerializeField] private Transform poolParent;
    private readonly Dictionary<MonsterData, MonsterPool> _pools = new();

    private IMonsterFieldService field;

    // 플레이어별 독립 코루틴
    private readonly Coroutine[] spawnCoroutines = new Coroutine[MaxPlayers];
    private readonly List<Monster> aliveMonsters = new();

    public int AliveCount => aliveMonsters.Count;

    private void Start()
    {
        field = MonsterFieldManager.Instance;
        dataRegistry?.Initialize();
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
            return 0;
        return waveSet.waves[waveIndex].groups.Length;
    }

    /// <summary>웨이브 시작 시 각종 상태 초기화</summary>
    public void PrepareWave()
    {
        StopSpawning();
        aliveMonsters.Clear();
    }

    /// <summary>특정 그룹을 지정 플레이어 보드에 스폰 시작 (서버 전용)</summary>
    public void SpawnWaveGroup(int waveIndex, int groupIndex, int playerIndex)
    {
        int pIdx = Mathf.Clamp(playerIndex, 0, MaxPlayers - 1);
        StopSpawning(pIdx);
        spawnCoroutines[pIdx] = StartCoroutine(CoSpawnWaveGroup(waveIndex, groupIndex, pIdx));
        Debug.Log($"[Spawner] 웨이브 {waveIndex} - 그룹 {groupIndex} - Player {pIdx} 스폰 시작");
    }

    /// <summary>모든 플레이어의 스폰 중단</summary>
    public void StopSpawning()
    {
        for (int i = 0; i < MaxPlayers; i++)
            StopSpawning(i);
    }

    /// <summary>특정 플레이어의 스폰 중단</summary>
    public void StopSpawning(int playerIndex)
    {
        if (spawnCoroutines[playerIndex] != null)
        {
            StopCoroutine(spawnCoroutines[playerIndex]);
            spawnCoroutines[playerIndex] = null;
        }
    }

    private IEnumerator CoSpawnWaveGroup(int waveIndex, int groupIndex, int playerIndex)
    {
        if (waveSet == null || waveSet.waves == null) yield break;
        if (waveIndex < 0 || waveIndex >= waveSet.waves.Length) yield break;

        var wave = waveSet.waves[waveIndex];
        if (groupIndex < 0 || groupIndex >= wave.groups.Length) yield break;

        var g = wave.groups[groupIndex];

        for (int i = 0; i < g.count; i++)
        {
            while (field != null && field.GetCurrentCount(playerIndex) >= field.GetFieldLimit(playerIndex))
                yield return null;

            SpawnOne(g.monster, g.pathId, playerIndex);

            if (g.spawnInterval > 0f)
                yield return new WaitForSeconds(g.spawnInterval);
            else
                yield return null;
        }

        Debug.Log($"[Spawner] 웨이브 {waveIndex} - 그룹 {groupIndex} - Player {playerIndex} 스폰 완료");
        spawnCoroutines[playerIndex] = null;
    }

    private void SpawnOne(MonsterData data, int pathId, int playerIndex)
    {
        if (!_pools.ContainsKey(data))
        {
            if (data.prefab == null)
            {
                Debug.LogError($"[MonsterSpawner] MonsterData '{data.name}'에 프리팹이 할당되지 않았습니다!");
                return;
            }
            var newPool = new MonsterPool(data.prefab, 10, poolParent);
            _pools.Add(data, newPool);
        }

        var m = _pools[data].Get();
        m.transform.position = GetSpawnPoint(pathId, playerIndex).position;

        // 네트워크 스폰
        var netObj = m.GetComponent<NetworkObject>();
        if (netObj != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            netObj.Spawn();
            int dataIndex = dataRegistry != null ? dataRegistry.GetIndex(data) : -1;
            m.InitServer(data, dataIndex, playerIndex);
        }

        field?.Register(m, playerIndex);
        m.OnMonsterDie += OnMonsterDie;
        aliveMonsters.Add(m);
    }

    private void OnMonsterDie(Monster m)
    {
        m.OnMonsterDie -= OnMonsterDie;
        CurrencyManager.Instance.AddGold(m.data.goldReward);

        field?.Unregister(m, m.OwnerPlayerIndex);
        aliveMonsters.Remove(m);

        // 네트워크 디스폰 (풀 재사용을 위해 오브젝트는 유지)
        var netObj = m.GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsSpawned)
            netObj.Despawn(false);

        if (m.data != null && _pools.TryGetValue(m.data, out var pool))
        {
            pool.Return(m);
        }
        else
        {
            Destroy(m.gameObject);
        }
    }

    /// <summary>플레이어별 스폰 포인트 조회</summary>
    public Transform GetSpawnPoint(int pathId, int playerIndex)
    {
        if (playerSpawnSets == null || playerSpawnSets.Length == 0) return transform;

        int pIdx = Mathf.Clamp(playerIndex, 0, playerSpawnSets.Length - 1);
        var set = playerSpawnSets[pIdx];
        if (set == null || set.spawnPoints == null || set.spawnPoints.Length == 0) return transform;

        int sIdx = Mathf.Clamp(pathId, 0, set.spawnPoints.Length - 1);
        return set.spawnPoints[sIdx];
    }
}
