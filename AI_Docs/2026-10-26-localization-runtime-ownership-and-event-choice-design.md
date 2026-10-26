# 로컬라이징 런타임 소유권 및 이벤트 선택지 수정 설계

## 목표

- 로비의 침식·공명 바로가기 라벨을 기존 로컬라이징 ID로 표시한다.
- 숫자 전용 BlueDustiumHUD 값을 정적 로컬라이징 대상에서 제외한다.
- 배틀 이벤트 선택지의 이름·설명·선택 불가 설명을 안정적인 이벤트 ID와 선택 순서 기반 키로 표시한다.
- 플레이어에게 노출되는 한국어 하드코딩을 로컬라이징 ID 기반으로 전환한다.

## 조사 결과

1. `Lobby.unity`의 `Icon_01/Text (TMP)`와 `Icon_02/Text (TMP)`는 한국어 TMP 원문만 있고 로컬라이저가 없다. 부모의 `LobbyPositionPanelShortcutUI`는 선택된 메인 라벨만 갱신한다.
2. `BlueDustiumHUD/Value`에는 숫자 출력과 무관한 몬스터 스킬 키의 `LocalizedTMPText`가 자동 연결되어 있다.
3. 게임데이터 이벤트 헤더는 `선택지 설명`이지만 `LocalizationProjectScanner`는 `선택지 내용`만 선택지 설명으로 분류한다. 이 때문에 일반 선택지 설명이 `choice_{order}_description` 키로 수집되지 않는다.
4. 배틀씬 선택지 TMP에는 정적 자동 로컬라이저가 붙어 있지만 실제 값은 `EventChoiceSlotUI`가 런타임에 관리한다. 두 소유자가 같은 TMP를 갱신한다.
5. `EventChoiceSlotUI`는 Event_02 일부 설명에서 로컬라이징 결과 대신 한국어 게임데이터 원문을 다시 대입한다.
6. 이벤트 결과·전투 자원 부족·스킬 툴팁 등에 플레이어 노출 가능성이 있는 한국어 리터럴이 남아 있다.

## 권장 구조

### 정적 라벨

침식·공명 라벨은 씬에 배치된 `LocalizedTMPText`가 각각 `ui.lobby.panel.erosion`, `ui.lobby.panel.resonance`를 소유한다. 버튼 선택 시 표시되는 메인 라벨은 기존 `LobbyPositionPanelShortcutUI`가 같은 ID를 사용한다.

### 동적 숫자와 이벤트 선택지

숫자와 게임데이터 기반 선택지는 런타임 표시 코드가 단독으로 소유한다. 해당 TMP에는 `LocalizationIgnore`를 두고 자동 로컬라이저를 제거하거나 비활성화한다. `LobbyBlueDustiumHudUI`와 `EventChoiceSlotUI`에서도 `LocalizedTMPText.ProtectDynamicOwnership`을 호출해 씬 설정이 다시 오염되어도 보호한다.

### 이벤트 키

`선택지 설명`과 기존 별칭 `선택지 내용`을 모두 `choice_description`으로 정규화한다. 런타임 키는 다음 형식으로 통일한다.

- `data.event.{event_id}.choice_{order}_name`
- `data.event.{event_id}.choice_{order}_description`
- `data.event.{event_id}.choice_{order}_disabled_description`

엑셀에 누락된 선택지 설명 행을 추가하고 영어·일본어를 채운 뒤 Unity String Table 에셋과 동기화한다.

### 하드코딩 경계

플레이어 UI에 직접 전달되는 한국어 문구만 ID로 이관한다. 주석, 로그, 디버그 전용 창, 게임데이터 파싱 토큰은 대상에서 제외한다. 숫자·이름 등 가변 값은 로컬라이징 템플릿의 자리표시자로 전달한다.

## 검증

- 스캐너가 `선택지 설명`을 올바른 키로 변환하는 EditMode 테스트
- 이벤트 선택지의 모든 플레이어 노출 필드가 엑셀과 String Table에 존재하는 정적 검증
- 로비 정적 라벨 및 동적 숫자/선택지 소유권에 대한 씬 YAML 회귀 테스트
- 플레이어 노출 코드의 한국어 직접 출력 탐지 테스트
- C# 프로젝트 컴파일 및 변경 파일 YAML/엑셀 구조 검사

## 멀티플레이 영향

전투 상태·명령·결과 계산은 변경하지 않는다. 이벤트 선택지의 표시 문자열과 결과 안내 텍스트만 ID 기반으로 바꾸므로 동기화 대상과 전투 결정성에는 영향이 없다.
