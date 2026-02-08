using UnityEngine;

/// <summary>
/// 모든 MonsterData를 인덱스로 조회할 수 있는 레지스트리.
/// ScriptableObject이므로 네트워크로 직접 전송이 불가능한 MonsterData를
/// int 인덱스로 변환하여 NetworkVariable에 저장할 수 있게 한다.
/// </summary>
[CreateAssetMenu(menuName = "Monster/MonsterDataRegistry")]
public class MonsterDataRegistry : ScriptableObject
{
    public static MonsterDataRegistry Instance { get; private set; }

    [SerializeField] private MonsterData[] allMonsters;

    /// <summary>MonsterSpawner.Start()에서 호출하여 정적 참조를 설정</summary>
    public void Initialize()
    {
        Instance = this;
    }

    public int GetIndex(MonsterData data)
    {
        if (allMonsters == null) return -1;
        for (int i = 0; i < allMonsters.Length; i++)
            if (allMonsters[i] == data) return i;
        Debug.LogError($"[MonsterDataRegistry] MonsterData '{data.name}'이 레지스트리에 없습니다!");
        return -1;
    }

    public MonsterData GetData(int index)
    {
        if (allMonsters == null || index < 0 || index >= allMonsters.Length) return null;
        return allMonsters[index];
    }

    public int Count => allMonsters != null ? allMonsters.Length : 0;
}
