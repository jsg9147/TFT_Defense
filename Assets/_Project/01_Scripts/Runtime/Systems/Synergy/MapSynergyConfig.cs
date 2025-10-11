using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/MapSynergyConfig")]
public class MapSynergyConfig : ScriptableObject
{
    [Header("이 맵에서 노출/사용할 잡 시너지 (비워두면 Config 기반 자동)")]
    public List<JobSynergy> jobs;

    [Header("이 맵에서 노출/사용할 오리진 시너지 (비워두면 Config 기반 자동)")]
    public List<OriginSynergy> origins;

    [Header("표시 순서 커스터마이즈 (optional)")]
    public List<int> costOrderOverride; // 예: 1,2,3,4,5

    [Header("라벨/아이콘 (optional)")]
    public Dictionary<JobSynergy, Sprite> jobIcons;
    public Dictionary<OriginSynergy, Sprite> originIcons;

    public bool HasJobList => jobs != null && jobs.Count > 0;
    public bool HasOriginList => origins != null && origins.Count > 0;
    public bool HasCostOrder => costOrderOverride != null && costOrderOverride.Count > 0;
}
