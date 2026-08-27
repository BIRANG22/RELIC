# 이벤트 주사위 및 옵션 패널 정상화 계획

## 조사 결과

- `EventRoomController.TryBeginDiceRollChoice()`가 롤 시작 직전에 `HideDiceRollPresenterImmediate()`를 호출한다.
- 현재 숨김 함수는 표시 오브젝트 비활성화와 컨트롤러의 진행 중 롤 코루틴 중지를 함께 수행한다.
- `EventDiceRollPresenter.Play()`는 활성화 전 참조/레이아웃 갱신이 약하고, 비활성 부모나 런타임 재참조 상황에서 표시 보장이 부족하다.
- `Option.prefab` 일부 한글 TMP 텍스트가 `m_fontAsset: 0`이거나 LiberationSans material을 참조한다.
- `OptionTMPFontEnforcer`는 폰트만 교체하고 material은 교체하지 않는다.
- `Option.prefab` 버튼 이벤트는 `ShowSound`, `ShowLanguage`, `ShowResolution`을 호출하지만 `OptionPanelUI`에 해당 공개 메서드가 없다.

## 권장 설계

- 주사위 롤 시작 전에는 기존 presenter 표시만 숨기고 컨트롤러 롤 코루틴은 취소하지 않는 전용 경로를 둔다.
- 방 이탈, 비활성화, 다음 버튼 이동처럼 실제 취소가 필요한 경우에만 컨트롤러 롤 코루틴을 중지한다.
- `EventDiceRollPresenter.Play()`는 자신과 부모 표시 상태, 참조 재검색, 레이아웃 갱신을 실행 순서상 더 강하게 보장한다.
- 옵션 패널은 기존 프리팹 버튼 이벤트와 맞도록 `ShowSound`, `ShowLanguage`, `ShowResolution`, `ShowControl` 공개 메서드를 복구한다.
- `OptionTMPFontEnforcer`는 TMP 폰트와 함께 `fontSharedMaterial`도 대상 폰트의 material로 교체한다.
- `Option.prefab`의 한글 텍스트는 한국어 지원 TMP 폰트(`TMP_Font_KR`)와 해당 material을 명시 참조하도록 보정한다.

## 검증 계획

- `Assets/Tests/EditMode~/`에 주사위 표시 시작 시 컨트롤러 코루틴이 즉시 취소되지 않는 테스트를 추가한다.
- 비활성 presenter에서 직접 `StartCoroutine`을 호출하지 않고 controller/host coroutine으로 주사위 연출을 진행하는 테스트를 추가한다.
- 옵션 패널 공개 메서드, 옵션 프리팹 TMP 폰트/material 상태, 폰트 enforcer material 교체 테스트를 추가한다.
- `MenuPanel.prefab`의 한글 TMP 텍스트 폰트/material 상태 테스트를 추가한다.
- 사용자 확인 결과 `TMP_Font_KR` 강제 변경은 의도와 다르므로 원래 옵션/메뉴 폰트는 유지하고, null 폰트 및 폰트-material 불일치만 원래 폰트 기준으로 복구한다.
- 주사위 프리팹의 기본 이미지 sprite가 비어 있어 애니메이션이 보이지 않으면 사운드만 날 수 있으므로, 롤 시작 즉시 결과 face sprite를 먼저 적용해 표시를 보장한다.
- 추가 확인 결과 첫 주사위 롤에서 막 활성화된 Animator에 `Play`/`Update`가 즉시 호출되어 inactive/state 경고가 발생한다. 주사위 sprite를 먼저 표시하고 한 프레임 뒤 active/controller/state 확인 후 Animator를 재생한다.
- MenuPanel 텍스트는 원래 폰트/material/색상 값이 유지되어 있으므로 폰트 교체가 아니라 TMP mesh/material 갱신 타이밍 문제로 처리한다. 패널 열림 및 런타임 텍스트 변경 시 TMP mesh/canvas refresh를 강제한다.
- Unity batchmode 테스트는 프로젝트 규칙상 실행하지 않고, MSBuild로 컴파일 검증한다.
