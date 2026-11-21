# 전투 실행 중 월드 클릭 차단 구현 계획

> **실행 방식:** 현재 작업 공간에서 직접 구현한다. 프로젝트 규칙에 따라 worktree와 커밋은 만들지 않는다.

**목표:** 전투 행동 실행 중 모든 전투방 월드 오브젝트 클릭을 차단하면서 호버는 유지한다.

**구조:** 순수 판정 API를 가진 중앙 게이트를 추가하고 각 월드 클릭 진입점에서 호출한다. 몬스터 정보 클릭에는 플레이어 입력 단계와 튜토리얼 조건을 추가로 AND 결합한다.

**기술:** Unity C#, NUnit EditMode 테스트

**설계 문서:** `AI_Docs/2026-11-21-battle-world-click-lock-design.md`

## 작업 1: 중앙 정책 회귀 테스트

**파일:**
- 생성: `Assets/Tests/EditMode~/BattleWorldClickGateTests.cs`
- 수정: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/BattleTurnExecutor.cs`

- [x] 실행 중 차단, 비실행 중 허용, 실행기 부재 허용 테스트를 먼저 작성한다.
- [x] 구현 전 테스트 컴파일 실패를 확인한다.
- [x] 최소 정책 API를 구현하고 테스트 컴파일을 확인한다.

## 작업 2: 월드 클릭 진입점 적용

**파일:**
- 수정: `Assets/Project/Scripts/Gameplay/Monster/MonsterUnit.cs`
- 수정: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Character/BattleCharacter.cs`
- 수정: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Grid/GridCell.cs`
- 수정: `Assets/Project/Scripts/Gameplay/Scene/Battle/BattleGridClickHandler.cs`

- [x] 각 클릭 진입점 첫 부분에서 중앙 실행 차단을 적용한다.
- [x] 몬스터 정보 클릭 정책이 실행기 입력 상태와 튜토리얼 허용을 모두 요구하도록 적용한다.
- [x] 호버 경로가 변경되지 않았는지 diff로 확인한다.

## 작업 3: 검증

- [ ] 회귀 테스트 어셈블리 컴파일을 확인한다.
- [ ] `Assembly-CSharp.csproj` 빌드를 확인한다.
- [ ] `git diff --check`와 대상 코드 검색을 수행한다.
- [ ] Unity 에디터 수동 재현은 미검증 항목으로 보고한다.

