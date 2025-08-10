using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelUpUI : MonoBehaviour
{
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text expText;
    [SerializeField] private Slider expBar; // 0~1 ���� ������ ����

    public void UpdateLevel(int level)
    {
        levelText.text = $"LV {level}";
    }

    public void UpdateExperience(int expInLevel, int levelExpNeeded)
    {
        // UI �ؽ�Ʈ
        if (levelExpNeeded > 0)
            expText.text = $"{expInLevel}/{levelExpNeeded}";
        else
            expText.text = "MAX";

        // �����̴� (0~1 ����)
        float normalized = levelExpNeeded > 0 ? (float)expInLevel / levelExpNeeded : 1f;
        expBar.value = normalized;

        Debug.Log($"[UI] ����ġ {expInLevel}/{levelExpNeeded} ({normalized * 100:F1}%)");
    }
}
