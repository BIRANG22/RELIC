# 레드더스티움 전리품 표시명 구현 계획

1. `BattleRewardData.GetDisplayName()`의 레드더스티움 표시명 테스트를 추가한다.
2. `BattleRewardData.GetDisplayName()`에서 `BattleRewardType.Remnant`일 때 `xAmount`를 붙인다.
3. 기존 이름이 비어 있는 레드더스티움 보상은 `더스티움`을 기본값으로 사용한다.
4. 런타임/에디터 어셈블리 빌드와 diff 검사를 수행한다.
