# 녹턴 포털 그리드 효과 미리보기 구현 계획

> **For agentic workers:** 구현은 테스트 우선 순서로 현재 작업 공간에서 수행한다.

**Goal:** 녹턴 포털 목적지 표시를 `GridEffectSpriteDatabase` 기반 `Vfx_Gr_Mon_E_01_move` 프리팹으로 교체한다.

**Architecture:** 포털 표시는 그리드 효과 데이터 ID와 프리팹 DB를 재사용하지만 전투 상태에는 등록하지 않는 프레젠테이션 객체로 유지한다. 기존 예약 명령과 참조 카운트 수명주기를 그대로 사용한다.

**Tech Stack:** Unity 6, C#, ScriptableObject, NUnit EditMode, XLSX/CSV

## Global Constraints

- 테스트는 `Assets/Tests/EditMode~/`에 작성한다.
- 엑셀 원본과 런타임 CSV를 함께 갱신한다.
- 전투 상태 변경 없이 예약 결과를 표현만 한다.
- batchmode 테스트는 실행하지 않는다.

### Task 1: 회귀 테스트

**Files:**
- Modify: `Assets/Tests/EditMode~/BattleReservationSystemTests.cs` 또는 관련 포털 테스트 파일

- [ ] 포털 미리보기 ID가 DB에서 프리팹으로 해석되는 테스트를 추가한다.
- [ ] Controller 소스가 별도 Sprite 필드 대신 DB 프리팹 인스턴스를 보관하는 계약을 테스트한다.
- [ ] 수정 전 테스트 실패 원인을 확인한다.

### Task 2: 프리팹 기반 미리보기

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/TimeLine/PlayerSkillReservationController.cs`

- [ ] `GR_nocturn_portal_preview` 상수를 추가한다.
- [ ] 포털 엔트리가 `GameObject` 인스턴스와 참조 카운트를 보관하도록 변경한다.
- [ ] `GridEffectSpriteDatabase.TryGetPrefab`으로 프리팹을 생성하고 목적지 그리드에 배치한다.
- [ ] 숨김과 전체 초기화 시 인스턴스를 제거한다.

### Task 3: 데이터 등록

**Files:**
- Modify: `Assets/DB/GridEffectSpriteDatabase.asset`
- Modify: `Assets/ExcelSource/GameData.xlsx`
- Modify: `Assets/Resources/Data/GameDataRuntime.csv`

- [ ] 프리팹 GUID `ecd05d8207b1dc7479b902ef4afeb80f`를 DB에 연결한다.
- [ ] GridEffect 시트에 효과 없는 포털 미리보기 행을 추가한다.
- [ ] 같은 행을 런타임 CSV에 추가한다.

### Task 4: 검증

- [ ] 엑셀 GridEffect 범위와 렌더 결과를 검사한다.
- [ ] DB ID, CSV ID, 코드 상수가 일치하는지 검사한다.
- [ ] 런타임 및 에디터 프로젝트를 컴파일한다.
- [ ] 변경 파일과 기존 변경사항 보존 여부를 확인한다.
