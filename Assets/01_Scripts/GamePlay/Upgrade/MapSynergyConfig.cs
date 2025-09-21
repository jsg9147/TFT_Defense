using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/MapSynergyConfig")]
public class MapSynergyConfig : ScriptableObject
{
    [Header("�� �ʿ��� ����/����� �� �ó��� (����θ� Config ��� �ڵ�)")]
    public List<JobSynergy> jobs;

    [Header("�� �ʿ��� ����/����� ������ �ó��� (����θ� Config ��� �ڵ�)")]
    public List<OriginSynergy> origins;

    [Header("ǥ�� ���� Ŀ���͸����� (optional)")]
    public List<int> costOrderOverride; // ��: 1,2,3,4,5

    [Header("��/������ (optional)")]
    public Dictionary<JobSynergy, Sprite> jobIcons;
    public Dictionary<OriginSynergy, Sprite> originIcons;

    public bool HasJobList => jobs != null && jobs.Count > 0;
    public bool HasOriginList => origins != null && origins.Count > 0;
    public bool HasCostOrder => costOrderOverride != null && costOrderOverride.Count > 0;
}
