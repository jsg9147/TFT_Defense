using UnityEngine;

[CreateAssetMenu(fileName = "TierVisualConfig", menuName = "TFTDefense/TierVisualConfig")]
public class TierVisualConfig : ScriptableObject
{
    [Header("2성 스케일")]
    [Range(1f, 2f)] public float star2Scale = 1.3f;

    [Header("오오라 프리팹")]
    public GameObject auraStar3Prefab;   // 3성
    public GameObject auraStar4Prefab;   // 4성 이상

    [Header("오오라 배치 옵션")]
    public Vector3 localOffset = Vector3.zero;
    public bool inheritUnitScale = false;  // 유닛 스케일을 따라갈지(보통 false)
}
