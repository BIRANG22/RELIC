# 플레이어 표시 한국어 하드코딩 제거 설계

## 목표

런타임 스크립트와 씬·프리팹 인스펙터가 한국어 원문을 직접 소유하거나 TMP에 다시 쓰지 않게 하고, 모든 플레이어 표시 문구를 `Localization.xlsx`의 안정적인 키로 관리한다.

## 범위

- 런타임 C#의 `TMP_Text.text`, `SetText`, 경고·확인·인트로 UI에 전달되는 한국어 리터럴
- 플레이어 표시 목적으로 직렬화된 `*Text`, `*Message`, `*Title`, `*Label`, `*Description`, 표시 이름 배열
- `SettingWarningUI`와 이를 호출하는 로비 UI
- 사용자 지정 오브젝트 네 경로의 로컬라이제이션 소유권
- 재발 방지용 프로젝트 스캐너와 EditMode 테스트

주석, `Tooltip`, `Header`, `Debug.Log`, 에디터 전용 안내 문구, 오브젝트 검색 이름, 데이터 파싱 및 게임 규칙 판정에 쓰이는 한국어 토큰은 표시 문자열이 아니므로 범위에서 제외한다.

## 런타임 API

- 코드와 인스펙터는 한국어 원문 대신 키를 보관한다.
- 단일 문구는 `GameLocalization.Get(key)`를 사용한다.
- 치환 문구는 `GameLocalization.Format(key, args)`를 사용한다.
- 현재 언어 셀이 비어 있거나 키가 없으면 해당 언어의 `미번역` 표기를 사용하고 한국어로 폴백하지 않는다.
- `SettingWarningUI`는 원문 비교 및 원문 교체를 하지 않고 `ShowKey(key, args)`로 번역한 결과만 표시한다. 이미 번역된 외부 문자열을 표시하는 호환 API는 원문 재해석 없이 그대로 표시한다.

## TMP 소유권

- ID 기반 동적 텍스트: 안정적인 키를 코드가 `LocalizedTMPText.Configure`에 전달한다.
- 문구 기반 동적 텍스트: 기존 한국어 원문을 키로 해석해야 하는 호환 대상만 `DynamicLocalizedTMPText`를 사용한다.
- 정적 텍스트: `LocalizedTMPText`가 고정 키를 소유한다.
- 번역 제외: `LocalizationIgnore`가 붙은 계층은 자동 로컬라이제이션 대상에서 제외한다.

지정 경로의 목표 상태:

- `PositionPanel/BackgroundPanel/Mainicon/MainText`: ID 기반 동적 `LocalizedTMPText`
- `PartPanel/PlayButton/PlayText`: ID 기반 동적 `LocalizedTMPText`
- `CultureTankPanel/Reference/Name/NameText`: 정적 `LocalizedTMPText`
- `CultureTankPanel/Storage/Name/NameText`: 정적 `LocalizedTMPText`

## 데이터와 키

기존 키가 있으면 재사용한다. 새 키는 의미 기반으로 `warning.*`, `ui.*`, `battle.*`, `event.*` 네임스페이스에 추가한다. 동일 문구라도 의미가 다르면 별도 키를 사용한다. 엑셀과 Unity StringTable은 기존 동기화 도구를 통해 같은 키 집합을 유지한다.

## 검증

- 스캐너가 직접 출력 한국어와 플레이어 표시용 직렬화 한국어를 보고한다.
- 지정 경로의 컴포넌트 유형과 키 소유권을 EditMode 테스트로 검증한다.
- `SettingWarningUI` 키 기반 포맷과 미번역 정책을 테스트한다.
- C# 컴파일과 `git diff --check`를 통과해야 한다.

## 멀티플레이 경계

표시 문자열과 UI 컴포넌트만 변경한다. 전투 Command, 상태 변경, 결과 이벤트 및 네트워크 동기화 데이터에는 영향을 주지 않는다.
