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
    public struct StageCurve   // 1~5단계 가정
    {
        [Range(0, 10)] public int maxStage;     // 사용중인 최대 단계 (기본 5)
        [Tooltip("단계별 공격력 % 증가 (예: 0.1 = +10%)")]
        public float[] attackPctByStage;       // length >= maxStage
        [Tooltip("단계별 공격속도 % 증가 (예: 0.1 = +10%)")]
        public float[] attackSpeedPctByStage;  // length >= maxStage

        public int[] attackFlatByStage; // 단계별 추가 고정 공격력

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
        [Tooltip("등급/코스트 키 (예: 1~9)")]
        public int costKey = 1;
        public StageCurve curve;
    }

    [Header("등급(코스트)별 업그레이드 곡선")]
#if ODIN_INSPECTOR
    [TableList(DrawScrollView = true, AlwaysExpanded = true)]
#endif
    public List<CostCurve> costCurves = new();

    [Header("Job 시너지별 업그레이드 곡선")]
#if ODIN_INSPECTOR
    [DictionaryDrawerSettings(DisplayMode = DictionaryDisplayOptions.Foldout)]
#endif
    public Dictionary<JobSynergy, StageCurve> jobCurves = new();

    [Header("Origin 시너지별 업그레이드 곡선")]
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
