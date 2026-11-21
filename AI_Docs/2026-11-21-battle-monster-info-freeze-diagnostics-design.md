# 몬스터 정보 패널 반복 개폐 정지 진단 설계

## 목적

몬스터 정보 패널 반복 개폐 후 발생하는 전투 정지의 실제 상태 경계를 한 번의 재현 로그로 식별한다. 기존 블러 복제 가설은 Battle 씬의 MonsterInfoPanel/Canvas에 `UIBlurBackground`가 연결되어 있지 않아 원인에서 제외한다.

## 확인된 증거

- 최근 Editor.log에 `Monster:RuntimeMonster_0001:S_Monster_02`가 8초 제한을 넘긴 `BattleActionRunner Action Timeout`이 기록되어 있다.
- 전투 행동 제한 시간은 unscaled 시간으로 증가하지만 내부 행동에는 scaled 대기가 존재한다.
- ESC 메뉴는 `Time.timeScale`을 0으로 만들 수 있다.
- 우클릭은 몬스터 패널, 타임라인, 스킬 예약, 유물 타기팅 등 여러 소비자가 직접 읽는다.
- 패널 닫기 후 캐릭터 선택 복원 코루틴은 추적되지 않은 채 다음 프레임 실행된다.

## 채택 설계

상태 변경 시점에만 `[BattleFreezeDiag]` 로그를 남긴다. 모든 로그는 frame, unscaled time, timeScale, 메뉴 열림 여부, 전투 실행/입력 상태, 선택 몬스터 RuntimeId를 공통 필드로 포함한다.

계측 지점:

1. 몬스터 정보 선택 이벤트와 패널 Show/Close/Hide
2. ESC·우클릭이 몬스터 패널에 의해 소비된 순간
3. 닫기 후 캐릭터 복원 코루틴 예약·실행·중단
4. 메뉴 pause 적용·해제
5. 타임라인·스킬 예약·유물 타기팅이 우클릭을 처리하는 순간
6. 전투 실행 시작과 `BattleActionRunner` 타임아웃

로그는 진단 전용이며 전투 상태나 입력 판정을 변경하지 않는다. 프레임별 Update 로그는 추가하지 않는다.

## 검증

- 공통 로그 포맷이 핵심 상태 필드를 빠뜨리지 않는지 EditMode 테스트로 검증한다.
- 런타임 및 Editor 어셈블리 컴파일을 검증한다.
- 실제 원인 확정은 사용자의 동일 재현 후 Editor.log 분석으로 수행한다.

## 멀티플레이 경계

Command, State Change, Result/Event 및 네트워크 Snapshot을 변경하지 않고 로컬 진단 로그만 추가한다.
