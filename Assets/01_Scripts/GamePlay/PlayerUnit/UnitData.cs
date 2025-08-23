using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

[CreateAssetMenu(menuName = "Unit/UnitData")]
public class UnitData : ScriptableObject
{
    [Header("�⺻")]
    public string unitName;
    public Sprite icon;
#if ODIN_INSPECTOR
    [EnumToggleButtons, HideLabel]
#endif
    public UnitType types = UnitType.SingleShot | UnitType.Physical;
    public int cost = 1;          // 1~9 ���� (���� �� ���, UI ǥ�ÿ� �ƴ�)

    [Header("���� ����")]
    public int baseAttack = 10;
    public float attackSpeed = 1.0f;     // �ʴ� ����
    public float range = 3.0f;           // ��Ÿ�

    [Header("ź/����Ʈ")]
    public GameObject projectilePrefab;  // ���Ÿ� �߻�� (����/����/���� ����)

    [Header("MultiShot")]
    public int multishotCount = 3;       // ���ÿ� ���� Ÿ�� ��

    [Header("Area")]
    public float areaRadius = 1.5f;      // AOE �ݰ�

    [Header("Chain")]
    public int chainCount = 3;           // �ִ� ���� Ƚ��
    public float chainRange = 2.5f;      // ���� ��� Ž�� �ݰ�

    [Header("Poison(DoT)")]
    public int poisonDamagePerTick = 2;
    public float poisonTickInterval = 0.5f;
    public int poisonTickCount = 6;

    [Header("����/Ư��(���� Ȯ���)")]
    public float buffValue = 0f;   // ��: ���ݷ�% ����
    public float debuffValue = 0f; // ��: ����% ����
}


[System.Flags]
public enum UnitType
{
    None = 0,

    // ���� ����
    SingleShot = 1 << 0,   // ���� Ÿ��
    MultiShot = 1 << 1,   // ���� ���� Ÿ�� (N��)
    Area = 1 << 2,   // ���� ���� (AOE)
    Chain = 1 << 3,   // ���� ���� ����

    // ���� �Ӽ�
    Physical = 1 << 4,   // ����
    Magic = 1 << 5,   // ����
    Elemental = 1 << 6,   // ����(��/��/�ٶ�/���� ��, ���δ� ���� enum�� Ȯ�� ����)
    Poison = 1 << 7,   // ���� ������(�ߵ�)

    // ����/Ư��
    Buff = 1 << 8,   // �Ʊ� ��ȭ
    Debuff = 1 << 9,   // �� ��ȭ
    Summon = 1 << 10,  // ��ȯ
}