# 아라벨라 최초 등장 애니메이션 및 대사 설계

## 목적

보스방 최초 진입 시 아라벨라가 스폰되면 새 `appear01`, `appear02` 애니메이션을 순서대로 재생하고 `Idle`로 전환한다. 등장 애니메이션 시작과 동시에 이미지와 같은 검은 대사 패널을 표시하며, 등장 연출이 끝날 때까지 전투 입력과 초기 몬스터 행동 계획을 잠근다.

## 조사 결과

- 기존 흐름은 플레이어 입장 뒤 배경 프리팹의 보스 등장 연출을 재생하고, 완료 뒤 아라벨라를 스폰했다.
- 최근 배경 등장 오브젝트가 제거되고 `Arabella/Clip/appear01.anim`, `appear02.anim`이 갱신됐지만 전투 Animator Controller에는 해당 상태가 없다.
- 현재 `CompleteDeferredMonsterSpawn()`이 아라벨라를 생성한 직후 `BattleSceneController.IsBattleRoomIntroPlaying`을 해제하므로 별도 등장 연출이 없으면 Idle부터 표시된다.
- 초기 행동 계획과 입력 활성화 루틴은 `IsBattleRoomIntroPlaying`이 해제될 때까지 이미 대기하므로, 이 게이트를 등장 연출 완료까지 유지하면 전투 로직과 표현을 분리할 수 있다.

## 선택한 접근 방식

### 아라벨라 프리팹 소유 등장 Presenter

`ArabellaSpawnPresentation`을 아라벨라 프리팹에 배치한다. Presenter는 프리팹의 Animator, `BattleUnitAnimator`, 두 등장 클립, 프리팹에 배치된 대사 패널과 TMP 텍스트를 명시적으로 참조한다.

흐름은 다음과 같다.

1. 기존 보스방 게이트가 플레이어 입장을 완료한다.
2. `BattleRoomLoader`가 아라벨라를 실제 스폰한다.
3. `ArabellaSpawnPresentation`이 같은 프레임에 대사 패널과 `Appear01`을 시작한다.
4. `appear01` 길이만큼 첫 문장을 표시한다.
5. `Appear02`와 두 번째 문장을 동시에 시작한다.
6. `appear02`가 끝나면 패널을 숨기고 `BattleUnitAnimator.PlayIdle()`을 호출한다.
7. 보스 인트로 게이트를 해제하여 초기 몬스터 계획과 플레이어 입력을 진행한다.

Animator Controller에는 `Appear01`, `Appear02` 상태를 추가하되 자동 전이는 두지 않는다. Presenter가 두 상태와 대사 전환 시점을 함께 소유하여 클립과 문장이 정확히 맞도록 한다.

## 대사 UI

- 대사 패널은 런타임 생성하지 않고 `Arabella.prefab`에 비활성 자식으로 배치한다.
- 검은 배경, 밝은 이중 테두리, 흰색 TMP 문구로 이미지의 형태를 재현한다.
- 첫 문장: `들리시나요? 이토록 아름다운 기도가...`
- 두 번째 문장: `고통은 잠시뿐입니다. 구원은 영원하니까요`
- 문구는 안정적인 로컬라이징 키를 통해 조회하고 한국어 원문을 fallback으로 유지한다.
- UI는 전투 결과를 계산하지 않으며 Presenter가 받은 표시 결과만 재생한다.

## 예외 처리

- 이어하기로 이미 시작된 보스 전투는 기존 `BossIntroStarted` 정책에 따라 등장 연출을 반복하지 않는다.
- Presenter, Animator, 클립 또는 대사 UI 참조가 없으면 경고 후 인트로를 안전하게 종료하여 전투가 멈추지 않게 한다.
- 아라벨라가 아닌 보스 또는 일반 몬스터의 스폰 흐름은 변경하지 않는다.
- Presenter는 인스턴스마다 한 번만 재생한다.

## 검증

- EditMode 테스트로 프리팹 참조, Animator 상태와 클립 연결, 대사 문구/키, 스폰 게이트 호출 순서를 검사한다.
- C# 런타임 및 테스트 어셈블리를 컴파일한다.
- 프리팹·Controller YAML과 대사 UI 직렬화 참조를 검사한다.
- Unity 에디터 직접 실행과 화면 확인은 프로젝트 규칙에 따라 미검증 항목으로 보고한다.

## 멀티플레이 경계

전투 상태 변경, 보스 RuntimeId, AI 계획, 랜덤 결과는 변경하지 않는다. 모든 클라이언트가 기존 보스 인트로 게이트를 로컬 표현 완료 시점까지 유지하는 프레젠테이션 변경이며 Command, State Change, Result/Event 및 네트워크 스냅샷 형식에는 영향이 없다.
