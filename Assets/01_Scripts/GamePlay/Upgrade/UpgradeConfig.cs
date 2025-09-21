using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

[CreateAssetMenu(menuName = "Game/UpgradeConfig")]
public class UpgradeConfig : SerializedScriptableObject
{
    [Serializable]
    public struct StageCurve   // 1~5�ܰ� ����
    {
        [Range(0, 10)] public int maxStage;     // ������� �ִ� �ܰ� (�⺻ 5)
        [Tooltip("�ܰ躰 ���ݷ� % ���� (��: 0.1 = +10%)")]
        public float[] attackPctByStage;       // length >= maxStage
        [Tooltip("�ܰ躰 ���ݼӵ� % ���� (��: 0.1 = +10%)")]
        public float[] attackSpeedPctByStage;  // length >= maxStage

        public int[] attackFlatByStage; // �ܰ躰 �߰� ���� ���ݷ�

        public (float atkPct, float aspdPct) GetBonus(int stage)
        {
            if (stage <= 0) return (0f, 0f);
            int idx = Mathf.Clamp(stage, 1, Mathf.Max(1, maxStage)) - 1;
            float atk = (attackPctByStage != null && attackPctByStage.Length > idx) ? attackPctByStage[idx] : 0f;
            float asp = (attackSpeedPctByStage != null && attackSpeedPctByStage.Length > idx) ? attackSpeedPctByStage[idx] : 0f;
            return (atk, asp);
        }

        public int GetFlatAttack(int stage)
        {
            if (stage <= 0 || attackFlatByStage == null) return 0;
            int idx = Mathf.Clamp(stage, 1, Mathf.Max(1, maxStage)) - 1;
            return (attackFlatByStage.Length > idx) ? attackFlatByStage[idx] : 0;
        }
    }

    [Serializable]
    public class CostCurve
    {
        [Tooltip("���/�ڽ�Ʈ Ű (��: 1~9)")]
        public int costKey = 1;
        public StageCurve curve;
    }

    [Header("���(�ڽ�Ʈ)�� ���׷��̵� �")]
#if ODIN_INSPECTOR
    [TableList(DrawScrollView = true, AlwaysExpanded = true)]
#endif
    public List<CostCurve> costCurves = new();

    [Header("Job �ó����� ���׷��̵� �")]
#if ODIN_INSPECTOR
    [DictionaryDrawerSettings(DisplayMode = DictionaryDisplayOptions.Foldout)]
#endif
    public Dictionary<JobSynergy, StageCurve> jobCurves = new();

    [Header("Origin �ó����� ���׷��̵� �")]
#if ODIN_INSPECTOR
    [DictionaryDrawerSettings(DisplayMode = DictionaryDisplayOptions.Foldout)]
#endif
    public Dictionary<OriginSynergy, StageCurve> originCurves = new();

    public StageCurve? FindCostCurve(int cost)
    {
        var c = costCurves.Find(x => x.costKey == cost);
        Debug.Log($"[UpgradeConfig] FindCostCurve({cost}) => {(c != null ? "FOUND" : "NULL")}");
        return c != null ? c.curve : (StageCurve?)null;
    }
}
