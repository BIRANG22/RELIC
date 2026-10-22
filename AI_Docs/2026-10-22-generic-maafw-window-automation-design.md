# 범용 창 선택형 MaaFramework 자동화 실행기 설계

## 목적

사용자가 테스트 권한을 가진 Windows 프로그램을 실행 시점에 선택하고, 프로젝트별 MaaFramework Pipeline과 PNG 리소스를 사용해 지정 화면까지 반복 진행하는 독립 실행 프로그램을 제공한다. RELIC를 첫 검증 대상으로 삼되 프로그램 코드에는 RELIC 전용 흐름을 넣지 않는다.

## 성공 기준

- 독립 실행 프로그램에서 현재 열려 있는 최상위 창을 조회하고 하나를 선택할 수 있다.
- 프로젝트 프로필 폴더를 선택하면 설정과 MaaFramework 리소스를 검증한다.
- 선택한 창의 HWND로 MaaFramework Win32 Controller를 연결한다.
- Pipeline 진입 노드를 지정 횟수만큼 실행한다.
- 목표 노드 완료, 실패, 창 종료, 창 크기 변경, 사용자 중지를 구분해 기록한다.
- `Ctrl+Shift+F12` 긴급 중지와 UI 중지를 제공한다.
- 프로젝트별 PNG와 Pipeline JSON만 교체해 다른 자체 프로젝트에 재사용할 수 있다.
- 범용 코어는 MaaFramework 및 Windows UI 없이 자동 테스트할 수 있다.

## 범위

### 포함

- Windows 10 이상, x64
- .NET 8 WinForms 실행기
- MaaFramework C# Binding 및 Win32 Controller
- 실행 시 열린 창 선택
- 프로젝트 프로필 검색 및 검증
- Pipeline v2 리소스 로드
- 순차 반복 실행
- 상태 및 로그 표시
- 실패 시 마지막 오류 정보 기록
- 긴급 중지, 창 유효성 및 크기 변경 검사
- RELIC 예제 프로필과 Pipeline 골격

### 제외

- 자동화 흐름 GUI 편집기
- PNG 캡처·편집기
- 탐지 회피 또는 사람 행동 위장
- 프로세스 메모리 읽기·변조
- 다중 창 동시 실행
- 최소화 창 강제 복구
- 관리자 권한 자동 상승
- 백그라운드 호환성 보장
- 사용자가 테스트 권한을 갖지 않은 프로그램용 리소스

## 아키텍처

### `RelicAutomation.Core`

MaaFramework나 WinForms에 의존하지 않는 순수 .NET 라이브러리다. 프로필 모델과 검증, 반복 실행 상태, 종료 사유, 창 스냅샷 및 추상 실행 인터페이스를 제공한다.

### `RelicAutomation.App`

WinForms UI와 Windows 창 조회를 제공한다. 사용자가 창과 프로필을 명시적으로 선택해야 실행할 수 있다. 선택 HWND, 프로필 리소스 경로와 실행 설정을 Maa 어댑터에 전달한다.

### `MaaWindowAutomationSession`

MaaFramework C# Binding을 감싸는 단일 어댑터다. Resource, Win32 Controller, Tasker 생명주기를 소유하며 연결, Pipeline 1회 실행, 중지, 해제를 제공한다. Binding API 변경은 이 파일 내부로 제한한다.

### 프로젝트 프로필

각 프로필은 `profile.json`, `pipeline/`, `image/`를 가진다. `profile.json`은 진입 노드, 목표 노드, 반복 횟수, 기준 창 크기와 안전 설정을 정의한다. 실제 화면 흐름은 Pipeline JSON이 담당한다.

## 데이터 흐름

1. 실행기가 현재 열린 최상위 창 목록을 조회한다.
2. 사용자가 자동화할 창과 프로젝트 프로필을 선택한다.
3. Profile Validator가 경로, 진입 노드, 반복 값과 기준 크기를 검사한다.
4. 실행기가 선택 창의 현재 크기와 프로필 기준을 비교한다.
5. Maa 어댑터가 Resource와 Win32 Controller를 만들고 Tasker에 바인딩한다.
6. 반복 실행기가 Pipeline 진입 노드를 한 번 실행하고 결과를 기록한다.
7. 목표 노드 완료 후 다음 반복을 시작하거나 지정 횟수에서 종료한다.
8. 창 종료·크기 변경·Task 실패·긴급 중지 시 추가 입력 없이 정지한다.

## 프로필 형식

```json
{
  "name": "RELIC Sample",
  "entryNode": "Start",
  "targetNode": "TargetReached",
  "repeatCount": 10,
  "expectedClientWidth": 1280,
  "expectedClientHeight": 720,
  "stopWhenWindowSizeChanges": true,
  "resourceDirectory": "resource"
}
```

## 안전 및 오류 처리

- 사용자가 선택하지 않은 HWND에는 연결하지 않는다.
- 실행 전에 선택 창의 제목, 프로세스, 클라이언트 크기를 다시 표시한다.
- HWND가 사라지거나 다른 창으로 재사용되면 정지한다.
- 기준 크기와 실제 크기가 다르면 기본적으로 실행하지 않는다.
- 한 번에 하나의 자동화 세션만 허용한다.
- 중지 요청 시 Pipeline 중단 후 Controller를 inactive 상태로 전환한다.
- 알 수 없는 화면 처리는 Pipeline timeout/on_error가 StopTask로 귀결되게 한다.
- 모든 실행은 최대 반복 횟수를 가져야 하며 무제한 반복을 허용하지 않는다.

## 테스트

- 유효 및 잘못된 프로필 검증
- 상대 리소스 경로 정규화와 프로필 루트 이탈 방지
- 반복 횟수 0 이하 및 상한 초과 거부
- 한 회 성공, 중간 실패, 사용자 중지, 창 소실, 크기 변경 종료
- 동일 세션 중복 시작 거부
- Maa 어댑터는 실제 창을 대상으로 수동 통합 검증

## 멀티플레이 영향

게임 런타임 코드와 전투 상태를 수정하지 않는 외부 QA 도구다. 화면과 입력만 사용하는 블랙박스 테스트이므로 멀티플레이 동기화 구조에는 직접 영향이 없다.
