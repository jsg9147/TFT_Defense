# TFT Defense - Architecture Reviewer Memory

## 네임스페이스 현황
- 전체 프로젝트가 네임스페이스 없이 작성되어 있음 (Monster, Unit, KillLogPanel 등 전부 전역)
- `TFT_Defense.Managers` 네임스페이스만 일부 사용 (DamageTextManager 등)
- 네임스페이스 미적용이 프로젝트 전반의 기술 부채임 — 매 리뷰마다 지적하되, 일괄 적용 전까지는 Critical 1회 언급 후 넘어갈 것

## 싱글톤 패턴
- `MonoSingleton<T>`: DontDestroyOnLoad 적용, 씬 영속
- `SceneSingleton<T>`: DontDestroyOnLoad 없음, 씬 전환 시 파괴, OnDestroy에서 instance=null 정리됨
- KillLogPanel은 수동 static Instance 구현 (MonoSingleton/SceneSingleton 미사용) — UI 패널은 씬 종속이므로 SceneSingleton이 적합

## DamagePayload 구조체
- `Source` 필드 타입: `UnityEngine.Object` (MonoBehaviour 상위)
- `payload.Source as Unit` 캐스팅 패턴이 RecordDamage에서 사용됨
- 포이즌(DoT)은 `Source = this.gameObject` (GameObject)로 설정 — Unit 캐스팅 시 null 반환됨 (의도적 설계)

## KillLog 시스템 (신규, 리뷰 완료)
- Monster._damageLog: `Dictionary<string, int>` — unitName 키로 누적 데미지 기록
- 직렬화 포맷: `"MonsterName|UnitA:1500,UnitB:800"` — 구분자 충돌 위험 있음
- KillLogPanel.ParseEntries: 매 호출마다 string[] 배열 + List 신규 할당 (GC 압력)
- KillLogPanel._entryPool: 증가-전용 풀 (항목 감소 시 비활성화만 함, 올바른 패턴)
- HideAfterDelay: `new WaitForSeconds` 매 호출 생성 — 캐시 필요
- BroadcastKillLogClientRpc: string RPC 페이로드 — 유닛 수 증가 시 크기 고려 필요

## Unit.cs 기존 이슈 (이전 리뷰에서 확인)
- `Update()`에서 `monstersInRange.RemoveAll()` 매 프레임 호출 — delegate 클로저 GC 발생
- `FireArea()`에서 `Physics2D.OverlapCircleAll()` + LINQ 매 공격마다 할당
- `FireMultiShot()`에서 `.Take().ToList()` LINQ 할당
- `SpawnProjectile(Transform, int)` 오버로드 중복 존재 (dead code)
- `logBreakdown` 블록 중복 실행 버그 (if logBreakdown 두 번)
- `BuildBasicPayload()`와 `BuildImpactPayload()` 중복 — 통합 필요

## NetworkGameManager
- `IsNetworkMode()` 인스턴스 메서드 — 프로젝트 코딩 컨벤션에서는 static처럼 사용되나 실제로는 인스턴스 메서드
- Monster.NotifyKillLog에서 `NetworkGameManager.IsNetworkMode()` 호출이 인스턴스 없으면 null 위험

## 알려진 미완성 영역
- ShopManager: Shop phase 주석 처리됨
- SummonManager: 클라이언트 사이드 랜덤 (보안 위험)
- 네트워크 동기화: 유닛/전투 미완성
- VFX/사운드: TODO 플레이스홀더
