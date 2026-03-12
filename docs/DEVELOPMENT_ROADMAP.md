# TFT Defense - 개발 우선순위 로드맵

> 문서 작성일: 2026-03-08
> 기반 분석: 실제 소스코드 및 docs/systems/ 문서 직접 검토
> 대상 독자: 1인 개발자 (또는 소규모 팀)

---

## 1. 현재 상태 요약

### 작동하는 것 (실제 코드 확인 기준)

| 시스템 | 구현 수준 | 근거 |
|--------|----------|------|
| 게임 루프 | 완성에 가까움 | `GameManager.WaveLoop()` 코루틴, 그룹 단위 전투 페이즈, 패배 판정까지 구현 |
| 몬스터 스폰/이동 | 완성에 가까움 | `Monster.cs` NetworkBehaviour, `MonsterFieldManager` 플레이어별 한도 카운팅, `MonsterPathManager` 웨이포인트 |
| 전투 (유닛 공격) | 핵심 구현됨 | `Unit.cs` 단일샷/멀티샷/에어리어/체인 패턴, `DamageFormula.cs` 물리/마법/광역/고정 피해, `Bullet.cs` 발사체 |
| 그리드/배치 | 완성 | `GridManager.cs` 8x5 슬롯 관리, 드래그 앤 드롭(`UnitDragHandler.cs`), 판매(`UnitSellManager.cs`) |
| 시너지 | 완성에 가까움 | `SynergyManager.cs` 직업8/오리진8 플래그 열거형, 임계값별 보너스, `SynergySnapshot`으로 데미지 보정 |
| 경제 (싱글) | 부분 완성 | `SummonManager.cs` 확률테이블 기반 소환, 네트워크/싱글 분기, `CurrencyManager.cs` 골드/에센스 |
| 업그레이드 | 완성에 가까움 | `UpgradeManager.cs` 코스트/직업/오리진별 배율 곡선, `UpgradeConfig.cs` ScriptableObject |
| 3성 진화 | 구현됨 | `EvolutionManager.cs`, `AuraService.cs` 오라 비주얼 |
| UI (기본) | 부분 완성 | `UIManager.cs` 배틀/승패 패널, `WinLosePanel.cs` 결과 스냅샷, 골드/에센스/레벨UI, 킬로그 |
| 카메라 | 완성 | `CameraMovementManager.cs` |
| 네트워크 (인프라) | 부분 완성 | `NetworkGameManager.cs` 호스트/클라이언트 연결, `NetworkPlayer.cs` 골드/경험치 NetworkVariable + ServerRPC |

### 작동하지 않는 것 (크리티컬 문제)

| 문제 | 심각도 | 근거 |
|------|--------|------|
| 상점 페이즈 완전 비활성화 | 높음 | `GameManager.WaveLoop()`에 Shop 페이즈 분기 없음. `GameState.Shop` 열거형은 있으나 `SetGameState(Shop)` 호출 없음 |
| 유닛 배치 네트워크 동기화 없음 | 높음 | `SummonManager.TrySpawnUnitAtFirstFreeCell()`이 `Instantiate()`만 사용. NetworkObject 스폰 아님 |
| 유닛 전투 네트워크 동기화 없음 | 높음 | `Unit.cs`는 일반 MonoBehaviour. 멀티에서 각 클라이언트가 독립적으로 공격 계산 |
| SummonManager 랜덤이 클라이언트 사이드 | 중간 | `Random.value` 사용. 멀티에서 플레이어마다 다른 유닛 소환 |
| VFX/사운드 시스템 없음 | 중간 | `05_Audio/` 폴더 비어있음. `OnSummonSuccess()` 내 TODO 주석만 존재 |
| 데이터 부족 (유닛) | 높음 | Cost01에 Archer/Magicion 2개만 존재. Cost02~05 데이터 없음 |
| 데이터 부족 (몬스터) | 중간 | MonsterTest 1~5 및 BossTest만 존재. 정식 웨이브 구성 없음 |
| WaveLoop 클리어 조건 불완전 | 중간 | `monsterSpawner.AliveCount == 0` 체크가 로그에만 있음. 다음 그룹으로 자동 진행 (클리어 대기 없음) |
| NetworkPlayer 레벨업 하드코딩 | 낮음 | `expPerLevel = 4`, `maxLevel = 10`이 상수로 박혀있음. PlayerLevelManager와 분리 필요 |

---

## 2. 개발 우선순위 로드맵

### Phase 1 - 필수/긴급 (MVP를 위한 최소 조건)

**목표**: 싱글플레이 기준으로 처음부터 끝까지 한 번 플레이할 수 있는 상태

| # | 작업 | 설명 | 복잡도 | 선행 조건 |
|---|------|------|--------|----------|
| 1-1 | 유닛 데이터 추가 (Cost01~05) | Archer/Magicion 외 최소 10~15종 UnitData ScriptableObject 생성, prefab 연결 | 낮음 | 없음 |
| 1-2 | 몬스터 데이터 정비 | MonsterTest들을 정식 MonsterData로 교체, WaveSet 5웨이브 이상 구성 | 낮음 | 없음 |
| 1-3 | WaveLoop 클리어 대기 로직 수정 | 각 그룹 종료 후 `AliveCount == 0`이 될 때까지 대기하는 코루틴 추가 | 낮음 | 없음 |
| 1-4 | 상점 페이즈 활성화 | `WaveLoop()`에 Shop 페이즈 복원, `UIManager.ShowGambleUI()` 호출 연동, 타이머 작동 | 중간 | 없음 |
| 1-5 | 골드 리워드 시스템 | 웨이브 클리어/몬스터 처치 시 골드 지급 로직 연결 (`MonsterData.goldReward` → `CurrencyManager.AddGold`) | 낮음 | 1-2 |
| 1-6 | 기본 사운드 추가 | 공격, 몬스터 처치, 소환 효과음 최소 구현 (AudioSource 컴포넌트 수준) | 낮음 | 없음 |

---

### Phase 2 - 핵심 기능 (게임의 재미 루프 완성)

**목표**: 반복 플레이가 가능하고, 시너지와 업그레이드 전략이 의미있는 상태

| # | 작업 | 설명 | 복잡도 | 선행 조건 |
|---|------|------|--------|----------|
| 2-1 | 시너지 효과 실제 적용 검증 | `SynergyManager.GetSnapshotFor()` → `Unit.BuildBasicPayload()` 연결이 모든 시너지 타입에 작동하는지 확인 및 누락 효과 구현 | 중간 | 없음 |
| 2-2 | 유닛 판매 밸런싱 | `UnitSellManager.cs` 판매가 공식 확정, UI 피드백 | 낮음 | 1-1 |
| 2-3 | 플레이어 레벨업 통합 | `PlayerLevelManager` 경험치 임계값 데이터화, `NetworkPlayer.CheckLevelUp()`과 동일 공식 사용 | 낮음 | 없음 |
| 2-4 | 3성 진화 시각 효과 완성 | `EvolutionManager` → 진화 성공 시 VFX/사운드/UI 알림 | 중간 | 1-6 |
| 2-5 | 인스펙트 패널 완성 | `InspectPanelManager.cs`, `UnitInspectable.cs` 유닛 상세 스탯 표시 (시너지 보너스 포함) | 낮음 | 2-1 |
| 2-6 | 킬로그 연동 | `KillLogPanel.cs` 몬스터 처치 시 자동 로그 표시 | 낮음 | 1-2 |
| 2-7 | 보스 웨이브 구현 | `BossTest.asset` 활용, 보스 처치 시 특별 보상 | 중간 | 1-2, 1-5 |
| 2-8 | 다중 유닛 타입 밸런싱 | Area/Chain/MultiShot/Poison 유닛의 스탯 조정 및 테스트 | 중간 | 1-1, 2-1 |

---

### Phase 3 - 완성도 (폴리싱 및 게임 품질)

**목표**: 공개 시연 또는 빌드 배포가 가능한 수준

| # | 작업 | 설명 | 복잡도 | 선행 조건 |
|---|------|------|--------|----------|
| 3-1 | VFX 시스템 구축 | 공격/피격/스킬 파티클 이펙트, DamageNumbersPro 연동 | 높음 | Phase 2 |
| 3-2 | UI 폴리싱 | 배틀HUD 정비, 시너지 요약 UI(`SynergySummaryUI.cs`), 업그레이드 패널 완성 | 중간 | Phase 2 |
| 3-3 | 배경음악/사운드 시스템 | BGM 매니저, 페이즈별 음악 전환, 효과음 풀링 | 중간 | 1-6 |
| 3-4 | 씬 전환 및 로비 | StartScene → NetworkLobby → GameScene 완전한 흐름, 싱글/멀티 선택 UI | 중간 | 없음 |
| 3-5 | 결과 화면 완성 | `WinLosePanel.cs` 상세 통계 (처치 몬스터 수, 최대 시너지, 사용 골드) | 낮음 | Phase 2 |
| 3-6 | 튜토리얼 또는 UI 가이드 | 첫 플레이 안내, 시너지 임계값 표시, 유닛 배치 힌트 | 높음 | Phase 2 |
| 3-7 | 설정 화면 | 음량, 해상도, 그래픽 품질 설정 | 낮음 | 3-3 |

---

### Phase 4 - 확장 (추가 콘텐츠 및 멀티플레이)

**목표**: 장기적인 콘텐츠 확장 및 멀티플레이 완성

| # | 작업 | 설명 | 복잡도 | 선행 조건 |
|---|------|------|--------|----------|
| 4-1 | 유닛 네트워크 동기화 | `SummonManager`를 ServerRPC 기반으로 전환, 유닛 배치를 NetworkObject로 스폰 | 높음 | Phase 3 |
| 4-2 | 전투 네트워크 동기화 | `Unit.cs` 공격 로직 서버 권위 처리, 데미지 검증 RPC | 높음 | 4-1 |
| 4-3 | 멀티플레이 UI 분리 | 각 플레이어 보드 카메라 독립, 상대방 보드 관전 뷰 | 높음 | 4-1 |
| 4-4 | SummonManager 서버 랜덤화 | `RollOneUnit()`을 서버 RPC로 이전, `SummonResultClientRpc` 패턴 구현 | 높음 | 4-1 |
| 4-5 | 추가 유닛 콘텐츠 | Cost01~05 각 5종 이상, 특수 스킬 유닛 (Summoner, Engineer 직업 실현) | 높음 | Phase 3 |
| 4-6 | 추가 맵/스테이지 | 서로 다른 웨이포인트 경로, 맵별 시너지 보너스 | 중간 | Phase 3 |
| 4-7 | 랭킹/저장 시스템 | 로컬 기록 저장, PlayerPrefs 또는 외부 백엔드 | 높음 | Phase 3 |

---

## 3. 의존성 맵

```
[데이터 기반: 1-1 유닛, 1-2 몬스터]
       │
       ├──▶ [1-3 웨이브 클리어 대기 수정]
       │
       ├──▶ [1-4 상점 페이즈 활성화] ──▶ [1-5 골드 리워드]
       │
       └──▶ [2-1 시너지 효과 검증] ──▶ [2-8 밸런싱]
                    │
                    └──▶ [2-5 인스펙트 패널]

[1-1 ~ 1-6] Phase 1 완성
       │
       ├──▶ [2-2 판매 밸런싱]
       ├──▶ [2-3 레벨업 통합]
       ├──▶ [2-4 진화 이펙트] ← [1-6 사운드]
       ├──▶ [2-6 킬로그]
       └──▶ [2-7 보스 웨이브]

Phase 2 완성
       │
       ├──▶ [3-1 VFX]
       ├──▶ [3-2 UI 폴리싱]
       ├──▶ [3-3 BGM] ← [1-6]
       ├──▶ [3-4 씬 전환]
       └──▶ [3-5 결과 화면]

Phase 3 완성
       │
       ├──▶ [4-1 유닛 네트워크] ──▶ [4-2 전투 네트워크] ──▶ [4-3 멀티 UI]
       ├──▶ [4-4 서버 랜덤 소환] (← 4-1 선행 필요)
       ├──▶ [4-5 추가 콘텐츠]
       └──▶ [4-6 추가 맵]
```

---

## 4. MVP 체크리스트

최소 플레이 가능한 빌드 기준: **싱글플레이, 5웨이브, 처음부터 끝까지 게임 흐름 완주 가능**

### 데이터
- [ ] Cost01~02 유닛 최소 4종 이상 (UnitData + 프리팹)
- [ ] 몬스터 3종 이상 정식 데이터 (MonsterData, 스탯 조정 완료)
- [ ] WaveSet 5웨이브 구성 (웨이브당 그룹 1~2개)
- [ ] 소환 확률 테이블 레벨 1~5 기입

### 게임 루프
- [ ] Prepare → Shop → Battle → (반복) → Win/Lose 전체 흐름 작동
- [ ] 준비 시간 타이머 표시
- [ ] 웨이브 클리어 후 다음 웨이브로 자동 진행
- [ ] 필드 한도 도달 시 패배 판정 작동
- [ ] 마지막 웨이브 클리어 시 승리 화면 표시

### 경제
- [ ] 소환 비용 차감 및 유닛 자동 배치
- [ ] 몬스터 처치 시 골드 획득
- [ ] 웨이브 클리어 보너스 골드
- [ ] 유닛 판매 작동

### 전투
- [ ] 유닛이 사거리 내 몬스터를 자동 공격
- [ ] 몬스터 HP 감소 및 처치 작동
- [ ] 몬스터가 웨이포인트를 따라 이동
- [ ] 시너지 2개 이상 활성화 시 보너스 적용 확인 가능

### UI
- [ ] 현재 골드/에센스 표시
- [ ] 현재 웨이브 번호 표시
- [ ] 타이머 표시
- [ ] 승리/패배 화면 표시
- [ ] 재시작 버튼 작동

### 안정성
- [ ] 씬 재시작 후 버그 없이 재플레이 가능
- [ ] NullReference 에러 없이 5웨이브 완주
- [ ] 메모리 누수 없이 `MonsterPool` 재사용 정상 동작

---

## 5. 주요 기술 부채

코드 분석에서 발견한 즉시 인지해야 할 부채들입니다.

### WaveLoop 클리어 조건 버그
`GameManager.WaveLoop()`에서 각 그룹의 배틀 타이머가 만료되면 남은 몬스터와 관계없이 다음 그룹으로 넘어갑니다. `monsterSpawner.AliveCount == 0` 체크가 `Debug.Log`에만 있고 실제 대기 로직이 없습니다.
→ **핵심 파일**: `Assets/_Project/01_Scripts/Systems/GameLoop/GameManager.cs`

### NetworkPlayer 레벨업 하드코딩
`CheckLevelUp()`에서 `expPerLevel = 4`, `maxLevel = 10`이 상수로 박혀있어 `PlayerLevelManager`의 데이터 기반 임계값과 불일치합니다.
→ **핵심 파일**: `Assets/_Project/01_Scripts/Systems/Network/NetworkPlayer.cs`

### Unit.cs 중복 코드
`SpawnProjectile(Transform, int)` 구버전 메서드가 현재 사용 중인 `SpawnProjectile(Transform)` 메서드와 공존합니다. `logBreakdown` 블록도 중복 실행됩니다.
→ **핵심 파일**: `Assets/_Project/01_Scripts/Systems/Units/Unit.cs`

### SummonManager Singleton 패턴 불일치
다른 매니저들은 `MonoSingleton<T>`를 사용하지만 `SummonManager`는 `public static SummonManager Instance`를 직접 선언합니다.
→ **핵심 파일**: `Assets/_Project/01_Scripts/Systems/Economy/SummonManager.cs`

---

## 6. 핵심 파일 참조

| 목적 | 파일 경로 |
|------|----------|
| 웨이브 루프 수정, 상점 페이즈 활성화 | `Assets/_Project/01_Scripts/Systems/GameLoop/GameManager.cs` |
| 네트워크 소환 서버화 (Phase 4 대상) | `Assets/_Project/01_Scripts/Systems/Economy/SummonManager.cs` |
| 유닛 네트워크 동기화 참고 모델 | `Assets/_Project/01_Scripts/Systems/Monsters/Monster.cs` |
| 멀티플레이 경제 시스템 | `Assets/_Project/01_Scripts/Systems/Network/NetworkPlayer.cs` |
| Phase 4 네트워크 전투 전환 대상 | `Assets/_Project/01_Scripts/Systems/Units/Unit.cs` |
