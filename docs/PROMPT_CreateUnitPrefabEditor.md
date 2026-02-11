# 유닛 프리팹 에디터 구현 프롬프트

아래 내용을 Claude Code 등 AI 코딩 어시스턴트에 복사해 붙여넣어 사용하세요.

---

## 바로 복사용 (한 번에 붙여넣기)

```
Unity TFT Defense 프로젝트에서 유닛 프리팹을 자동 생성하는 에디터 도구를 만들어주세요.

현재 상태:
- CreateUnitDataWindow로 UnitData(ScriptableObject)만 생성됨
- 실제 GameObject 유닛 프리팹은 씬에서 수동 제작 중

요구사항:
1. Tools/Units/Create Unit Prefab 메뉴로 열리는 EditorWindow
2. 템플릿 프리팹(UnitPrefab.prefab)을 복제해 새 유닛 프리팹 생성
3. UnitData 선택 시 해당 UnitData를 Unit.data에 연결 + UnitData.unitPrefab 역연결
4. 저장 경로: Assets/_Project/02_Prefabs/SpumPrefabs/Unit/CostXX/ (폴더 없으면 생성)
5. 여러 UnitData 일괄 처리 옵션

Unit 컴포넌트(Unit.cs)가 필요로 하는 참조: animator, canvasGroup, firePoint, data, bulletPrefab, visualRoot.
템플릿에 이 구조가 있으므로 복제 후 data만 바꾸면 됩니다.
```

---

## 과제 요약 (상세)

Unity TFT Defense 프로젝트에서 **유닛 프리팹을 자동으로 생성하는 에디터 도구**를 만들어주세요.  
현재는 UnitData(ScriptableObject)만 에디터로 생성하고, 실제 GameObject 프리팹은 씬에서 수동으로 만드는 상황입니다.  
이를 개선해 **템플릿 기반으로 유닛 프리팹을 일괄 생성하고 UnitData와 양방향으로 연결**하는 도구를 구현해주세요.

---

## 프로젝트 컨텍스트

- **엔진**: Unity (C#)
- **게임**: 타워 디펜스 + 오토 체스 하이브리드
- **스프라이트 시스템**: SPUM 사용 (캐릭터 비주얼)
- **코딩 규칙**: `.cursorrules` 참고 – PascalCase, camelCase, `#if UNITY_EDITOR` 등

---

## 기존 구조

### 1. UnitData (ScriptableObject)
경로: `Assets/_Project/01_Scripts/Systems/Units/UnitData.cs`

- `unitPrefab` (GameObject): 유닛 프리팹 참조
- `unitName`, `icon`, `types`, `jobs`, `origins`, `cost`
- 전투 스탯: `baseAttack`, `attackSpeed`, `range`
- `projectilePrefab`, `multishotCount`, `areaRadius`, `chainCount` 등

### 2. Unit (MonoBehaviour)
경로: `Assets/_Project/01_Scripts/Systems/Units/Unit.cs`

필수/권장 참조:
- `animator` (Animator)
- `canvasGroup` (CanvasGroup)
- `firePoint` (Transform) – 발사 지점
- `data` (UnitData)
- `bulletPrefab` (구버전 호환, 없으면 data.projectilePrefab 사용)
- `visualRoot` (Transform, 없으면 transform 사용)
- UnitRangeDetector – 자식으로 없으면 `EnsureRangeDetector()`가 자동 생성

`[RequireComponent(typeof(UnitInspectable))]` 적용됨.

### 3. CreateUnitDataWindow (기존 에디터)
경로: `Assets/_Project/01_Scripts/Editor/CreateUnitDataWindow.cs`

- UnitData ScriptableObject만 생성
- 저장 경로: 폴더 지정 + `CostXX` 하위 폴더 옵션
- `Tools/Units/Create UnitData` 메뉴로 열림

### 4. 경로 구조

- **UnitData**: `Assets/_Project/04_Data/PlayerUnitData/CostXX/Unit_X_XXX.asset`
- **유닛 프리팹**: `Assets/_Project/02_Prefabs/SpumPrefabs/Unit/CostXX/Unit_X_XXX.prefab`
- **템플릿 프리팹**: `Assets/_Project/02_Prefabs/UnitPrefab.prefab` (또는 새로 지정 가능)

---

## 구현 요구사항

### 기능 1: 유닛 프리팹 생성 에디터 윈도우

- `Tools/Units/Create Unit Prefab` 메뉴로 열리는 `EditorWindow`
- 입력 필드:
  - **템플릿 프리팹** (GameObject): 기존 UnitPrefab.prefab 또는 동일 구조의 프리팹
  - **UnitData** (UnitData): 새 프리팹에 연결할 UnitData (또는 여러 개 선택 가능)
  - **저장 폴더** (DefaultAsset): 프리팹 저장 경로
  - **Cost 하위 폴더 사용** (bool): `Cost01`, `Cost02` 등 자동 생성/사용

### 기능 2: 프리팹 생성 로직

1. 템플릿 프리팹을 복제 (PrefabUtility.InstantiatePrefab)
2. 루트 GameObject에서 `Unit` 컴포넌트 찾기
3. `Unit.data`에 선택한 UnitData 할당
4. `Unit.bulletPrefab`이 비어 있으면 `UnitData.projectilePrefab`으로 설정
5. UnitData.unitPrefab에 생성된 프리팹 참조 저장 (에디터 유틸로 dirty/저장)
6. `CostXX` 폴더가 없으면 생성 후 해당 경로에 프리팹 저장

### 기능 3: 배치 모드 (선택)

- 여러 UnitData를 한 번에 선택해 **일괄 프리팹 생성** 지원
- Project 창에서 UnitData 에셋을 여러 개 선택 → 우클릭 컨텍스트 메뉴 `Create Unit Prefab from UnitData` → 창에서 저장 폴더만 지정 후 생성

### 기능 4: 유효성 검사

- 템플릿에 Unit 컴포넌트가 있는지 확인
- UnitData가 null인지 확인
- 저장 경로 유효성 검사 (폴더가 실제로 존재하는지)
- UnitData.unitPrefab이 이미 연결되어 있으면 경고 표시 (덮어쓸지 물어보기)

---

## 기술적 제약·참고사항

1. **#if UNITY_EDITOR**  
   에디터 스크립트 전체를 `#if UNITY_EDITOR`로 감싸기.

2. **PrefabUtility 사용**
   - `PrefabUtility.InstantiatePrefab`으로 프리팹 인스턴스 생성
   - `PrefabUtility.SaveAsPrefabAsset`로 저장

3. **SerializedObject / EditorUtility**  
   UnitData의 `unitPrefab` 필드 수정 시 `SerializedObject` 또는 `Undo.RecordObject` + `EditorUtility.SetDirty`로 변경 저장.

4. **SPUM 비주얼**  
   초기 버전에서는 템플릿 그대로 복제. 이후 필요하면 "비주얼만 교체" 옵션 추가 가능.

5. **파일명 규칙**  
   UnitData 이름 또는 `Unit_{cost}_{name}` 패턴으로 프리팹 파일명 생성. 기존 `Unit_1_Warrior` 등과 맞추기.

---

## 기대하는 결과

- `CreateUnitPrefabWindow.cs` (또는 유사한 이름) 파일 추가
- 기존 `CreateUnitDataWindow`와 분리하되, 필요 시 UnitData 생성 후 바로 프리팹 생성 버튼으로 연동 가능하게 설계
- 유닛을 많이 추가할 때 씬에서 하나씩 만드는 반복 작업을 크게 줄일 수 있는 워크플로우 확보

---

## 참고할 파일

- [Unit.cs](Assets/_Project/01_Scripts/Systems/Units/Unit.cs) – Unit 컴포넌트 구조
- [UnitData.cs](Assets/_Project/01_Scripts/Systems/Units/UnitData.cs) – UnitData 필드
- [CreateUnitDataWindow.cs](Assets/_Project/01_Scripts/Editor/CreateUnitDataWindow.cs) – 기존 에디터 UI/로직 패턴
