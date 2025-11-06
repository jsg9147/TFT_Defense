// MonsterInspectable.cs
using UnityEngine;

[RequireComponent(typeof(Monster))]
public class MonsterInspectable : MonoBehaviour, IInspectable
{
    private Monster m;
    public string DisplayName => m?.data ? m.data.monsterName : name;
    public Sprite Icon =>/* m?.data ? m.data.icon : null;*/null;
    public Transform FollowTarget => transform;

    void Awake() => m = GetComponent<Monster>();

    public InspectData BuildData()
    {
        var d = m.data;
        var r = new InspectData
        {
            name = d.monsterName,
            //icon = d.icon,
            //hp = m.CurrentHP,
            //hpMax = m.MaxHP,
            //moveSpd = m.MoveSpeed,
            gold = d.goldReward,
            //tags = d.tags,         // 예: Armor/Resist/Type 등 문자열
            range = 0,             // 몬스터가 공격 안 한다면 0
            attack = 0,
            aps = 0
        };
        return r;
    }
}
