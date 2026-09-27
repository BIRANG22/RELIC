# Localization Manager 안전성 및 동적 텍스트 경계 구현 계획

## 목표

전체 검사 및 적용의 잠금/검사 범위 오류를 방지하고, 배틀 동적 TMP 두 곳을 정적 로컬라이제이션 흐름에서 제외한다.

## 작업 순서

1. 잠금 및 검사 경로 정책 테스트를 먼저 추가하고 실패를 확인한다.
2. 재사용 가능한 에디터 정책 함수로 XLSX 잠금과 제외 경로를 판정한다.
3. Localization Manager 적용 버튼에 사전 검사와 예외 처리를 연결한다.
4. 바인딩 복구 도구가 제외 경로의 프리팹·씬을 열지 않도록 한다.
5. Battle 씬의 `Back2/Name`, `BattleWarningUI`에 `LocalizationIgnore`를 명시적으로 연결한다.
6. 런타임 누락 감시가 `LocalizationIgnore` 정책을 동일하게 따르는지 테스트한다.
7. 영어 전용 TMP 정책 안내를 Localization Manager UI에 표시한다.
8. C# 컴파일과 정적 회귀 검증을 수행한다.

## 변경 파일

- `Assets/Editor/LocalizationManagerWindow.cs`
- `Assets/Editor/LocalizationTextBindingRepairTool.cs`
- `Assets/Editor/LocalizationEditorSafetyPolicy.cs` (신규)
- `Assets/Project/Scripts/Core/Localization/RuntimeLocalizationMissingRegistry.cs`
- `Assets/Project/Scenes/YDM/Battle.unity`
- `Assets/Tests/EditMode~/LocalizationEditorSafetyPolicyTests.cs` (신규)
- `Assets/Tests/EditMode~/LocalizationDynamicTextBoundaryTests.cs` (신규)

## 범위 제외

- 사용자가 확정하지 않은 영어 번역문 생성
- 외부 VFX 샘플 및 `TestAni`의 Missing Prefab 복구
- 전투 상태 및 네트워크 동기화 변경
- 커밋, Push, PR

## 후속 구현 항목

1. 명시적 `GameLocalization` 호출의 키와 한국어 원문 파서를 추가한다. 번역 열은 코드에서 채우지 않는다.
2. Localization Manager가 해당 결과를 자동 신규/갱신 후보로 적용한다.
3. 워크북 작성기가 `English(en)` 신규 값과 기존 값 갱신을 지원한다.
4. 세 동적 출력의 실제 공급부를 명시적 키 호출로 이전한다.
5. 파서, 언어 열 보존, 실제 프로젝트 공급부 발견을 EditMode 테스트로 검증한다.

## 후속 구현

- [x] 씬 초기 TMP 영구 제외 제거 및 초기화 직후 전체 TMP 연결
- [x] `LocalizationIgnore` 동적 출력용 런타임 바인더 추가
- [x] 기존 정적 바인딩의 런타임 문구 교체 감지
- [x] 동적 표시 API의 직접 한국어 리터럴 자동 검사 추가
- [x] 배틀 인트로/경고 및 추가 제보 경로를 동일한 공통 경로로 처리
- [x] 선택 로케일 공란에서 교차 로케일 fallback 차단
- [x] Unity YAML `\v`를 줄바꿈으로 정규화하고 XML 비허용 문자 저장 전 검증
- [x] 여러 줄 TMP 및 직렬화된 `displayName` 자동 검사 추가
