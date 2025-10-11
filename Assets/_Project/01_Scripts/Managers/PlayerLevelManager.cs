using UnityEngine;

public class PlayerLevelManager : MonoSingleton<PlayerLevelManager>
{
    [Header("���� UI")]
    [SerializeField] private LevelUpUI levelUpUI;

    [Header("���� / ����ġ")]
    [SerializeField] private int level = 1;
    [SerializeField] private int currentExp = 0;

    [Tooltip("�� �������� �ʿ��� ����ġ (�ش� ��������� �������� �ʿ� ����ġ)")]
    public int[] expThresholds;
    // ��: [4, 6, 10] => 
    // 1��2:4, 2��3:6, 3��4:10

    public int Level => level;
    public int CurrentExp => currentExp;

    public delegate void LevelUpEvent(int newLevel);
    public event LevelUpEvent OnLevelUp;

    private void Start()
    {
        levelUpUI.UpdateLevel(level);
        levelUpUI.UpdateExperience(GetExpInLevel(), GetExpForCurrentLevel());
    }

    /// <summary> ����ġ �߰� </summary>
    public void AddExp(int amount)
    {
        currentExp += amount;
        Debug.Log($"����ġ {amount} �߰� �� ���� ���� {currentExp}");

        CheckLevelUp();
    }

    /// <summary> ���� �������� ���� �������� �ʿ��� ����ġ </summary>
    private int GetExpForCurrentLevel()
    {
        if (level > expThresholds.Length) return 0; // �ִ� �����̸� 0
        return expThresholds[level - 1];
    }

    /// <summary> ���� ���� �������� ȹ���� ����ġ </summary>
    private int GetExpInLevel()
    {
        int prevSum = 0;
        for (int i = 0; i < level - 1; i++)
            prevSum += expThresholds[i];

        return currentExp - prevSum;
    }

    /// <summary> ������ üũ </summary>
    private void CheckLevelUp()
    {
        while (level <= expThresholds.Length && GetExpInLevel() >= GetExpForCurrentLevel())
        {
            level++;
            Debug.Log($"�÷��̾� ������! ���� ���� {level}");
            OnLevelUp?.Invoke(level);
        }

        levelUpUI.UpdateLevel(level);
        levelUpUI.UpdateExperience(GetExpInLevel(), GetExpForCurrentLevel());
    }

    /// <summary> ���� ���������� ����ġ ���� (UI��) </summary>
    public float GetExpRatio()
    {
        int maxExp = GetExpForCurrentLevel();
        if (maxExp <= 0) return 1f; // �ִ� ����

        return Mathf.Clamp01((float)GetExpInLevel() / maxExp);
    }
}
