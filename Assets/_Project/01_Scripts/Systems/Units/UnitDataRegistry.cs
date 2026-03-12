using UnityEngine;

/// <summary>
/// 모든 UnitData를 인덱스로 조회할 수 있는 레지스트리.
/// ScriptableObject이므로 네트워크로 직접 전송이 불가능한 UnitData를
/// int 인덱스로 변환하여 NetworkVariable에 저장할 수 있게 한다.
/// </summary>
[CreateAssetMenu(menuName = "Unit/UnitDataRegistry")]
public class UnitDataRegistry : ScriptableObject
{
    public static UnitDataRegistry Instance { get; private set; }

    [SerializeField] private UnitData[] allUnits;

    /// <summary>SummonManager.Start()에서 호출하여 정적 참조를 설정</summary>
    public void Initialize()
    {
        Instance = this;
    }

    public int GetIndex(UnitData data)
    {
        if (allUnits == null) return -1;
        for (int i = 0; i < allUnits.Length; i++)
            if (allUnits[i] == data) return i;
        Debug.LogError($"[UnitDataRegistry] UnitData '{data.name}'이 레지스트리에 없습니다!");
        return -1;
    }

    public UnitData GetData(int index)
    {
        if (allUnits == null || index < 0 || index >= allUnits.Length) return null;
        return allUnits[index];
    }

    public int Count => allUnits != null ? allUnits.Length : 0;
}
