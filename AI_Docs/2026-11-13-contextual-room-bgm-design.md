# 방·상황별 BGM 및 구간 루프 설계

## 의도

기획/아트가 `SoundDatabase`에서 BGM 오디오를 직접 연결하고, 코드 수정 없이 방 종류와 전투 상황에 따라 BGM을 전환할 수 있게 한다. 상태에 메인 오디오가 연결되지 않았다면 그 상태는 사용하지 않으며 현재 재생 중인 BGM을 유지한다.

## 목표

- Start, Battle, Boss, Event, Shop, Rest, Special, Tutorial 방별 기본 BGM 상태를 인스펙터에서 매핑한다.
- 전멸/패배, 보스 등장 연출, 파티 저체력 조건을 우선순위로 처리한다.
- 높은 우선순위 조건이 끝나면 다음 유효 조건 또는 방 기본 BGM으로 복귀한다.
- BGM 클립별로 전체 루프 또는 인트로 이후 `[Loop Start, Loop End)` 구간 루프를 지원한다.
- 메인 클립이 없는 `BgmState` 요청은 무시하고 현재 BGM을 끊지 않는다.

## 비목표

- 오디오 클립 자체 연결과 음악 밸런싱은 자동화하지 않는다.
- 전투 결과나 체력 계산을 BGM 시스템에서 수행하지 않는다.
- 네트워크 메시지 또는 동기화 상태를 추가하지 않는다.

## 검토한 방식

1. `BattleSceneController`에 모든 분기를 직접 추가: 구현은 짧지만 방/조건이 늘어날수록 거대 컨트롤러와 enum switch가 커진다.
2. 방 프리팹마다 BGM 컴포넌트 배치: 방 기본음에는 편하지만 저체력·보스 연출·패배 우선순위와 복귀를 여러 객체가 경쟁하게 된다.
3. **선택: 중앙 `BattleBgmDirector` + `SoundDatabase` 상태 조회**: 방 매핑과 조건 우선순위를 한 곳에서 결정하고 `AudioManager`는 재생만 담당한다. 새 방 매핑은 인스펙터 배열로 추가하며, 미연결 상태 fallback을 일관되게 보장한다.

## 데이터

### BgmState

기존 상태를 유지하고 상황용 상태를 추가한다.

- `BattleLowHealth`
- `BossIntro`
- `BattleDefeat`

방 기본 상태는 `BattleMain`, `Battle1~3`, `Event1~2`, `Shop`, `Rest` 등 기존 상태를 자유롭게 매핑한다.

### BgmClipData

- `loop`: 전체 또는 구간 반복 여부
- `useLoopRegion`: 구간 루프 사용 여부
- `loopStartSeconds`: 인트로가 끝나고 반복이 시작되는 위치
- `loopEndSeconds`: 반복 구간 끝 위치. 0 이하이면 클립 끝을 사용한다.

유효 구간은 `0 <= start < end <= clip.length`이다. 유효하지 않으면 기존 전체 루프로 안전하게 대체한다.

## 런타임 흐름

`BattleBgmDirector`는 현재 방 타입과 조건 플래그를 보관하고 아래 순서로 첫 번째 재생 가능한 상태를 선택한다.

1. 전멸/패배
2. 보스 등장 연출
3. 파티 저체력
4. 현재 방 기본 BGM

`AudioManager.IsBgmConfigured(state)`가 false인 후보는 건너뛴다. 유효 후보가 하나도 없으면 `PlayBgm`이나 `StopBgm`을 호출하지 않아 기존 음악이 계속 재생된다.

저체력은 살아 있는 파티원 중 한 명이라도 `CurrentHP / MaxHP <= threshold`이면 활성화한다. 기본 threshold는 30%이며 인스펙터에서 조정한다. 플레이어 피해 이벤트와 턴 반환 시 다시 평가하여 회복 후 기본/보스 BGM으로 복귀한다.

방 진입은 `BattleSceneController`가 선택된 노드 타입을 디렉터에 전달한다. 보스 인트로는 기존 `BattleRoomIntroStarted/Completed` 이벤트와 현재 방 타입을 함께 사용한다. 패배는 `BattleResultChecker`의 명시적 패배 이벤트를 사용한다.

## 구간 루프

`AudioManager`는 재생 중인 각 BGM 레이어와 해당 `BgmClipData`를 함께 추적한다. 구간 끝 sample에 도달하면 초과 sample을 보존하여 loop start로 이동한다. 메인 BGM과 ambience layer 모두 동일 규칙을 사용한다.

## 멀티플레이 경계

BGM은 각 클라이언트의 로컬 프레젠테이션이다. 방 타입, 체력 스냅샷, 보스 인트로/패배 결과처럼 이미 동기화되는 결과만 읽고 전투 상태를 변경하지 않는다.

## 검증

- 미연결 조건 상태가 현재 BGM을 덮어쓰지 않는 우선순위 테스트
- 조건 종료 후 방 기본 상태 복귀 테스트
- 저체력 임계값 및 사망자 제외 테스트
- 구간 루프 sample 계산과 잘못된 구간 fallback 테스트
- 런타임/에디터 C# 프로젝트 컴파일 및 씬 YAML 참조 검사

