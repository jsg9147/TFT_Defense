using TMPro;
using Unity.Netcode;
using UnityEngine;
using TFT_Defense.Managers;

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
            ShowDamageClientRpc(finalDamage, transform.position);

        netHP.Value -= finalDamage;

        if (netHP.Value <= 0)
            Die();
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
        OnMonsterDie?.Invoke(this);
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
