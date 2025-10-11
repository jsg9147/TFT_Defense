using UnityEngine;

[CreateAssetMenu(menuName = "Monster/MonsterData")]
public class MonsterData : ScriptableObject
{
    public string monsterName;
    public Sprite sprite;
    public int maxHP;
    public float moveSpeed;
    public int defense;
    public int magicResistance;
    public int goldReward;
}
