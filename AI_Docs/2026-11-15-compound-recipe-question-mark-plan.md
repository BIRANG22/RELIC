# 연성 레시피 물음표 글리프 회귀 수정 구현 계획

**목표:** 미발견 레시피의 `???`가 정상 물음표로 표시되도록 Legacy 폰트 회귀를 수정한다.

**설계:** `AI_Docs/2026-11-15-compound-recipe-question-mark-design.md`

**기술:** Unity, TextMeshPro, NUnit EditMode 테스트, YAML 직렬화 에셋

## 제약

- 현재 브랜치와 작업 공간에서 수행한다.
- Unity 에디터를 직접 제어하거나 batchmode 테스트를 실행하지 않는다.
- 테스트는 `Assets/Tests/EditMode~/` 아래에만 둔다.
- 커밋, Push, PR은 수행하지 않는다.

## 작업 1: 회귀 테스트 강화

**파일:**
- 수정: `Assets/Tests/EditMode~/FontQuestionMarkFallbackTests.cs`

- [ ] 실제 `CompoundRecipeSlot/Background/Value`가 Legacy 폰트를 사용하고, 직접 `?`가 아닌 fallback으로 문자를 해결하는지 검사한다.
- [ ] 현재 상태에서 테스트가 잘못된 직접 `?` 문자 때문에 실패하는 것을 확인한다.

## 작업 2: Legacy 폰트 에셋 복구

**파일:**
- 수정: `Assets/Fonts/TMP/DungGeunMo Legacy SDF.asset`

- [ ] U+003F 문자 매핑과 그 매핑에서만 사용하는 잘못된 글리프를 제거한다.
- [ ] 정상 `?`를 제공하는 기존 fallback 폰트 연결을 복원한다.
- [ ] 에셋 구조 검사와 회귀 테스트가 통과하는지 확인한다.

## 작업 3: 검증

- [ ] `Assembly-CSharp.csproj`와 `Assembly-CSharp-Editor.csproj`를 빌드한다.
- [ ] EditMode 테스트 실행이 가능한 비-batchmode 경로가 있으면 실행하고, 없으면 미검증으로 명시한다.
- [ ] 최종 diff에서 요청 범위 밖 변경이 없는지 확인한다.

## 검토 초점

- Legacy 폰트에 U+003F 직접 문자가 남아 있지 않은가
- U+003F 전용 잘못된 글리프가 고아 데이터로 남아 있지 않은가
- fallback 폰트가 실제로 `?`를 제공하는가
- 프리팹의 `Value`가 검증 대상 폰트를 참조하는가
- 발견된 레시피의 한글 이름 렌더링 경로를 바꾸지 않았는가
