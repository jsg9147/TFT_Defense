using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using System;

[CreateAssetMenu(menuName = "Unit/UnitData")]
public class UnitData : ScriptableObject
{
    [Header("프리팹 참조(필수 권장)")]
#if ODIN_INSPECTOR
    [InlineEditor(Expanded = true)]
#endif
    public GameObject unitPrefab;  // ← 유닛 고유 프리팹 (SPUM Export 결과 등)
    [Header("기본")]
    public string unitName;
    public Sprite icon;
    [Header("유닛 타입")]
#if ODIN_INSPECTOR
    [EnumToggleButtons, HideLabel]
#endif
    public UnitType types = UnitType.SingleShot | UnitType.Physical;
    [Header("유닛 직업")]
#if ODIN_INSPECTOR
    [EnumToggleButtons, HideLabel]
#endif
    public JobSynergy jobs = JobSynergy.None;
    [Header("고유 시너지")]
#if ODIN_INSPECTOR
    [EnumToggleButtons, HideLabel]
#endif
    public OriginSynergy origins = OriginSynergy.None;
    public int cost = 1;          // 1~9 가정 (게임 내 비용, UI 표시용 아님)

    [Header("전투 스탯")]
    public int baseAttack = 10;
    public float attackSpeed = 1.0f;     // 초당 공격
    public float range = 3.0f;           // 사거리

    [Header("탄/이펙트")]
    public GameObject projectilePrefab;  // 원거리 발사용 (물리/마법/원소 공통)

    [Header("MultiShot")]
    public int multishotCount = 3;       // 동시에 맞출 타겟 수

    [Header("Area")]
    public float areaRadius = 1.5f;      // AOE 반경

    [Header("Chain")]
    public int chainCount = 3;           // 최대 점프 횟수
    public float chainRange = 2.5f;      // 다음 대상 탐색 반경

    [Header("Poison(DoT)")]
    public int poisonDamagePerTick = 2;
    public float poisonTickInterval = 0.5f;
    public int poisonTickCount = 6;

    [Header("지원/특수(향후 확장용)")]
    public float buffValue = 0f;   // 예: 공격력% 증가
    public float debuffValue = 0f; // 예: 방어력% 감소
}


[System.Flags]
public enum UnitType
{
    None = 0,

    // 공격 패턴
    SingleShot = 1 << 0,   // 단일 타겟
    MultiShot = 1 << 1,   // 동시 다중 타겟 (N발)
    Area = 1 << 2,   // 범위 피해 (AOE)
    Chain = 1 << 3,   // 연쇄 점프 피해

    // 공격 속성
    Physical = 1 << 4,   // 물리
    Magic = 1 << 5,   // 마법
    Elemental = 1 << 6,   // 원소(불/물/바람/대지 등, 세부는 별도 enum로 확장 가능)
    Poison = 1 << 7,   // 지속 피해형(중독)

    // 지원/특수
    Buff = 1 << 8,   // 아군 강화
    Debuff = 1 << 9,   // 적 약화
    Summon = 1 << 10,  // 소환
}


[Flags]
public enum JobSynergy
{
    None = 0,
    Warrior = 1 << 0,
    Mage = 1 << 1,
    Ranger = 1 << 2,
    Assassin = 1 << 3,
    Guardian = 1 << 4,
    Support = 1 << 5,
    Engineer = 1 << 6,
    Summoner = 1 << 7,
}

[Flags]
public enum OriginSynergy
{
    None = 0,
    Kingdom = 1 << 0,
    Undead = 1 << 1,
    Beast = 1 << 2,
    Mech = 1 << 3,
    Spirit = 1 << 4,
    Void = 1 << 5,
    Goblin = 1 << 6,
    Slime = 1 << 7,
}
