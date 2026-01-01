# TFT Defense 프로젝트 컨텍스트

## 프로젝트 개요

TFT Defense는 Teamfight Tactics 스타일의 오토 체스 메커니즘과 타워 디펜스 게임플레이를 결합한 Unity 게임 프로젝트입니다.

## 게임 플로우

1. **Prepare Phase**: 웨이브 시작 전 준비 시간
2. **Battle Phase**: 몬스터 스폰 및 전투 진행
3. **Shop Phase**: (현재 비활성화) 상점에서 유닛 구매
4. **Win/Lose**: 승리 또는 패배 조건 달성 시 종료

## 핵심 시스템

### 1. GameLoop 시스템 (`Systems/GameLoop/`)

게임의 전체 흐름을 관리하는 핵심 시스템입니다.

- **GameManager.cs**: 게임 상태 관리 및 웨이브 루프 제어
  - 상태: `Prepare`, `Battle`, `Shop`, `Win`, `Lose`
  - 웨이브 루프: Prepare → Battle → (Shop) → 다음 웨이브
  - 이벤트: `OnWaveChanged`, `OnPhaseChanged`, `OnTimerTick`, `OnTimerEnd`

- **CurrencyManager.cs**: 게임 내 통화(골드, 에센스) 관리
- **PlayerLevelManager.cs**: 플레이어 레벨 및 경험치 관리
- **MonsterFieldManager.cs**: 필드의 몬스터 수 추적 및 한도 관리
- **UIManager.cs**: UI 상태 전환 관리
- **WaveSet.cs**: 웨이브 데이터 구조

### 2. Monsters 시스템 (`Systems/Monsters/`)

몬스터 생성, 이동, 관리 시스템입니다.

- **MonsterSpawner.cs**: 웨이브별 몬스터 스폰 제어
- **Monster.cs**: 몬스터 개체 로직
- **MonsterData.cs**: 몬스터 데이터 구조 (ScriptableObject)
- **MonsterPathManager.cs**: 몬스터 이동 경로 관리
- **MonsterPool.cs**: 몬스터 오브젝트 풀링

### 3. Units 시스템 (`Systems/Units/`)

플레이어 유닛 배치, 전투, 진화 시스템입니다.

- **Unit.cs**: 유닛 기본 클래스
- **UnitData.cs**: 유닛 데이터 구조 (ScriptableObject)
- **UnitPlacementManager.cs**: 유닛 배치 관리
- **UnitDragHandler.cs**: 유닛 드래그 앤 드롭
- **UnitSelectionManager.cs**: 유닛 선택 관리
- **UnitSellManager.cs**: 유닛 판매 시스템
- **UnitRangeDetector.cs**: 유닛 공격 범위 감지
- **EvolutionManager.cs**: 유닛 진화 시스템
- **AuraService.cs**: 오라 효과 관리
- **TierVisualConfig.cs**: 티어별 시각적 설정

### 4. Economy 시스템 (`Systems/Economy/`)

경제 및 상점 시스템입니다.

- **ShopManager.cs**: 상점 시스템 관리
- **ShopProbabilityTable.cs**: 상점 확률 테이블
- **ShopProbabilityUI.cs**: 상점 확률 UI
- **SummonManager.cs**: 유닛 소환 관리

### 5. Synergy 시스템 (`Systems/Synergy/`)

유닛 간 시너지 효과 시스템입니다.

- **SynergyManager.cs**: 시너지 효과 계산 및 관리
- **MapSynergyConfig.cs**: 맵별 시너지 설정
- **SynergyUtil.cs**: 시너지 유틸리티 함수
- **SynergyRowUI.cs**, **SynergySummaryUI.cs**, **SynergyTagUI.cs**: 시너지 UI 컴포넌트

### 6. Combat 시스템 (`Systems/Combat/`)

전투 및 데미지 계산 시스템입니다.

- **DamageFormula.cs**: 데미지 계산 공식
- **IDamageable.cs**: 데미지를 받을 수 있는 객체 인터페이스

### 7. Battle 시스템 (`Systems/Battle/`)

전투 필드 및 그리드 관리 시스템입니다.

- **GridManager.cs**: 그리드 시스템 관리
- **GridCellManager.cs**: 그리드 셀 관리
- **BoardSlot.cs**: 보드 슬롯 (유닛 배치 위치)
- **Bullet.cs**: 투사체 로직

### 8. Upgrade 시스템 (`Systems/Upgrade/`)

업그레이드 시스템입니다.

- **UpgradeManager.cs**: 업그레이드 관리
- **UpgradeConfig.cs**: 업그레이드 설정
- **UpgradeButtonPanelBuilder.cs**: 업그레이드 버튼 UI 빌더

## 데이터 구조

### ScriptableObject 기반 데이터
- `PlayerUnitData/`: 플레이어 유닛 데이터 (Cost별 분류)
- `MonsterData/`: 몬스터 데이터
- `WaveData/`: 웨이브 데이터
- `ProbabilityTable/`: 확률 테이블
- `Upgrade/`: 업그레이드 설정

## UI 시스템

UI 컴포넌트는 `01_Scripts/UI/` 폴더에 위치합니다.

- **EssenceUI.cs**: 에센스 UI
- **GoldUI.cs**: 골드 UI
- **LevelUpUI.cs**: 레벨업 UI
- **WaveTimeUI.cs**: 웨이브 타이머 UI
- **FieldMonsterCounterUI.cs**: 필드 몬스터 카운터 UI
- **WinLosePanel.cs**: 승리/패배 패널
- **UnitInspectable/**: 유닛 검사 UI

## 코어 유틸리티

### 싱글톤 패턴
- **MonoSingleton.cs**: 씬 전역 싱글톤
- **SceneSingleton.cs**: 씬별 싱글톤

### 인터페이스
- **IDamageable.cs**: 데미지 받을 수 있는 객체
- **IMonsterFieldService.cs**: 몬스터 필드 서비스 인터페이스

## 주요 이벤트 흐름

1. **게임 시작**: `GameManager.InitializeGame()` → 웨이브 루프 시작
2. **웨이브 시작**: `OnWaveChanged` 이벤트 → UI 업데이트
3. **전투 시작**: `SetGameState(Battle)` → `MonsterSpawner.StartWave()`
4. **몬스터 도달**: `MonsterFieldManager.OnLimitReached` → 게임 종료
5. **웨이브 완료**: 모든 몬스터 처치 → 다음 웨이브 또는 승리

## 의존성 및 외부 에셋

- **SPUM**: 스프라이트 애니메이션 시스템
- **DamageNumbersPro**: 데미지 숫자 표시
- **Epic Toon FX**: 이펙트
- **TextMeshPro**: 텍스트 렌더링
- **Unity Input System**: 입력 처리

## 개발 가이드

### 새 시스템 추가 시
1. `Systems/` 폴더 하위에 적절한 카테고리 폴더 생성
2. 매니저 클래스는 싱글톤 패턴 고려
3. 이벤트 기반 통신 사용 권장
4. ScriptableObject로 데이터 분리

### 디버깅 팁
- `GameManager`의 `Debug.Log`로 게임 상태 추적
- 씬 재로딩 시 모든 매니저 초기화 확인
- 코루틴 중복 실행 방지 확인
- 이벤트 구독/해제 확인

