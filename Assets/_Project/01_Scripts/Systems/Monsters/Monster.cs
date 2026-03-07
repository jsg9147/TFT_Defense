using System.Collections.Generic;
using System.Text;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using TFT_Defense.Managers;
using UI.KillLog;

/// <summary>
/// 네트워크 동기화되는 몬스터.
/// 서버에서만 이동/피격 로직을 실행하고, HP/위치를 클라이언트에 동기화한다.
/// NetworkTransform은 프리팹에 추가해야 위치 자동 동기화가 작동한다.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class Monster : NetworkBehaviour, IDamageable
{
    public delegate void MonsterDieHandler(Monster monster);
    public event MonsterDieHandler OnMonsterDie;

    [Header("Visual")]
    public SpriteRenderer unitSprite;
    public TextMeshPro hpText;

    // === Network Synced Variables ===
    private NetworkVariable<int> netHP = new(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<int> netDataIndex = new(
        -1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<int> netOwnerPlayerIndex = new(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // === Local State (서버에서만 사용) ===
    [HideInInspector] public MonsterData data;
    private Transform target;
    private int currentWaypointIndex;
    private bool _unregistered;

    /// <summary>이 몬스터에 데미지를 입힌 유닛별 누적 데미지 (서버 전용)</summary>
    private readonly Dictionary<string, int> _damageLog = new();

    public int OwnerPlayerIndex => netOwnerPlayerIndex.Value;
    public bool IsAlive => netHP.Value > 0;
    public Transform Transform => transform;

    // === Network Lifecycle ===

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        netHP.OnValueChanged += OnHPChanged;
        netDataIndex.OnValueChanged += OnDataIndexChanged;

        // Client: NetworkVariable에서 MonsterData 복원
        if (!IsServer && netDataIndex.Value >= 0)
            ResolveMonsterData();

        UpdateHpUI();
    }

    public override void OnNetworkDespawn()
    {
        netHP.OnValueChanged -= OnHPChanged;
        netDataIndex.OnValueChanged -= OnDataIndexChanged;
        base.OnNetworkDespawn();
    }

    /// <summary>서버 전용: 스폰 직후 몬스터 데이터 초기화</summary>
    public void InitServer(MonsterData monsterData, int dataIndex, int playerIndex)
    {
        if (!IsServer) return;

        data = monsterData;
        netDataIndex.Value = dataIndex;
        netOwnerPlayerIndex.Value = playerIndex;
        netHP.Value = monsterData.maxHP;

        currentWaypointIndex = 0;
        target = MonsterPathManager.Instance.GetWaypoint(playerIndex, 0);
        _unregistered = false;
        _damageLog.Clear();
    }

    // === Server-Authoritative Movement ===

    private void Update()
    {
        if (!IsServer || !IsSpawned) return;
        MoveTowardsTarget();
    }

    private void MoveTowardsTarget()
    {
        if (target == null) return;

        Vector3 dir = (target.position - transform.position).normalized;
        transform.position += dir * data.moveSpeed * Time.deltaTime;

        if (Vector3.Distance(transform.position, target.position) < 0.1f)
            GetNextTarget();
    }

    private void GetNextTarget()
    {
        currentWaypointIndex++;
        int playerIdx = netOwnerPlayerIndex.Value;

        if (currentWaypointIndex >= MonsterPathManager.Instance.GetWaypointCount(playerIdx))
            currentWaypointIndex = 0;

        target = MonsterPathManager.Instance.GetWaypoint(playerIdx, currentWaypointIndex);
    }

    // === Damage (Server-Authoritative) ===

    public void TakeDamage(in DamagePayload payload)
    {
        if (!IsServer) return;

        int finalDamage = DamageFormula.ComputeFinal(
            payload, data.defense, data.magicResistance);

        if (finalDamage > 0)
        {
            ShowDamageClientRpc(finalDamage, transform.position);
            RecordDamage(payload.Source as Unit, finalDamage);
        }

        netHP.Value -= finalDamage;

        if (netHP.Value <= 0)
            Die();
    }

    /// <summary>데미지를 입힌 유닛의 기여도를 누적한다. 동일 유닛의 여러 공격은 합산된다.</summary>
    private void RecordDamage(Unit attacker, int damage)
    {
        if (attacker == null || attacker.data == null) return;
        string key = attacker.data.unitName;
        _damageLog.TryGetValue(key, out int prev);
        _damageLog[key] = prev + damage;
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ShowDamageClientRpc(int damage, Vector3 position)
    {
        if (DamageTextManager.Instance != null)
            DamageTextManager.Instance.ShowDamage(damage, position);
    }

    // === UI Updates (모든 클라이언트) ===

    private void OnHPChanged(int oldValue, int newValue)
    {
        UpdateHpUI();
    }

    private void OnDataIndexChanged(int oldValue, int newValue)
    {
        if (!IsServer)
            ResolveMonsterData();
    }

    private void UpdateHpUI()
    {
        if (hpText) hpText.text = netHP.Value.ToString();
    }

    // === Data Resolution (클라이언트) ===

    private void ResolveMonsterData()
    {
        var registry = MonsterDataRegistry.Instance;
        if (registry != null && netDataIndex.Value >= 0)
            data = registry.GetData(netDataIndex.Value);
    }

    // === Death ===

    private void Die()
    {
        if (_unregistered) return;
        _unregistered = true;
        NotifyKillLog();
        OnMonsterDie?.Invoke(this);
    }

    /// <summary>몬스터 처치 시 데미지 기여도 로그를 UI에 전달한다.</summary>
    private void NotifyKillLog()
    {
        if (_damageLog.Count == 0) return;

        string monsterName = data?.monsterName ?? "Monster";
        string logPayload = SerializeKillLog(monsterName, _damageLog);

        if (!NetworkGameManager.Instance.IsNetworkMode())
            KillLogPanel.Instance?.ShowLog(logPayload);
        else
            BroadcastKillLogClientRpc(logPayload);
    }

    /// <summary>
    /// 데미지 로그를 단일 문자열로 직렬화한다.
    /// ASCII 제어 문자를 구분자로 사용해 유닛/몬스터 이름과 충돌을 방지한다.
    /// 포맷: "{monsterName}\u0001{unitA}\u0003{dmg}\u0002{unitB}\u0003{dmg}..."
    /// </summary>
    private static string SerializeKillLog(string monsterName, Dictionary<string, int> log)
    {
        var sb = new StringBuilder();
        sb.Append(monsterName).Append('\u0001'); // SOH: 몬스터명 구분자
        bool first = true;
        foreach (var kv in log)
        {
            if (!first) sb.Append('\u0002'); // STX: 항목 구분자
            sb.Append(kv.Key).Append('\u0003').Append(kv.Value); // ETX: key-value 구분자
            first = false;
        }
        return sb.ToString();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void BroadcastKillLogClientRpc(string logPayload)
    {
        KillLogPanel.Instance?.ShowLog(logPayload);
    }

    private void OnDisable()
    {
        if (!_unregistered && Application.isPlaying)
        {
            var svc = FindAnyObjectByType<MonsterFieldManager>();
            if (svc != null) svc.Unregister(this, netOwnerPlayerIndex.Value);
            _unregistered = true;
        }
    }
}
