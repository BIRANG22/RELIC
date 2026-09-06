# Bootstrap DebugBattle 버튼 설계

## 목표

Bootstrap 씬은 기존 시간 설정으로 자동으로 Title 씬으로 전환한다. Title 씬 우측 하단의 작은 디버그 버튼을 누르면 기본 캐릭터 데이터를 구성하고 DebugBattle 씬으로 이동한다.

## 동작

- Bootstrap 초기화, 페이드 시간, Title 자동 이동은 기존 흐름을 그대로 유지한다.
- Title 화면 우측 하단에 Unity 기본 흰색 사각형 이미지 기반의 작은 `DebugBattle` 버튼을 둔다.
- 버튼을 누르면 중복 입력을 차단하고 DebugBattle 씬으로 이동한다.
- `CharacterDatabase`에서 `IsDefaultProvided`가 참인 캐릭터를 최대 3명 선택한다.
- 각 캐릭터의 HP, 코스트, 고유/능력/공용 스킬, 시작 유물을 마스터 데이터 기준으로 생성하고 파티 슬롯 및 시작 그리드에 배치한다.
- 이미 저장 데이터로 파티가 구성되어 있더라도 디버그 진입 시에는 재현 가능한 기본 디버그 파티로 교체한다.
- 데이터 구성에 실패하면 DebugBattle로 이동하지 않고 오류를 기록한다.
- DebugBattle 씬을 Build Settings에 등록한다.

## 구조

- `TitleDebugBattleLauncher`: Title 화면의 버튼 요청을 받아 디버그 파티 구성 후 DebugBattle을 로드한다.
- 별도 디버그 데이터 구성 클래스: 마스터 데이터를 이용해 런타임 캐릭터/파티 데이터를 만든다.
- Title UI: 전투 상태를 직접 변경하지 않고 디버그 진입 요청만 전달한다.

이 구조는 UI가 전투 결과를 계산하지 않으며, 파티 구성은 캐릭터 ID와 그리드 인덱스로 저장되므로 기존 멀티플레이 경계 규칙을 유지한다.
