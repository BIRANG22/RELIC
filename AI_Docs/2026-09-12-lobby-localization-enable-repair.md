# 로비 현지화 활성화 누락 복구

## 원인

1. `LobbyWorldObjectHoverName`가 `ObjectName`을 동적 소유 텍스트로 처리하면서 기존 현지화 컴포넌트를 제거하고, 하드코딩 한국어를 직접 대입한다.
2. `Lobby.unity`와 일부 프리팹의 정적 `LocalizeStringEvent` 및 `LocalizedTMPText`가 비활성화되어 있다.
3. 침식 선택 슬롯의 이름·효과는 런타임 UI가 데이터 원문을 직접 대입해 정적 로컬라이저의 결과를 덮어쓴다.

## 설계

- 월드 오브젝트 이름은 동적 텍스트 소유를 유지하되, 타입별 안정 키를 `GameLocalization.Get`으로 해석한다.
- 침식 슬롯은 `GameDataLocalization` 기반 값으로 갱신해 로케일 변경 및 패널 재개방에도 데이터 현지화 결과를 표시한다.
- 현지화 편집 잠금 도구로 Lobby 씬과 대상 프리팹의 정적 로컬라이저를 활성화하고, EditMode 검증이 비활성 컴포넌트 재발을 감지한다.
- 정적 텍스트와 런타임 데이터 텍스트의 소유권을 분리하며, 후자는 `LocalizeStringEvent`가 아닌 코드의 키 기반 조회만 사용한다.

## 검증

- 월드 오브젝트 타입-키 매핑 EditMode 테스트
- Lobby 및 ErosionSlot/Equip 프리팹의 대상 로컬라이저 활성 상태 검사
- 기존 현지화 관련 EditMode 테스트 실행
# DialogueText 후속 수정

LobbyTutorialController의 씬 직렬화 배열이 코드 기본값을 덮어쓴 한국어 원문 상태여서, 컨트롤러가 원문을 키로 조회하고 있었다. introDialogue와 firstExpeditionDialogue를 각각 tutorial.intro.* 및 tutorial.first_expedition.* 키로 복구했다.

DialogueText는 컨트롤러가 대사 진행 중 직접 작성하는 동적 TMP이므로 정적 LocalizedTMPText를 제거하고 LocalizationIgnore를 부착했다. 한국어 Text String Table에는 실제 씬 대사 10개와 화자 키를 추가했으며, 이후 Tools/Localization/Sync Tutorial Localization 실행 시에도 같은 한국어 원문이 유지되도록 동기화 원본도 맞췄다.

# Bootstrap Intro 후속 수정

Bootstrap의 DontDestroyOnLoad 인트로는 IntroSequenceController가 씬의 introLines 배열을 Text에 직접 대입하고 있었다. 원문 배열을 intro.story.01~09 키로 교체하고 GetLine에서 GameLocalization을 조회하도록 변경했다. 동적 출력 Text에는 LocalizationIgnore를 붙여 전체검사/자동 바인딩이 정적 LocalizedTMPText를 다시 붙이지 못하게 했다.

한국어 Text String Table과 동기화 명령에 9개 원문을 등록했다. 다른 언어 번역은 Excel의 Text 시트에서 intro.story 키 행을 채운 뒤 Tools/Localization/Sync Intro Story Localization으로 반영한다.
