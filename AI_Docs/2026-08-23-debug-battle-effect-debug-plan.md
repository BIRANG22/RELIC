# DebugBattle 배틀효과디버그 구현 계획

## 작업 순서

1. 기존 테스트를 새 슬롯 적용 정책 기준으로 갱신한다.
2. `BattleEffectDebugTool`에 순수 슬롯 조작 API를 추가한다.
3. `BattleEffectDebugWindow`를 엑셀 기반 DB 선택 UI로 재구성한다.
4. 스킬 직접 시전 테스트 UI와 하드코딩 프리셋 UI를 제거한다.
5. `DebugBattle.unity`의 BattleSlot RectTransform을 Battle 씬 기준으로 맞춘다.
6. 컴파일 검증과 git diff 확인을 수행한다.

## 검증 계획

- EditMode 테스트 파일에서 슬롯 조작, 스탯 조절, 검색 필터링을 검증한다.
- Unity batchmode 테스트는 프로젝트 규칙에 따라 실행하지 않는다.
- MSBuild로 런타임/에디터 컴파일을 확인한다.
