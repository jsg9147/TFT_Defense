using UnityEngine;

public class PlayerLevelManager : MonoSingleton<PlayerLevelManager>
{
    [Header("레벨 UI")]
    [SerializeField] private LevelUpUI levelUpUI;

    [Header("레벨 / 경험치")]
    [SerializeField] private int level = 1;
    [SerializeField] private int currentExp = 0;

    [Tooltip("각 레벨업에 필요한 경험치 (해당 레벨→다음 레벨까지 필요 경험치)")]
    public int[] expThresholds;
    // 예: [4, 6, 10] => 
    // 1→2:4, 2→3:6, 3→4:10

    public int Level => level;
    public int CurrentExp => currentExp;

    public delegate void LevelUpEvent(int newLevel);
    public event LevelUpEvent OnLevelUp;

    private void Start()
    {
        levelUpUI.UpdateLevel(level);
        levelUpUI.UpdateExperience(GetExpInLevel(), GetExpForCurrentLevel());
    }

    /// <summary> 경험치 추가 </summary>
    public void AddExp(int amount)
    {
        currentExp += amount;
        Debug.Log($"경험치 {amount} 추가 → 현재 누적 {currentExp}");

        CheckLevelUp();
    }

    /// <summary> 현재 레벨에서 다음 레벨까지 필요한 경험치 </summary>
    private int GetExpForCurrentLevel()
    {
        if (level > expThresholds.Length) return 0; // 최대 레벨이면 0
        return expThresholds[level - 1];
    }

    /// <summary> 현재 레벨 구간에서 획득한 경험치 </summary>
    private int GetExpInLevel()
    {
        int prevSum = 0;
        for (int i = 0; i < level - 1; i++)
            prevSum += expThresholds[i];

        return currentExp - prevSum;
    }

    /// <summary> 레벨업 체크 </summary>
    private void CheckLevelUp()
    {
        while (level <= expThresholds.Length && GetExpInLevel() >= GetExpForCurrentLevel())
        {
            level++;
            Debug.Log($"플레이어 레벨업! 현재 레벨 {level}");
            OnLevelUp?.Invoke(level);
        }

        levelUpUI.UpdateLevel(level);
        levelUpUI.UpdateExperience(GetExpInLevel(), GetExpForCurrentLevel());
    }

    /// <summary> 현재 레벨에서의 경험치 비율 (UI용) </summary>
    public float GetExpRatio()
    {
        int maxExp = GetExpForCurrentLevel();
        if (maxExp <= 0) return 1f; // 최대 레벨

        return Mathf.Clamp01((float)GetExpInLevel() / maxExp);
    }
}
