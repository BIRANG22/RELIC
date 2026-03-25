# RELIC Unity 개발 컨벤션 초안

## 1. 프로젝트 기본 원칙

### 1-1. 프로젝트 개요

* 프로젝트명: **RELIC**
* 장르: **로그라이크 기반 전술 RPG**
* 핵심 특징:

  * 액션 예약 기반 턴제 전투
  * 판단과 관리 중심의 플레이
  * 플레이어는 전투원이 아닌 **Handler(관리자)** 역할
* 개발 인원: **3인**
* 엔진: **Unity 6**
* 버전 관리: **Git + GitHub**

---

## 2. 협업 역할 분리 원칙

### 2-1. 역할별 수정 범위

각 역할은 **자기 담당 폴더만 수정**하는 것을 원칙으로 한다.

#### 프로그래밍

* `Assets/_Project/Core`
* `Assets/_Project/Combat`
* `Assets/_Project/Run`
* `Assets/_Project/UI/Common`

#### 아트

* `Assets/Art`
* `Assets/_Project/Characters` 내 프리팹의 **Visual 계층**
* Animator / Mesh / Material / Texture / VFX 관련 리소스

#### 기획

* `Assets/_Project/Data`

### 2-2. 역할 간 작업 원칙

* 프로그래머는 **스크립트(.cs)** 중심으로 작업
* 아트는 **모델, 머티리얼, 텍스처, 애니메이션, 프리팹의 비주얼 요소** 중심으로 작업
* 기획은 **데이터 파일, 밸런스 수치, 테이블 데이터** 중심으로 작업
* 다른 역할의 작업 영역 수정이 필요하면 **사전 공유 후 진행**

---

## 3. 프로젝트 폴더 구조 규칙

## 3-1. 루트 폴더 구조

Assets/
├─ _Project/
│  ├─ Core/
│  ├─ Combat/
│  ├─ Run/
│  ├─ Characters/
│  ├─ Stages/
│  ├─ UI/
│  └─ Data/
├─ Art/
├─ Audio/
└─ Plugins/

## 3-2. 각 폴더 역할

* `_Project/`: 실제 게임 개발 자산 및 코드
* `Art/`: 외부 원본 에셋, 모델링 소스, 텍스처 원본 등
* `Audio/`: 사운드, BGM, SFX
* `Plugins/`: 외부 패키지, 플러그인

## 3-3. 스크립트 폴더 구조

현재 구조를 기준으로 다음처럼 정리한다.

Assets/_Project/Scripts/
├─ Core/
├─ Gameplay/
├─ Light/
├─ Managers/
├─ Events/
├─ Bootstrap/
├─ EventBus/
├─ GameManager/
├─ SaveSystem/
├─ Settings/
└─ Singleton/

### 권장 정리 방식

현재처럼 폴더를 기능 단위로 나누되, 아래 원칙을 유지한다.

* **Core**
  게임 전반에서 공통으로 쓰이는 추상화, 유틸, 베이스 클래스
* **Gameplay**
  실제 플레이 로직
* **Managers**
  전역 흐름 관리 객체
* **Events / EventBus**
  이벤트 정의 및 발행/구독 시스템
* **Bootstrap**
  게임 시작, 초기화, 의존성 연결
* **SaveSystem**
  저장/로드
* **Settings**
  환경설정, 옵션
* **Singleton**
  싱글톤 베이스 또는 관련 유틸
  → 가능하면 남용하지 않고 최소화

### 추가 권장 사항

`GameManager`를 별도 폴더로 둘지, `Managers` 안으로 합칠지는 통일하는 것이 좋다.
개인적으로는 아래처럼 정리하는 편이 더 깔끔하다.


Managers/
├─ GameManager.cs
├─ UIManager.cs
├─ SceneFlowManager.cs
└─ ...


즉, **Manager 종류는 Managers 폴더 아래로 통합**하는 것을 권장한다.

---

## 4. 씬(Scene) 관리 규칙

### 4-1. 기본 원칙

* 동시에 같은 씬을 수정하지 않는다.
* 각자 다른 씬을 작업할 경우, 서로의 씬은 건드리지 않는다.
* 씬 충돌 가능성이 있으면 먼저 공유한다.

### 4-2. 씬 작업 역할

* 프로그래머: 씬 내 스크립트 연결, 테스트용 오브젝트, 로직 검증
* 아트: 배치, 모델, 라이팅, 연출 관련 오브젝트
* 기획: 데이터 연결 및 밸런스 확인

### 4-3. 권장 사항

* 테스트용 씬은 별도로 분리
* 메인 씬 / 테스트 씬 / UI 씬 / 전투 씬 용도를 분명히 분리
* 공용 씬 수정 전에는 팀원에게 공유

예시:

Scenes/
├─ MainMenu
├─ Lobby
├─ Battle
├─ Test_Combat
├─ Test_UI
└─ Test_Character

---

## 5. Git 브랜치 규칙

## 5-1. 브랜치 구조

* `main` : 항상 실행 가능한 출시 기준 버전
* `develop` : 개발 통합 및 테스트 버전
* `feature/*` : 개인 작업 브랜치

## 5-2. 브랜치 운용 방식

1. `develop`에서 개인 작업 브랜치를 생성
2. 각자 작업 후 `develop`으로 병합
3. 통합 테스트 및 정리 완료 후 `main`으로 반영

예시:

feature/ui-inventory
feature/combat-turnsystem
feature/save-load

## 5-3. 브랜치 규칙 상세

* `main`

  * 출시 가능한 상태만 유지
  * 디버그 코드, 치트, 테스트 에셋 금지
* `develop`

  * 개발자 테스트용
  * 테스트 모델, 치트 기능, 디버그용 UI 허용
* `feature/*`

  * 개인 작업 상태 저장
  * 작업 단위 명확히 유지

---

## 6. C# 스크립트 작성 컨벤션

사용 중인 코드 스타일을 기준으로 RELIC용으로 통일하면 좋습니다.

---

## 6-1. 클래스 작성 원칙

* 클래스명은 **PascalCase**
* 파일명과 클래스명은 반드시 일치
* MonoBehaviour는 **한 파일에 한 클래스**
* 한 스크립트는 **하나의 책임만 가지도록 작성**

예시:

public class BoardGridBuilder : MonoBehaviour

---

## 6-2. 변수명 규칙

### 필드

* private 직렬화 필드는:

  * `[SerializeField] private` 사용
  * **camelCase**
* public 필드는 되도록 지양
* 외부 노출이 필요하면 **프로퍼티** 사용

예시:

[SerializeField] private RectTransform slotsRoot;
[SerializeField] private int columns = 6;

### 지역 변수

* **camelCase**

예시:

GameObject slotGO = new GameObject(...);
RectTransform slotRect = slotGO.GetComponent<RectTransform>();

### 상수

* **PascalCase** 또는 **UPPER_SNAKE_CASE**
* 팀 내 하나로 통일
  권장: `PascalCase`

예시:

private const int DefaultColumns = 6;

---

## 6-3. 접근 제한자 규칙

* 기본적으로 `private`
* 인스펙터 노출은 `[SerializeField] private`
* 외부 접근이 꼭 필요한 경우만 `public`
* setter가 불필요하면 읽기 전용 프로퍼티 사용

예시:

public int SlotCount => columns * rows;

---

## 6-4. 함수명 규칙

* 메서드명은 **PascalCase**
* 동사로 시작
* 역할이 드러나게 작성

좋은 예:

Build()
ClearChildren()
ApplyLayout()
AutoCollectSlots()

---

## 6-5. 메서드 정렬 순서

스크립트 내부 순서를 통일한다.

권장 순서:

1. `using`
2. 클래스 선언
3. 상수 / enum
4. SerializeField
5. private field
6. public property
7. Unity 생명주기 함수 (`Awake`, `Start`, `Update`)
8. public method
9. private method
10. editor / debug method

예시:

using UnityEngine;
using UnityEngine.UI;

public class BoardGridBuilder : MonoBehaviour
{
    [SerializeField] private RectTransform slotsRoot;
    [SerializeField] private int columns = 6;

    public void Build()
    {
    }

    private void ClearChildren()
    {
    }
}

---

## 6-6. 헤더와 인스펙터 정리 규칙

인스펙터에서 보기 쉽도록 관련 필드는 묶는다.

예시:

[Header("Layout")]
[SerializeField] private int columns = 6;
[SerializeField] private int rows = 2;
[SerializeField] private Vector2 slotSize = new Vector2(100f, 100f);

[Header("Sprites")]
[SerializeField] private Sprite defaultFrameSprite;
[SerializeField] private Sprite defaultIconSprite;

### 규칙

* 관련 값이 2개 이상이면 `[Header]` 사용
* 필요 시 `[Space]`, `[Tooltip]` 사용 가능
* 너무 많은 헤더 남발 금지

---

## 6-7. null 체크 규칙

참조형 필드는 사용 전에 null 체크를 우선한다.

예시:

if (slotsRoot == null)
{
    Debug.LogWarning("SlotsRoot is null.");
    return;
}

### 규칙

* 필수 참조 누락 시 빠르게 반환
* 경고 로그는 원인 파악 가능하게 작성
* `Awake` 또는 `OnValidate`에서 사전 검증 가능하면 적극 활용

---

## 6-8. GetComponent 사용 규칙

* `GetComponent` 반복 호출 최소화
* 자주 쓰는 컴포넌트는 캐싱
* 부모 탐색은 꼭 필요할 때만 사용

예시:

PerspectiveBoardController controller = GetComponent<PerspectiveBoardController>();
if (controller == null)
    controller = GetComponentInParent<PerspectiveBoardController>();

### 권장

가능하면 `Awake()`에서 캐싱:

private PerspectiveBoardController controller;

private void Awake()
{
    controller = GetComponent<PerspectiveBoardController>();
    if (controller == null)
        controller = GetComponentInParent<PerspectiveBoardController>();
}

---

## 6-9. 코드 스타일 규칙

* 중괄호는 줄바꿈 스타일 통일
* `if`, `for`, `foreach`는 항상 중괄호 사용 권장
* 한 줄이 너무 길어지면 적절히 개행
* 매직 넘버는 가능하면 상수화

예시:

for (int row = 0; row < rows; row++)
{
    for (int col = 0; col < columns; col++)
    {
    }
}

---

## 6-10. 주석 규칙

* 주석은 “무엇”보다 **왜**를 설명
* 당연한 코드는 주석 금지
* TODO/FIXME 태그 사용 가능

예시:

// UI 슬롯은 런타임/에디터 양쪽에서 재생성 가능해야 하므로
// 플레이 모드 여부에 따라 Destroy 방식 분기

---

## 6-11. 로그 규칙

* 로그는 목적이 분명할 때만 사용
* 로그 앞에 시스템 태그를 붙이는 것을 권장

예시:

Debug.LogWarning("[BoardGridBuilder] SlotsRoot is null.");
Debug.LogError("[SaveSystem] Save failed.");

---

## 6-12. ContextMenu / Editor 유틸 규칙

`[ContextMenu]`는 반복 작업 자동화용으로 적극 활용 가능

예시:

[ContextMenu("Build 6x2 Slots")]
public void Build()
{
}

### 규칙

* 에디터용 자동 생성 기능은 ContextMenu 허용
* 팀원이 눌렀을 때 위험한 기능이면 이름에 명시

  * 예: `Rebuild All Slots (Clear Existing)`

---

## 7. 추가 권장사항

* `[SerializeField] private` 사용
* null 체크 존재
* 인스펙터 그룹화 (`[Header]`)
* 로그에 클래스명 태그 붙이기
* 중복되는 RectTransform 세팅 코드 유틸화
* `GetComponent` 캐싱
* 생성 오브젝트 이름 포맷 통일
* `Build()`가 길어지면 하위 메서드로 분리


즉, **현재 스타일을 유지하되 중복 제거와 책임 분리만 강화**하면 충분히 좋은 컨벤션이 됩니다.

---

## 8. 이벤트 버스(EventBus) 사용 규칙

## 8-1. 목적

* 시스템 간 직접 참조를 줄인다
* UI / 전투 / 매니저 간 결합도를 낮춘다
* 상태 변화 전달을 중앙 이벤트 구조로 통일한다

## 8-2. 규칙

* 이벤트 이름은 **의도가 명확하게**
* 이벤트는 **과거형/발생형** 기준으로 작성 권장
* 이벤트 payload는 최소한으로 유지
* 구독 해제 누락 방지

예시:

public struct TurnStartedEvent
{
    public int TurnIndex;
}

public struct UnitSelectedEvent
{
    public int UnitId;
}

## 8-3. 네이밍 규칙

* 이벤트 타입명: `SomethingHappenedEvent`
* 발행 메서드: `Publish`
* 구독 메서드: `Subscribe`
* 구독 해제: `Unsubscribe`

예시:

EventBus.Publish(new TurnStartedEvent { TurnIndex = 1 });

## 8-4. 사용 원칙

* 한 시스템이 다른 시스템 내부 구현을 알 필요 없을 때 EventBus 사용
* 1:1 직접 의존이 더 자연스러운 경우 EventBus 남용 금지
* UI 갱신, 상태 변경 알림, 전투 단계 전환 알림에 적합
* 초기화 순서에 민감한 로직은 EventBus만 믿지 말고 Bootstrap에서 보장

---

## 9. Hierarchy 창 오브젝트 정리 규칙

말한 것처럼 **빈 오브젝트를 구분용으로 사용**하는 방식은 팀 협업에서 매우 좋습니다.
다만 규칙을 통일해야 합니다.

## 9-1. 기본 원칙

* 하이라키는 한눈에 구조가 보여야 한다
* 구분용 빈 오브젝트를 적극 사용 가능
* 단, 실제 로직 오브젝트와 구분되게 이름 규칙을 둔다

## 9-2. 구분용 오브젝트 네이밍

권장 방식:

[UI]
[Combat]
[Managers]

_ UI
_ Combat
_ Managers
_ Debug

## 9-3. 하위 오브젝트 정리 예시

BattleScene
├─ _ Systems
│  ├─ TurnSystem
│  ├─ CombatManager
│  └─ EventSystem
├─ _ Characters
│  ├─ PlayerParty
│  └─ Enemies
├─ _ UI
│  ├─ HUD
│  ├─ ActionQueuePanel
│  └─ ResultPopup
├─ _ Environment
└─ _ Debug

## 9-4. 규칙

* 구분용 빈 오브젝트는 **Transform reset**
* 로직 없는 정리용 오브젝트에는 불필요한 컴포넌트 추가 금지
* 실제 매니저 오브젝트와 단순 정리 오브젝트 이름을 구분

  * `_ Managers` : 정리용 부모
  * `GameManager` : 실제 동작 객체

---

## 10. 파일 네이밍 컨벤션

파일 종류를 한눈에 알 수 있게 접두사를 붙인다.

## 10-1. 공통 규칙

* 파일명은 영어 사용
* 공백 대신 PascalCase 또는 `_` 사용
* 약어 남발 금지
* 의미 없는 이름 금지 (`NewMaterial`, `Test1` 금지)

## 10-2. 권장 접두사 규칙

### 아트 리소스

* `M_` : Material
* `T_` : Texture
* `S_` : Sprite
* `SM_` : Static Mesh 또는 Model
* `SK_` : Skeletal/Character Mesh
* `A_` : Animation Clip
* `AC_` : Animator Controller
* `VFX_` : Visual Effect
* `P_` : Prefab

### 데이터

* `SO_` : ScriptableObject
* `DT_` : Data Table 또는 데이터 에셋

### UI

* `UI_` : UI 프리팹
* `IMG_` : UI 이미지 리소스

### 오디오

* `BGM_` : 배경음
* `SFX_` : 효과음
* `VO_` : 보이스

### 씬

* `SC_` : Scene

예시:

M_CharacterArmor
T_UI_InventoryBg
SO_UnitStatData
UI_ActionSlot
P_PlayerCharacter
SC_Battle

---

## 11. 프리팹 / 오브젝트 네이밍 규칙

## 11-1. 프리팹

* 프리팹은 `P_` 접두사 사용 권장

예시:

P_EnemyGoblin
P_ActionSlot
P_DamagePopup

## 11-2. 씬 오브젝트

* 씬 오브젝트는 역할이 바로 보이게 작성
* 복제 객체는 의미 있는 이름 유지

예시:

GameManager
UIRoot
MainCamera
DirectionalLight
PlayerSpawnPoint

## 11-3. 자동 생성 오브젝트

런타임 생성 객체는 규칙 유지:

Slot_0_0
Slot_1_0
Slot_2_0

또는 더 명확히:

Slot_R0_C0
Slot_R0_C1

행/열 의미가 더 분명해져서 추천합니다.

---

## 12. 프리팹 계층 분리 규칙

특히 캐릭터 프리팹은 로직과 비주얼을 분리합니다.

예시:

P_Character_Handler
├─ Root
├─ Logic
│  ├─ AnimatorDriver
│  ├─ StatusController
│  └─ CombatUnit
└─ Visual
   ├─ Mesh
   ├─ Rig
   ├─ VFX
   └─ WeaponSocket

### 규칙

* 프로그래머는 `Logic`
* 아트는 `Visual`
* 비주얼 관련 수정은 가능하면 `Visual` 하위만 작업

이렇게 해두면 역할 분리가 매우 쉬워집니다.

---

## 13. 인스펙터 세팅 규칙

* 꼭 필요한 값만 SerializeField로 노출
* 런타임 전용 값은 숨김
* 디버그용 값은 명확한 헤더 아래 배치
* 참조 누락 가능성이 있으면 `[Required]`에 준하는 검사 코드 작성

예시:

[Header("References")]
[SerializeField] private RectTransform slotsRoot;

[Header("Debug")]
[SerializeField] private bool showDebugLog;

---

## 14. 금지 / 주의 사항

### 금지

* 의미 없는 파일명 사용
* 공용 씬 무단 수정
* 역할 외 폴더 수정
* 직접 참조 남발로 시스템 결합 증가
* 싱글톤 남용
* 테스트 코드/테스트 에셋을 main에 포함

### 주의

* prefab override를 무심코 올리지 않기
* 씬 저장 전 변경사항 확인
* 자동 생성 코드로 hierarchy를 크게 바꾸는 경우 사전 공유
* EventBus 구독 해제 누락 주의

---

# 최종 권장 한 줄 정리

RELIC의 Unity 컨벤션은 아래 4개 핵심

1. **역할별 폴더 수정 범위 엄수**
2. **코드는 `[SerializeField] private + PascalCase/camelCase` 기반으로 통일**
3. **Hierarchy는 구분용 부모 오브젝트로 정리**
4. **파일명은 접두사(`M_`, `P_`, `SO_`, `UI_`)로 종류를 즉시 식별 가능하게 작성**

