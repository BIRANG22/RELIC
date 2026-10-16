# Battle Dynamic Text Localization Implementation Plan

1. 스캐너가 보간 문자열을 놓치는 현재 동작과 필요한 번역 키 누락을 재현하는 EditMode 테스트를 추가한다.
2. `LocalizationProjectScanner`의 동적 출력 탐지를 보간 문자열 할당/반환까지 확장한다.
3. `BattleEffectInfoFeed`의 고정 한국어 및 GameData 원문 사용을 기존 로컬라이징 API로 교체한다.
4. `BattleRewardPanelUI`, `EventRoomController`, `RestRoomController`의 공유 버튼 및 획득 결과 문구를 키 기반으로 교체한다.
5. `Localization.xlsx`에 신규 형식/알림 키의 5개 언어 값을 추가하고 Unity String Table에 반영한다.
6. 관련 EditMode 테스트, 전체 테스트 가능한 범위, 런타임/에디터 C# 빌드를 검증한다.
7. 실제 보상 슬롯 소유자인 `BattleRewardSlotUI.Refresh()`의 두 하드코딩 분기를 슬롯 전용 키로 교체한다.
8. 엑셀과 5개 Unity String Table에 같은 키·ID를 반영하고 회귀 테스트 및 컴파일을 다시 검증한다.
