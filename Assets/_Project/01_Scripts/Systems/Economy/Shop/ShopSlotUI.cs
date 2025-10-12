using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

public class ShopSlotUI : MonoBehaviour
{
    [Header("UI 요소")]
    public Image unitIconImage;
    public TextMeshProUGUI unitNameText;
    public TextMeshProUGUI costText;

    [Header("시너지 정보")]
    public Transform synergyContent;
    public SynergyTagUI synergyTagPrefab;
    public Button buyButton;

    [HideInInspector] public UnitData unitData;

    // (선택) 아이콘 매핑 ? 없으면 null 리턴되어 텍스트만 표시됨
    [Header("시너지 아이콘(선택)")]
    public List<Sprite> jobIcons;   // Warrior, Mage ... 순서 맞춰두면 아래 GetJobIcon에서 사용
    public List<Sprite> originIcons;// Kingdom, Undead ... 순서 맞춰두면 아래 GetOriginIcon에서 사용

    private Action<UnitData> onBuyCallback;

    public void Init(UnitData data, Action<UnitData> onBuy)
    {
        unitData = data;
        onBuyCallback = onBuy;

        // 기본 표시
        if (unitIconImage) unitIconImage.sprite = data.icon;
        if (unitNameText) unitNameText.text = data.unitName;
        if (costText) costText.text = $"★ {data.cost}";

        // 버튼
        if (buyButton)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(OnBuyClicked);
        }

        // 기존 시너지 태그 비우기
        ClearSynergyTags();

        // Job 시너지 태그 생성
        foreach (var job in GetActiveFlags(data.jobs))
            CreateSynergyTag(GetJobIcon(job), job.ToString());

        // Origin 시너지 태그 생성
        foreach (var origin in GetActiveFlags(data.origins))
            CreateSynergyTag(GetOriginIcon(origin), origin.ToString());
    }

    private void OnBuyClicked()
    {
        Debug.Log($"[ShopSlotUI] Buy clicked for {unitData.unitName}");
        onBuyCallback?.Invoke(unitData);
    }
    private void ClearSynergyTags()
    {
        if (!synergyContent) return;
        for (int i = synergyContent.childCount - 1; i >= 0; i--)
            Destroy(synergyContent.GetChild(i).gameObject);
    }

    private void CreateSynergyTag(Sprite icon, string label)
    {
        if (!synergyContent || !synergyTagPrefab) return;
        var tag = Instantiate(synergyTagPrefab, synergyContent);
        tag.Set(icon, label);
    }

    // === [Flags] 유틸 ===
    private static IEnumerable<JobSynergy> GetActiveFlags(JobSynergy flags)
    {
        foreach (JobSynergy f in Enum.GetValues(typeof(JobSynergy)))
        {
            if (f == JobSynergy.None) continue;
            if ((flags & f) == f) yield return f;
        }
    }

    private static IEnumerable<OriginSynergy> GetActiveFlags(OriginSynergy flags)
    {
        foreach (OriginSynergy f in Enum.GetValues(typeof(OriginSynergy)))
        {
            if (f == OriginSynergy.None) continue;
            if ((flags & f) == f) yield return f;
        }
    }

    // === 아이콘 매핑 (선택) ===
    private Sprite GetJobIcon(JobSynergy job)
    {
        // 간단 매핑 예시: enum 순서와 jobIcons 리스트 인덱스를 맞춰두는 방식
        // 필요하면 Dictionary<JobSynergy, Sprite>로 교체
        int idx = job switch
        {
            JobSynergy.Warrior => 0,
            JobSynergy.Mage => 1,
            JobSynergy.Ranger => 2,
            JobSynergy.Assassin => 3,
            JobSynergy.Guardian => 4,
            JobSynergy.Support => 5,
            JobSynergy.Engineer => 6,
            JobSynergy.Summoner => 7,
            _ => -1
        };
        return (idx >= 0 && jobIcons != null && idx < jobIcons.Count) ? jobIcons[idx] : null;
    }

    private Sprite GetOriginIcon(OriginSynergy origin)
    {
        int idx = origin switch
        {
            OriginSynergy.Kingdom => 0,
            OriginSynergy.Undead => 1,
            OriginSynergy.Beast => 2,
            OriginSynergy.Mech => 3,
            OriginSynergy.Spirit => 4,
            OriginSynergy.Void => 5,
            OriginSynergy.Goblin => 6,
            OriginSynergy.Slime => 7,
            _ => -1
        };
        return (idx >= 0 && originIcons != null && idx < originIcons.Count) ? originIcons[idx] : null;
    }
}
