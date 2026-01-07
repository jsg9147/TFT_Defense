# SummonManager 서버 연동 분석

## 현재 구조 분석

### 1. 네트워크 인프라
- ✅ Unity Netcode for GameObjects 사용 중
- ✅ NetworkPlayer에 골드/에센스/레벨이 NetworkVariable로 관리됨
- ✅ 서버 RPC로 통화 관리 (AddGoldServerRpc, SpendGoldServerRpc 등)
- ⚠️ CurrencyManager는 로컬 싱글톤 (NetworkPlayer와 분리됨)

### 2. SummonManager 현재 동작
```csharp
// 현재 흐름 (모두 클라이언트에서 실행)
1. CurrencyManager.Instance.SpendGold(summonCost)  // 로컬 차감
2. RollOneUnit()  // 클라이언트에서 랜덤 생성
3. TrySpawnUnitAtFirstFreeCell()  // 로컬 스폰
```

## 서버 연동 필요성 판단

### 🔴 **반드시 서버에서 처리해야 할 부분**

#### 1. **통화 차감 (골드/에센스)**
- **이유**: 치팅 방지 (무한 골드 생성 방지)
- **현재 문제**: 클라이언트에서 직접 CurrencyManager 사용
- **해결**: NetworkPlayer의 ServerRPC 사용

#### 2. **랜덤 유닛 생성 (RollOneUnit)**
- **이유**: 확률 조작 방지, 공정성 보장
- **현재 문제**: 클라이언트에서 Random.value 사용
- **해결**: 서버에서 랜덤 결정 후 결과를 클라이언트에 전달

#### 3. **플레이어 레벨 기반 확률**
- **이유**: 레벨 조작 방지, 일관된 확률 적용
- **현재 문제**: 클라이언트에서 PlayerLevelManager.Instance.Level 참조
- **해결**: NetworkPlayer의 Level NetworkVariable 사용

### 🟡 **서버 검증이 필요한 부분**

#### 4. **유닛 스폰 위치**
- **이유**: 보드 범위 초과 방지, 중복 배치 방지
- **현재**: GridManager에서 이미 검증 중 (로컬)
- **해결**: 서버에서도 최종 검증 필요 (선택적)

### 🟢 **클라이언트에서 처리 가능한 부분**

#### 5. **UI 업데이트**
- 확률 표시, 버튼 활성화/비활성화 등

#### 6. **로컬 유닛 스폰/배치**
- 서버에서 결과를 받은 후 클라이언트에서 시각적 표현

## 권장 아키텍처

### 옵션 1: 완전 서버 제어 (권장)
```
[클라이언트] SummonOnce() 호출
    ↓
[클라이언트] SummonServerRpc() 전송
    ↓
[서버] 골드 차감 검증
    ↓
[서버] 랜덤 유닛 생성 (RollOneUnit)
    ↓
[서버] SummonResultClientRpc() 전송 (UnitData 전달)
    ↓
[클라이언트] 유닛 스폰 및 배치
```

### 옵션 2: 하이브리드 (현재 구조 유지 + 서버 검증)
```
[클라이언트] 골드 차감 시도 (NetworkPlayer.SpendGoldServerRpc)
    ↓
[서버] 차감 성공 여부 반환
    ↓
[클라이언트] 성공 시 → SummonRequestServerRpc() 전송
    ↓
[서버] 랜덤 유닛 생성 후 결과 반환
    ↓
[클라이언트] 유닛 스폰
```

## 구현 우선순위

### Phase 1: 핵심 보안 (필수)
1. ✅ 통화 차감을 NetworkPlayer ServerRPC로 변경
2. ✅ 랜덤 유닛 생성을 서버에서 처리
3. ✅ 플레이어 레벨을 NetworkPlayer에서 가져오기

### Phase 2: 최적화 (선택)
4. 유닛 스폰 위치 서버 검증
5. 배치 실패 시 롤백 처리

## 주의사항

1. **CurrencyManager와 NetworkPlayer 통합 필요**
   - 현재 두 시스템이 분리되어 있음
   - 네트워크 모드일 때는 NetworkPlayer 사용
   - 싱글 플레이일 때는 CurrencyManager 사용

2. **로컬/네트워크 모드 분기**
   - `NetworkGameManager.IsNetworkMode()` 체크 필요
   - 싱글 플레이는 기존 로직 유지

3. **에러 처리**
   - 서버에서 차감 실패 시 클라이언트에 알림
   - 네트워크 지연 시 UI 피드백 필요

