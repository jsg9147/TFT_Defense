using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

public class ShopSlotUI : MonoBehaviour
{
    [Header("UI ���")]
    public Image unitIconImage;
    public TextMeshProUGUI unitNameText;
    public TextMeshProUGUI costText;

    [Header("�ó��� ����")]
    public Transform synergyContent;
    public SynergyTagUI synergyTagPrefab;
    public Button buyButton;

    [HideInInspector] public UnitData unitData;

    // (����) ������ ���� ? ������ null ���ϵǾ� �ؽ�Ʈ�� ǥ�õ�
    [Header("�ó��� ������(����)")]
    public List<Sprite> jobIcons;   // Warrior, Mage ... ���� ����θ� �Ʒ� GetJobIcon���� ���
    public List<Sprite> originIcons;// Kingdom, Undead ... ���� ����θ� �Ʒ� GetOriginIcon���� ���

    private Action<UnitData> onBuyCallback;

    public void Init(UnitData data, Action<UnitData> onBuy)
    {
        unitData = data;
        onBuyCallback = onBuy;

        // �⺻ ǥ��
        if (unitIconImage) unitIconImage.sprite = data.icon;
        if (unitNameText) unitNameText.text = data.unitName;
        if (costText) costText.text = $"�� {data.cost}";

        // ��ư
        if (buyButton)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(OnBuyClicked);
        }

        // ���� �ó��� �±� ����
        ClearSynergyTags();

        // Job �ó��� �±� ����
        foreach (var job in GetActiveFlags(data.jobs))
            CreateSynergyTag(GetJobIcon(job), job.ToString());

        // Origin �ó��� �±� ����
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

    // === [Flags] ��ƿ ===
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

    // === ������ ���� (����) ===
    private Sprite GetJobIcon(JobSynergy job)
    {
        // ���� ���� ����: enum ������ jobIcons ����Ʈ �ε����� ����δ� ���
        // �ʿ��ϸ� Dictionary<JobSynergy, Sprite>�� ��ü
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
