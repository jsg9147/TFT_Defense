// IInspectable.cs
using UnityEngine;

public interface IInspectable
{
    string DisplayName { get; }
    Sprite Icon { get; }
    Transform FollowTarget { get; }

    // 숫자/태그/텍스트를 모은 전송용 구조체
    InspectData BuildData();
}

public struct InspectData
{
    // 공통
    public string name;
    public Sprite icon;
    public int star;           // 유닛만 있으면 0 허용
    public int cost;           // 유닛만
    public string tags;        // Job/Origin/Type 요약

    // 전투 스탯(표시 전용, 가공 숫자)
    public int attack;
    public float aps;          // 1초당 공격수
    public float range;

    // 몬스터 전용(있으면 표시, 없으면 숨김)
    public int hp;
    public int hpMax;
    public float moveSpd;
    public int gold;

    // 부가 정보
    public string synergySummary;   // Mage x2 … 등
    public string upgradeBreakdown; // 업그레이드 합산 로그
}
