using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EvolutionManager : MonoSingleton<EvolutionManager>
{
    [Header("설정")]
    [Tooltip("벤치까지 포함할지 여부 (기본: 꺼짐, '필드만' 카운트)")]
    [SerializeField] private bool includeBench = false;
    [Tooltip("최대 성급 (설계: 최대 5성)")]
    [SerializeField] private int maxStar = 5;
    [Tooltip("연쇄 진화 허용 (3개 → 2성, 또 3개 모이면 계속)")]
    [SerializeField] private bool allowChain = true;

    // data별, 성급별 버킷
    // buckets[data][star] = 해당 스타 유닛 리스트(필드에 존재하는 것만)
    private readonly Dictionary<UnitData, Dictionary<int, List<Unit>>> buckets = new();
    // 정렬용 타임스탬프(최근 배치 우선 업그레이드 등 정책에 사용)
    private readonly Dictionary<Unit, float> placedAt = new();

    // === 외부에서 호출: 필드에 올라왔을 때 ===
    public void RegisterOnField(Unit u)
    {
        if (!u || !u.data) return;

        if (!buckets.TryGetValue(u.data, out var byStar))
        {
            byStar = new Dictionary<int, List<Unit>>();
            buckets[u.data] = byStar;
        }
        if (!byStar.TryGetValue(u.starLevel, out var list))
        {
            list = new List<Unit>();
            byStar[u.starLevel] = list;
        }
        if (!list.Contains(u)) list.Add(u);
        placedAt[u] = Time.time;

        TryEvolve(u.data, u.starLevel);
    }

    // === 외부에서 호출: 필드에서 내려갔을 때(파괴/벤치 이동/비활성화 등) ===
    public void UnregisterOnField(Unit u)
    {
        if (!u || !u.data) return;

        if (buckets.TryGetValue(u.data, out var byStar) &&
            byStar.TryGetValue(u.starLevel, out var list))
        {
            list.Remove(u);
            if (list.Count == 0) byStar.Remove(u.starLevel);
            if (byStar.Count == 0) buckets.Remove(u.data);
        }
        placedAt.Remove(u);
    }

    // === 핵심: 같은 data + 같은 star 가 3개 모였으면 업그레이드 ===
    private void TryEvolve(UnitData data, int star)
    {
        if (!buckets.TryGetValue(data, out var byStar)) return;
        if (!byStar.TryGetValue(star, out var list)) return;
        if (star >= maxStar) return;

        while (list.Count >= 3)
        {
            // 1개는 업그레이드 타겟, 2개는 소모
            Unit target = PickUpgradeTarget(list);
            var toConsume = PickConsumeTwo(list, target);

            // 실제 적용
            DoEvolve(target, toConsume);

            // 버킷 갱신: 기존 star 리스트에서 제거
            list.Remove(target);
            foreach (var c in toConsume) list.Remove(c);
            if (list.Count == 0) byStar.Remove(star);

            // 업그레이드된 유닛을 새 성급 버킷에 추가
            int newStar = Mathf.Min(maxStar, star + 1);
            if (!byStar.TryGetValue(newStar, out var higher))
            {
                higher = new List<Unit>();
                byStar[newStar] = higher;
            }
            higher.Add(target);

            // 연쇄 진화 허용 시 계속
            if (!allowChain) break;
            if (newStar >= maxStar) break;
            // 새 성급 버킷에 3개가 쌓였을 수도 있으므로 루프를 반복
            // (단, target 1개만으로는 불가하니 일반적으로 다른 동일 성급이 더 있어야 함)
            if (!byStar.TryGetValue(newStar, out var maybeNext) || maybeNext.Count < 3) break;

            // 다음 단계 시도
            data = target.data;
            star = newStar;
            list = maybeNext;
        }
    }

    private Unit PickUpgradeTarget(List<Unit> sameStarList)
    {
        // 정책: 가장 최근에 배치한 유닛(시각적으로 변화가 방금 생겨서 피드백이 좋음)
        return sameStarList
            .OrderByDescending(u => placedAt.TryGetValue(u, out var t) ? t : 0f)
            .First();
    }

    private List<Unit> PickConsumeTwo(List<Unit> sameStarList, Unit exclude)
    {
        // target을 제외하고 가장 오래된 2개를 소모
        return sameStarList
            .Where(u => u != exclude)
            .OrderBy(u => placedAt.TryGetValue(u, out var t) ? t : 0f)
            .Take(2)
            .ToList();
    }

    private void DoEvolve(Unit target, List<Unit> consume)
    {
        // 1) 소모 유닛 언레지스터 + 파괴
        foreach (var c in consume)
        {
            SynergyManager.Instance?.UnregisterUnit(c); // 있으면 정합성 유지
            UnregisterOnField(c);
            PlayConsumeVFX(c.transform.position);
            Destroy(c.gameObject);
        }

        // 2) 타겟 성급 증가
        target.starLevel = Mathf.Min(maxStar, target.starLevel + 1);

        // (선택) 성급에 따른 비주얼/스탯 갱신: 데미지는 이미 starLevel을 참조
        // 공격속도/사거리도 성급 반영하고 싶다면 Unit에 public 메서드 하나 추가 권장
        PlayEvolveVFX(target.transform.position);
        target.RefreshAfterStarChanged();
        // 시너지 재집계는 유지(같은 UnitData라면 보통 변화 없음). 필요 시 알림:
        // SynergyManager.Instance?.OnSynergyChanged?.Invoke();
    }

    private void PlayConsumeVFX(Vector3 pos)
    {
        // TODO: 파티클/사운드 훅 (비어있어도 무방)
        // e.g., VFXManager.Instance.Play("consume", pos);
    }

    private void PlayEvolveVFX(Vector3 pos)
    {
        // TODO: 파티클/사운드 훅
        // e.g., VFXManager.Instance.Play("evolve", pos);
    }

    // 디버그용: 현재 버킷 덤프
    [ContextMenu("Dump Buckets")]
    private void Dump()
    {
        foreach (var kv in buckets)
        {
            string d = kv.Key ? kv.Key.unitName : "NULL";
            string lines = string.Join(", ", kv.Value.Select(v => $"{v.Key}★x{v.Value.Count}"));
            Debug.Log($"[Evolution] {d}: {lines}");
        }
    }
}
