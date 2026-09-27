# Localization Manager 안전성 및 동적 텍스트 경계 설계

## 목표

- `Localization.xlsx`가 Excel에서 열려 있을 때 전체 적용이 예외와 GUI 오류로 번지지 않게 한다.
- 전체 바인딩 복구가 외부 다운로드 샘플 씬을 열어 기존 Missing Prefab/Lighting 오류를 쏟지 않게 한다.
- 런타임 코드가 내용을 쓰는 TMP를 정적 로컬라이제이션 대상으로 잘못 수집하지 않게 한다.
- 배틀 `MenuRoot/Back2/Name`과 `BattleWarningUI/Text (TMP)`의 실제 소유권을 명확히 한다.
- 영어만 입력된 TMP는 자동 번역 원문으로 추론하지 않고, 검사 결과에서 제외 이유를 알 수 있게 한다.

## 조사 결과

1. Localization Manager의 적용 경로는 `LocalizationWorkbookWriter`가 XLSX ZIP을 업데이트 모드로 열지만, `LocalizationSyncCommand`와 달리 Excel 잠금 파일 사전 검사가 없다.
2. 적용 중 예외가 `OnGUI`의 `BeginHorizontal/EndHorizontal` 사이로 전파되어 `Invalid GUILayout state`가 연쇄 발생한다.
3. `LocalizationTextBindingRepairTool`은 `Assets/Project`의 모든 씬을 실제로 열어 `Assets/Project/Download`의 공급업체 샘플 씬 오류까지 노출한다.
4. `MenuRoot/Back2/Name`은 `BattleSceneController`가 지역명과 턴 번호를 쓰는 동적 TMP다. 런타임에는 정적 로컬라이저를 비활성화하지만 에디터 검사기는 정적 원문으로 본다.
5. `BattleWarningUI/Text (TMP)`는 호출부가 넘긴 메시지를 표시하는 동적 출력 슬롯이며, 씬의 `New Text`는 번역 원문이 아니다.
6. 현재 검사기는 한글 포함 문자열만 신규 원문으로 수집한다. 영어 TMP를 `English(en)` 열로 자동 해석할 근거가 없으므로 수집하지 않는 것이 안전하다.

## 권장 설계

### 적용 실행 보호

- 적용 전에 `~$Localization.xlsx` 존재 여부와 대상 XLSX의 쓰기 가능 여부를 검사한다.
- 잠겨 있으면 적용을 시작하지 않고 명확한 오류 로그와 다이얼로그를 표시한다.
- 버튼 콜백의 적용 예외는 내부에서 처리하여 IMGUI 레이아웃 범위를 벗어나지 않게 한다.

### 검사 범위

- 프로젝트 자체 프리팹과 씬은 계속 검사한다.
- `Assets/Project/Download` 아래 공급업체/샘플 에셋은 검사 및 바인딩 복구 대상에서 제외한다.
- 개별 테스트 씬의 기존 Missing Prefab은 이번 변경에서 복구하지 않는다. 검사 도구가 해당 오류를 새로 만들지는 않으며, 별도의 에셋 정리 작업으로 다룬다.

### 정적/동적 텍스트 경계

- `LocalizationIgnore`가 붙은 TMP 또는 부모는 정적 수집·바인딩 복구·런타임 누락 감시 대상에서 제외한다.
- `Back2/Name`과 `BattleWarningUI` 루트에는 `LocalizationIgnore`를 명시적으로 배치한다.
- `BattleWarningUI.ShowMessage` 호출부가 사용자 표시 문자열을 키 기반으로 해결한 뒤 전달한다.
- `Back2/Name`의 턴/지역명은 동적 결과이므로 정적 `LocalizedTMPText`가 표시를 소유하지 않게 한다. 기존 지역명 문자열의 전면 키 이전은 번역문 확정이 필요한 별도 데이터 작업으로 남기고, 이번 변경에서는 잘못된 정적 수집만 차단한다.

### 영어 입력 정책

- TMP에 영어만 입력해도 `English(en)` 열로 자동 추가하지 않는다.
- 기존 로컬라이제이션 키가 있는 정적 TMP의 영어 번역은 `Localization.xlsx`의 동일 키 `English(en)` 열이 원본이다.
- 동적 TMP의 영어 번역은 호출부가 사용하는 키의 `English(en)` 열이 원본이다.
- 전체 검사 결과에 영어 전용 TMP가 자동 등록 대상이 아니라는 안내를 표시한다.

## 검증

- 잠금 파일/쓰기 잠금 판정 단위 테스트.
- `Download` 경로 제외와 일반 프로젝트 씬 포함 정책 단위 테스트.
- 영어 전용 TMP가 신규 한글 원문 후보가 아님을 고정하는 테스트.
- Battle 씬 YAML에서 두 동적 영역에 `LocalizationIgnore`가 존재하는 회귀 테스트.
- 에디터 C# 프로젝트 컴파일.
- Unity 에디터가 열려 있다는 프로젝트 규칙에 따라 batchmode 테스트는 실행하지 않는다.

## 멀티플레이 영향

전투 상태와 결과를 변경하지 않는다. UI 문자열 수집·표시 경계와 에디터 도구만 변경하므로 멀티플레이 동기화 구조에는 영향이 없다.

## 후속 보완: 동적 원문 자동 수집

- 출력 TMP는 런타임 값의 표시 슬롯이므로 `m_text` 자리표시자를 번역 원문으로 사용하지 않는다.
- `GameLocalization.Get`, `FormatWithFallback`에 리터럴로 선언된 키와 한국어 원문을 안전한 자동 적용 후보로 수집한다.
- 코드의 문자열로 번역 열을 추론하거나 채우지 않는다. 번역 데이터는 워크북 각 언어 열만을 원본으로 사용한다.
- 동일 키가 여러 호출부에 있으면 워크북 갱신은 한국어 원문에만 적용한다.
- `Back2/Name`, `BattleWarningUI/Text (TMP)`, `BattleMapIntroText/IntroText`는 동적 출력으로 유지하고 실제 문구 공급부를 명시적 키 호출로 연결한다.
- 키 없는 임의 문자열은 의미가 같은지 안전하게 판별할 수 없으므로 계속 Review 대상으로 남긴다.

## 런타임 동적 출력 후속 보완

- 씬 시작 때 존재하던 TMP를 영구 제외하던 `SceneStartupTextIds` 정책을 제거한다.
- 로컬리제이션 표 초기화 직후 현재 씬의 TMP를 한 번 검사하고, 이후 모든 TMP 변경 이벤트도 검사한다.
- `LocalizationIgnore`가 붙은 반복 변경 출력은 정적 `LocalizedTMPText` 대신 `DynamicLocalizedTMPText`가 마지막 한국어 원문과 고유 키를 보관한다.
- 이미 `LocalizedTMPText`가 있는 TMP도 런타임 작성자가 다른 한국어 문구를 넣으면 해당 원문의 고유 키로 재구성한다.
- 자동 검사에서는 `BattleWarningUI.ShowMessage`, `ShowBattleWarning`, `BattleMapIntroText.ShowMessage/ShowMessageAndWait`에 직접 전달된 한국어 리터럴만 안전 자동 등록 대상으로 본다.
- 중복 한국어 원문은 임의 키를 선택하지 않는다.
- 선택 로케일 셀이 비어 있으면 다른 로케일로 fallback하지 않고 해당 언어의 `미번역` 문구를 표시한다.
- Unity YAML의 `\v`는 XML에서 허용되지 않는 U+000B로 복원되므로, 스캔 시 의도된 줄바꿈 `\n`으로 정규화한다.
- 그 밖의 XML 비허용 문자는 워크북을 열기 전에 키·열·코드 포인트가 포함된 오류로 차단하여 기존 파일이 잘리지 않게 한다.
- 번역 매니저는 번역문을 임의 생성하지 않으며 한국어 원문과 키만 자동 등록한다.
- 여러 줄 TMP YAML과 명시적인 `displayName` 직렬화 필드도 하나의 온전한 원문으로 검사한다.

## Play Mode 선택 언어 보장

- 편집 잠금 비활성화는 씬·프리팹을 편집할 때 한국어 원문을 보여 주기 위한 기능으로만 사용한다.
- Unity Editor의 Play Mode에서는 편집 잠금 상태와 무관하게 항상 `SelectedLocale`을 조회한다.
- 확인창처럼 런타임에 직렬화 문자열을 다시 쓰는 경로는 `GameLocalization`으로 해결한 값을 전달한다.
- 숫자가 포함된 반복 변경 텍스트는 샘플 숫자 문자열을 원문으로 재사용하지 않고, `{0}` 형식의 명시적 키를 선언한다.
- 선택 언어 셀이 공란이면 한국어 원문을 되살리지 않고 선택 언어별 `미번역` 표기를 유지한다.

## 재수집 및 중복 방지

- `미번역`, `Untranslated`, `未翻訳`, `未翻译`, `Sin traducir`는 표시 결과이지 원문이 아니므로 검사 후보에서 제외한다.
- 이미 등록된 키가 TMP에 연결되어 있으면 현재 화면 문자열로 키를 다시 추측하지 않고 기존 키를 우선한다.
- 한국어 원문을 편집한 경우에는 기존 키의 한국어 원문을 갱신하며 새 해시 키를 만들지 않는다.
- YAML `m_text` 파서는 빈 값에서 다음 Unity 문서 경계를 넘어가지 않아야 한다.
- 전체 적용 마지막에는 신규 행 수와 무관하게 워크북을 String Table에 최종 동기화하여 삭제된 오염 키도 제거한다.
- 동일 한국어라도 문맥별 번역이 가능한 게임데이터 키는 유지하고, 표시 결과 재수집 및 YAML 오독으로 생성된 키만 제거한다.
## 편집 잠금과 Play Mode 분리

- 편집 잠금 해제는 에디터에서 한국어 원문을 보여주는 표시 정책일 뿐이다.
- `LocalizedTMPText` 컴포넌트의 `enabled` 값은 잠금 상태와 무관하게 항상 켜 둔다.
- 전체 검사 및 바인딩 복구는 과거에 비활성 상태로 저장된 `LocalizedTMPText`도 다시 활성화한다.
- 따라서 편집 잠금이 꺼진 채 Play Mode에 진입해도 선택 로케일을 조회하며, 선택 언어 셀이 비어 있으면 해당 언어의 미번역 표식을 표시한다.
- 조회 시 선택 로케일의 String Table 엔트리와 원시 값이 실제로 존재하는지 먼저 확인한다. 엔트리가 없거나 공백이면 패키지의 로케일 fallback 결과를 신뢰하지 않고 즉시 선택 언어의 미번역 표식을 사용한다.

## 동적 로컬라이저 직렬화 안전성

- `DynamicLocalizedTMPText`는 Unity 직렬화 규칙에 맞게 클래스명과 동일한 전용 스크립트 파일에 둔다.
- 전체 검사 및 적용은 Play Mode에서 실행하지 않는다. 런타임 TMP 변경 감시기와 Prefab 편집이 동시에 동작하면 런타임 컴포넌트가 Prefab에 저장될 수 있다.
- 바인딩 복구는 `m_Script: {fileID: 0}`이 있는 Prefab을 저장하지 않고 오류 경로를 남긴 뒤 다음 Prefab으로 진행한다. 하나의 손상된 Prefab이 전체 최종 동기화를 막지 않게 한다.
