# 영어 로컬라이징 누락 및 우회 경로 보완 설계

## 문제

- `Localization.xlsx`의 `Text` 시트에는 한국어 원문이 있지만 영어가 비어 있는 행이 238개 있다.
- 전투 유물 보상 패널의 추출 버튼 키 `ui.battle.canvas.relic_panel.extractbutton.text_tmp.b5d536cd`도 이 누락 집합에 포함된다.
- 일부 스킬 UI는 `GameDataLocalization`을 거치지 않고 `SkillMasterData.Name` 또는 `Details`를 직접 표시한다.
- `GameDataLocalization.ResolveLocalizedOrSource`는 선택 언어의 번역이 없을 때 `Untranslated` 표기를 한국어 원문으로 다시 바꾸므로 영어 화면에 한국어가 노출된다.
- 현재 `Localization.xlsx`와 Unity String Table의 항목 수가 달라 최신 행이 런타임 테이블에 반영되지 않았다.

## 설계

1. 한국어 원문이 있고 영어가 비어 있는 238개 행의 `English(en)`만 채운다.
2. 기존 영어 번역, 다른 언어, 키, 행 순서와 서식은 보존한다.
3. `{E_Strike}`, `{ValueRate1}`, `{CountRate1}` 같은 토큰과 줄바꿈 및 리치 텍스트를 원문과 동일하게 보존한다.
4. 스킬 이름과 설명을 표시하는 확인된 우회 경로는 `GameDataLocalization.SkillName`과 `SkillDetailsTemplate`을 사용한다.
5. 선택 언어 번역이 비어 있을 때 한국어 원문으로 되돌리는 처리는 한국어 로케일에만 허용한다. 영어에서는 `Untranslated`를 유지해 누락을 숨기지 않는다.
6. 워크북을 Unity Localization String Table로 가져와 엑셀과 런타임 테이블을 동기화한다.

## 검증

- 영어 공란 수가 0인지 검사한다.
- 수정 전 존재하던 영어 값이 변경되지 않았는지 검사한다.
- 한국어와 영어의 치환 토큰 집합이 동일한지 검사한다.
- 추출 버튼 및 활성 플레이어 스킬의 이름/설명에 영어가 존재하는지 검사한다.
- 누락 번역이 영어에서 한국어 원문으로 대체되지 않는 회귀 테스트를 추가한다.
- 스킬 설명 포매터와 확인된 표시 경로가 현지화 템플릿을 사용하는지 검증한다.
- Unity 에디터가 열려 있는 상태를 전제로 batchmode 없이 EditMode 테스트를 실행하고 C# 프로젝트를 컴파일한다.

## 멀티플레이 영향

번역 데이터와 UI 표시 문자열 조회 경로만 변경한다. 전투 Command, 상태 변경, 결과 이벤트, 랜덤 및 동기화 데이터에는 영향이 없다.

## 후속 조사: 동적 스킬 효과 문장

스킬 효과 설명은 `SkillDescriptionFormatter`가 런타임에
`skill.sentence.{action}.{unit}.{count}.{position}` 형태의 키를 조합한다.
이 키는 코드에 완전한 문자열 리터럴로 존재하지 않으므로 기존 전체 검사에서 수집되지 않는다.
한국어는 코드 fallback 문장을 사용해 정상처럼 보이지만 영어는 누락 정책에 따라
`Untranslated`를 표시한다.

현재 `GameData.xlsx`의 스킬이 실제로 요구하는 동적 문장 키를 런타임 소스 카탈로그에
명시적으로 등록한다. 카탈로그는 후보 생성, 미사용 정리 보존, 동기화 명령이 공통으로 사용한다.
엑셀에는 해당 키의 한국어·영어 템플릿을 추가하고, 테스트는 현재 스킬 데이터에서 생성되는
모든 문장 키가 카탈로그와 엑셀 양쪽에 존재하는지 검증한다.
