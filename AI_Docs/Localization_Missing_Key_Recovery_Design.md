# 로컬라이제이션 누락 키 복구 설계

## 문제

- `SkillMaster.Details`가 `{E_Strike} {ValueRate1}`처럼 토큰만 포함하면 한글 감지 조건을 통과하지 못해 전체 검사에서 제외된다.
- 첫 전투 튜토리얼은 `LocalizationKeys` 상수를 사용하지만 실제 키가 워크북에 없고, 런타임 한국어 폴백이 누락을 가려 왔다.
- 일부 스킬의 `EffectIds` 오타가 실제 Effect DB 키와 일치하지 않는다.

## 권장 구조

1. 게임 데이터 스캔은 플레이어 노출 열을 먼저 판별하고, `SkillMaster.Details`에 한해 한글 또는 로컬라이즈 가능한 토큰이 있으면 수집한다.
2. 튜토리얼 문구의 원문은 에디터 로컬라이제이션 소스 카탈로그 한 곳에 등록한다. 전체 검사와 동기화가 같은 카탈로그를 사용한다.
3. 런타임 튜토리얼 컨트롤러는 키만 전달하고 한국어 폴백 문자열을 보유하지 않는다.
4. 누락된 Skill Details 및 Tutorial 키를 `Localization.xlsx`에 생성하고 모든 언어 셀을 채운다.
5. 잘못된 EffectId는 `GameData.xlsx` 원본에서 수정한 뒤 런타임 CSV와 일치시킨다.

## 검증 기준

- 토큰 전용 Details가 자동 수집 대상이다.
- 활성 스킬의 name/details 키와 첫 전투 튜토리얼 7개 키가 워크북에 존재하고 영어가 비어 있지 않다.
- 튜토리얼 런타임 코드에 한국어 폴백 및 fallback 상태가 없다.
- 런타임 데이터에 `E_Chargeana`, `E_RestoreManaPerHitTarge`가 없다.

