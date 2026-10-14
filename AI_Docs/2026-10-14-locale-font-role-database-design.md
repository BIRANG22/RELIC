# 언어별 폰트 역할 Database 설계

## 배경

현재 프로젝트는 `TMP Settings`의 기본 폰트와 각 씬·프리팹의 `TMP_Text.font` 직접 참조를 함께 사용한다. 대부분의 텍스트는 `DungGeunMo SDF`를 사용하며, 일부 UI는 `OptionTMPFontEnforcer` 같은 전용 컴포넌트가 폰트를 다시 지정한다.

Localization은 `LocalizationSettings.SelectedLocaleChanged`를 기준으로 문자열을 갱신하지만, 선택 언어에 따라 주 폰트를 교체하지는 않는다. TMP fallback은 주 폰트에 glyph가 없을 때만 동작하므로, 라틴 문자를 포함하는 주 폰트를 유지한 채 영어·스페인어만 다른 글꼴로 표시하는 용도로는 충분하지 않다.

## 목표

- 기존 `TMP Settings` 기본 폰트와 폰트 안정화 도구를 그대로 유지한다.
- 언어별로 `본문`, `제목`, `숫자` 등 표시 역할에 맞는 TMP Font Asset을 Inspector에서 연결한다.
- 번역 여부와 관계없이 모든 `TMP_Text`가 필요할 때 언어별 역할 폰트를 사용할 수 있게 한다.
- 언어 변경 시 활성·비활성 오브젝트를 포함한 적용 대상이 올바른 폰트로 갱신된다.
- DB, 언어 또는 역할 매핑이 누락되어도 기존 텍스트의 직렬화된 폰트와 TMP 기본 폰트를 안전망으로 사용한다.
- UI 코드가 구체적인 폰트 에셋이나 언어별 분기문을 소유하지 않게 한다.
- 새 언어 또는 폰트 교체 시 enum, switch, 전용 함수를 추가하지 않고 DB 설정만으로 확장한다.

## 비목표

- 기존 모든 TMP 텍스트를 한 번에 새 구조로 마이그레이션하지 않는다.
- TMP Settings의 기본 폰트를 제거하거나 런타임마다 변경하지 않는다.
- 문자열 번역 Key, String Table 또는 Excel Localization 흐름을 변경하지 않는다.
- 폰트 크기, 자간, 줄 간격, Auto Size 같은 타이포그래피 스타일 전체를 이번 DB에서 관리하지 않는다.
- 전투 판정이나 네트워크 동기화 데이터에 폰트 정보를 포함하지 않는다.

## 권장 구조

### 1. `LocaleFontDatabase`

ScriptableObject 하나가 언어별 폰트 프로필 목록을 보관한다.

각 언어 항목은 다음 데이터를 가진다.

- `localeCode`: Unity Localization의 Locale Identifier와 비교할 문자열 ID
- `roleFonts`: 역할 ID와 `TMP_FontAsset`의 매핑 목록
- `defaultFont`: 해당 언어에서 역할 매핑이 없을 때 선택적으로 사용할 언어 기본 폰트

Locale 코드는 `ko`, `en`, `ja`, `zh-Hans`, `es`처럼 현재 Localization 설정의 값을 그대로 사용한다. 완전 일치 항목을 먼저 찾고, 필요하면 `en-US -> en`처럼 부모 언어 코드로 한 번 폴백한다.

DB에는 프로젝트가 실제 사용하는 Locale만 등록한다. 등록되지 않은 Locale은 DB가 임의의 다른 언어 폰트를 반환하지 않고 조회 실패를 반환한다.

### 2. 역할 ID

역할은 enum 대신 직렬화 가능한 문자열 ID로 관리한다. 새 역할을 추가할 때 런타임 코드를 수정하지 않도록 하기 위함이다.

초기 권장 역할은 다음과 같다.

- `body`: 일반 본문, 설명, 버튼 라벨
- `title`: 화면 제목, 패널 제목, 강조 헤더
- `number`: 피해량, 비용, 카운트, 시간처럼 숫자가 중심인 텍스트

필요해질 때만 `caption`, `dialogue`, `combat` 같은 역할을 DB에 추가한다. 역할이 과도하게 늘어나면 프리팹 설정 비용이 커지므로 초기 구현은 세 역할로 제한한다.

문자열 오타를 줄이기 위해 Inspector의 역할 필드는 DB에 등록된 역할 목록을 보여주는 드롭다운 PropertyDrawer를 제공한다. 런타임 저장값은 문자열 ID이므로 역할 추가 자체는 코드 변경을 요구하지 않는다.

### 3. `LocalizedTMPFont`

언어별 폰트를 사용할 `TMP_Text`와 같은 GameObject에 부착하는 독립 컴포넌트다. 번역 컴포넌트가 없는 숫자·고유명사·동적 텍스트도 동일하게 사용할 수 있다.

직렬화 필드는 다음으로 제한한다.

- `database`: 사용할 `LocaleFontDatabase`
- `roleId`: `body`, `title`, `number` 등의 역할 ID
- `target`: 같은 오브젝트의 `TMP_Text`; Reset/OnValidate에서 자동 연결하되 런타임에는 명시적 참조를 사용

컴포넌트 동작은 다음과 같다.

1. 처음 활성화될 때 대상 TMP의 현재 `font`와 `fontSharedMaterial`을 원본 fallback으로 기억한다.
2. `LocalizationSettings.SelectedLocaleChanged`를 구독한다.
3. 활성화 시 현재 Locale을 즉시 적용한다.
4. DB가 `(localeCode, roleId)`에 해당하는 폰트를 반환하면 `target.font`에 적용한다.
5. 언어별 역할 폰트가 없으면 같은 언어의 `defaultFont`를 사용한다.
6. 둘 다 없으면 대상 TMP가 원래 가지고 있던 폰트를 유지 또는 복원한다.
7. 대상의 원본 폰트도 없다면 TMP 자체 기본 폰트 동작에 맡긴다.

폰트를 적용할 때 TMP의 폰트 참조만 교체하고 문자열, 크기, 색상, 정렬, Auto Size는 변경하지 않는다. Font Asset의 기본 Material을 무조건 강제로 넣으면 개별 Outline·Underlay Material Preset이 사라질 수 있으므로 `fontSharedMaterial` 교체는 별도 정책으로 다룬다.

초기 구현에서는 폰트 에셋만 교체하고, 기존 Material Preset이 새 폰트와 호환되지 않을 때 TMP가 선택한 기본 Material을 사용하도록 한다. 폰트별 Material Preset까지 언어 DB에 넣는 확장은 실제 요구가 확인된 뒤 진행한다.

### 4. Database 제공 방식

씬마다 DB를 반복 연결하지 않도록 프로젝트 공용 Database Asset을 `Resources` 또는 기존 프로젝트 Database 접근 패턴을 따르는 단일 위치에 둔다.

권장 방식은 `LocalizedTMPFont`의 `database`가 비어 있으면 공용 Database Provider에서 기본 DB를 조회하고, Inspector에 개별 DB가 지정되어 있으면 그것을 우선하는 방식이다. 이 구조는 대부분의 텍스트에서 연결 작업을 줄이면서 테스트나 특수 UI가 별도 DB를 주입할 수 있게 한다.

Provider는 읽기 전용 참조만 제공하며 런타임 mutable static 상태를 폰트 저장소처럼 사용하지 않는다.

## 폰트 선택 우선순위

폰트 해석 순서는 다음으로 고정한다.

1. 현재 Locale의 정확한 코드와 역할이 일치하는 폰트
2. 부모 언어 코드와 역할이 일치하는 폰트
3. 현재 Locale 또는 부모 언어 프로필의 `defaultFont`
4. `LocalizedTMPFont`가 처음 기억한 대상 TMP의 직렬화된 원본 폰트
5. TMP Settings의 기본 폰트

이 순서에서 4번과 5번이 기존 설정을 예외 안전망으로 남긴다. DB 누락을 이유로 강제로 한국어 폰트를 선택하지 않는다.

## 문자열 Localization과의 관계

`LocalizedTMPText`와 `DynamicLocalizedTMPText`는 문자열만 책임진다. `LocalizedTMPFont`는 폰트만 책임지며 서로 직접 참조하지 않는다.

동일 오브젝트에 두 컴포넌트가 함께 있어도 각각 같은 `SelectedLocaleChanged` 이벤트를 받아 독립적으로 갱신한다. 문자열 테이블 로딩 완료를 기다릴 필요 없이 폰트를 먼저 적용할 수 있으며, 문자열의 비동기 갱신과 폰트 선택이 결합되지 않는다.

번역되지 않는 숫자 텍스트에는 `LocalizedTMPFont(roleId: number)`만 붙인다. 번역되는 제목에는 `LocalizedTMPText`와 `LocalizedTMPFont(roleId: title)`을 함께 붙인다.

## 런타임 생성 UI

런타임에 생성되는 TMP는 다음 둘 중 하나를 따른다.

- 역할이 필요한 프리팹에 `LocalizedTMPFont`를 미리 배치한다.
- 코드가 TMP를 직접 생성하는 불가피한 경우 생성 직후 `LocalizedTMPFont.Configure(database, roleId, target)` 같은 명시적 API로 설정한다.

이름 기반 Transform 검색이나 모든 TMP를 매 프레임 탐색해 자동 적용하지 않는다. 반복 생성 UI는 가능한 한 역할 컴포넌트가 포함된 프리팹을 사용한다.

`TMP_Dropdown`이 생성하는 런타임 목록처럼 원본 Template을 복제하는 UI는 Template의 TMP에 `LocalizedTMPFont`를 배치해 복제본이 설정을 상속하게 한다.

## 기존 폰트 강제 적용 코드와의 충돌 처리

`OptionTMPFontEnforcer`는 `Awake`, `OnEnable`, 지연 Coroutine, `LateUpdate`에서 폰트를 반복 덮어쓸 수 있다. 같은 TMP에 `LocalizedTMPFont`를 붙이면 마지막 실행자가 폰트를 결정하는 경쟁 상태가 생긴다.

마이그레이션 시 다음 원칙을 적용한다.

- 새 구조 적용 대상에서는 `OptionTMPFontEnforcer`의 직접 폰트 강제를 제거하거나 언어 DB 호출로 대체한다.
- 두 컴포넌트가 같은 TMP의 폰트를 동시에 소유하지 않게 한다.
- Dropdown Template과 런타임 List는 `LocalizedTMPFont`가 포함된 Template 복제로 해결하고, `LateUpdate` 폰트 재적용은 제거하는 방향을 우선한다.
- `UIBlurReplicaSource`처럼 원본 TMP의 폰트를 복제하는 표현 코드는 원본 결과를 그대로 복사하므로 유지한다.
- 전투 팝업처럼 코드에서 `text.font`를 직접 지정하는 UI는 새 역할 컴포넌트를 적용할 경우 직접 지정을 제거하고 역할 DB를 단일 소유자로 삼는다.

## Editor 지원

`LocaleFontDatabase` Inspector는 다음 검증을 제공한다.

- 빈 Locale 코드
- 중복 Locale 코드
- 한 언어 안의 중복 역할 ID
- 빈 역할 ID
- 연결되지 않은 Font Asset
- 공용 역할 목록에는 존재하지만 특정 언어에 매핑되지 않은 역할

경고는 설정 실수를 알려주되 Asset 저장을 막지는 않는다. 언어별 디자인 의도에 따라 일부 역할이 `defaultFont`로 폴백할 수 있기 때문이다.

초기 마이그레이션을 돕는 Editor 도구는 선택한 GameObject 계층 또는 선택한 프리팹에만 `LocalizedTMPFont`를 추가한다. 프로젝트 전체에 자동 AddComponent하지 않으며, 각 텍스트의 역할은 사용자가 확인할 수 있게 기본 `body`로만 제안한다. `title`과 `number`는 명시적으로 지정한다.

## Asset 구성 예시

```text
LocaleFontDatabase
├─ ko
│  ├─ defaultFont: DungGeunMo SDF
│  ├─ body: DungGeunMo SDF
│  ├─ title: DungGeunMo SDF
│  └─ number: MASTOD__ SDF
├─ en
│  ├─ defaultFont: <영문 기본 폰트>
│  ├─ body: <영문 본문 폰트>
│  ├─ title: <영문 제목 폰트>
│  └─ number: MASTOD__ SDF
├─ ja
│  ├─ defaultFont: TMP_Font_JP
│  ├─ body: TMP_Font_JP
│  ├─ title: TMP_Font_JP
│  └─ number: MASTOD__ SDF
└─ zh-Hans
   ├─ defaultFont: TMP_Font_CN
   ├─ body: TMP_Font_CN
   ├─ title: TMP_Font_CN
   └─ number: MASTOD__ SDF
```

위 연결은 구조 예시이며 실제 폰트 선택은 구현 시 Inspector에서 결정한다. 각 주 폰트의 fallbackFontAssetTable에는 해당 언어의 누락 glyph를 보완할 Font Asset을 연결한다.

## 오류 및 예외 처리

- DB가 없으면 경고를 최초 한 번만 남기고 원본 폰트를 유지한다.
- Locale이 없거나 Localization 초기화 전이면 원본 폰트를 유지하고 초기화 완료 후 다시 적용한다.
- 역할 ID가 없으면 언어 `defaultFont`를 사용한다.
- Font Asset 참조가 비어 있으면 다음 우선순위로 폴백한다.
- 비활성 오브젝트는 다시 활성화될 때 현재 Locale을 적용하므로 별도 전역 탐색이 필요 없다.
- 빠르게 언어를 연속 변경해도 적용은 현재 Selected Locale을 다시 확인하고 최신 값만 사용한다.
- 에디터 설정 변경이 런타임 Font Asset의 atlas population mode를 변형하지 않게 한다.

## 단계적 적용

1. DB, Provider, Resolver, `LocalizedTMPFont`와 Inspector 검증을 구현한다.
2. 테스트용 소규모 UI에서 `body`, `title`, `number` 역할을 검증한다.
3. Title의 언어 선택 UI와 Option UI를 마이그레이션하고 `OptionTMPFontEnforcer` 충돌을 제거한다.
4. 공용 UI 프리팹부터 역할 컴포넌트를 추가한다.
5. Lobby와 Battle 씬의 직접 배치 TMP를 점진적으로 전환한다.
6. 기존 직접 폰트 지정 코드와 미적용 TMP를 Editor 감사 도구로 확인한다.

한 번에 모든 씬 YAML을 대량 치환하지 않는다. Prefab API와 SerializedObject를 사용하는 Editor 도구 또는 수동 Inspector 작업으로 적용한다.

## 테스트 및 검증

EditMode 테스트는 `Assets/Tests/EditMode~/` 아래에 작성한다.

- 정확한 Locale 코드와 역할 매핑 반환
- `en-US`에서 `en` 부모 코드 폴백
- 역할 누락 시 언어 `defaultFont` 선택
- Locale 누락 시 조회 실패와 원본 폰트 유지
- 중복 Locale/역할 검증
- `LocalizedTMPFont` 활성화 시 현재 Locale 폰트 적용
- 언어 변경 시 번역 컴포넌트가 없는 TMP도 폰트 갱신
- 비활성화 후 이벤트 구독 해제 및 재활성화 시 최신 Locale 적용
- 원본 Font Asset fallback 복원
- 빠른 연속 Locale 변경 시 최종 Locale 폰트 유지

정적 검증은 다음을 포함한다.

- 런타임 코드와 Editor 코드 컴파일
- DB Asset의 Locale 및 역할 누락 검사
- 새 구조 적용 대상에 폰트 소유자가 둘 이상 존재하지 않는지 검사
- 직접 `TMP_Text.font`를 지정하는 기존 코드 목록 감사

Unity Editor 수동 검증은 `ko`, `en`, `zh-Hans`, `ja`, `es`를 순서대로 전환하며 다음을 확인한다.

- 본문·제목·숫자가 각각 지정된 역할 폰트를 사용한다.
- 언어 변경 직후 화면에 이미 존재하던 텍스트가 갱신된다.
- 비활성 패널을 다시 열었을 때 현재 언어 폰트가 적용된다.
- Dropdown 런타임 목록과 동적 생성 프리팹도 같은 규칙을 따른다.
- 한글·일본어·중국어·라틴 문자와 기호의 누락 glyph가 없다.
- Outline, Underlay 등 기존 Material 연출이 훼손되지 않는다.

프로젝트 규칙에 따라 Unity batchmode 테스트는 실행하지 않는다.

## 멀티플레이 영향

이 구조는 클라이언트 표시용 Locale과 TMP Font Asset만 변경한다. 전투 Command, State Change, Result/Event 및 네트워크 Snapshot에는 영향을 주지 않는다. Locale 코드, 역할 ID 또는 폰트 Asset 참조를 멀티플레이 동기화 대상으로 추가하지 않는다.

## 구현 시 예상 파일 범위

- 신규 Runtime: `LocaleFontDatabase`, 폰트 Resolver/Provider, `LocalizedTMPFont`
- 신규 Editor: DB Inspector/PropertyDrawer, 선택 범위 마이그레이션 및 감사 도구
- 신규 Asset: 공용 `LocaleFontDatabase` Asset
- 수정 가능성 있음: `OptionTMPFontEnforcer`와 직접 폰트를 지정하는 일부 UI 코드
- 프리팹·씬: 승인된 마이그레이션 범위의 TMP에 역할 컴포넌트 추가
- 테스트: `Assets/Tests/EditMode~/`의 폰트 DB 및 Locale 전환 테스트

구체적인 파일 경로와 마이그레이션 대상은 구현 계획에서 확정한다.
