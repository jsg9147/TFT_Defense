using UnityEngine;

[CreateAssetMenu(menuName = "Monster/MonsterData")]
public class MonsterData : ScriptableObject
{
    [Header("기본 정보")]
    public string monsterName;
    public Sprite sprite; // 레거시, 향후 삭제될 수 있음

    [Header("프리팹")]
    [Tooltip("이 몬스터 데이터에 해당하는 실제 프리팹")]
    public Monster prefab;

    [Header("스탯")]
    public int maxHP;
    public float moveSpeed;
    public int defense;
    public int magicResistance;
    public int goldReward;
}
