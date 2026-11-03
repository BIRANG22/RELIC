# Skill VFX DB 머지 안전성 설계

## 문제

`Assets/DB/SkillVfxDatabase.asset`은 여러 스킬의 VFX 연결을 하나의 YAML 리스트로 저장한다. 서로 다른 브랜치가 같은 파일을 수정하면 Git은 `SkillId`가 아니라 줄 단위로 병합하므로 다른 스킬의 프리팹 GUID가 덮어써질 수 있다.

또한 2026-09-29~10-01에 `_grid`, `_impact`, `_missle` 보조 프리팹을 주 VFX 프리팹에 합친 뒤 삭제했지만 DB의 일부 보조 참조가 남아 Missing 상태가 되었다.

## 권장 설계

1. 삭제된 보조 프리팹을 가리키는 여섯 GUID 참조는 `null`로 정리한다. 보조 연출은 이미 주 VFX 프리팹에 포함되어 있으므로 런타임 표현은 유지된다.
2. `SkillVfxDatabaseIntegrityValidator`가 직렬화 YAML을 검사한다.
   - 빈 `SkillId`
   - 중복 `SkillId`
   - 프로젝트에 존재하지 않는 GUID 참조
3. 에디터 메뉴에서 같은 감사를 수동 실행할 수 있게 한다.
4. EditMode 프로젝트 무결성 테스트가 실제 DB를 검사하여 머지 또는 에셋 삭제로 참조가 깨지면 테스트가 실패하게 한다.

## 구현 계획

### Task 1: 감사 규칙과 회귀 테스트

- `Assets/Tests/EditMode~/SkillVfxDatabaseIntegrityTests.cs`에 가상 YAML 기반 빈 ID, 중복 ID, Missing GUID 검사를 먼저 작성한다.
- 실제 `Assets/DB/SkillVfxDatabase.asset`에 감사 이슈가 없음을 확인하는 프로젝트 무결성 테스트를 작성한다.
- 테스트가 감사 도구 부재 또는 현재 Missing GUID 때문에 실패하는 것을 확인한다.

### Task 2: 감사 도구와 DB 복구

- `Assets/Project/Editor/SkillVfxDatabaseIntegrityValidator.cs`에 순수 텍스트 감사 API와 메뉴 명령을 구현한다.
- 삭제된 보조 프리팹의 DB 참조를 `null`로 정리한다.
- EditMode 테스트 컴파일 및 프로젝트 C# 컴파일로 검증한다.

## 멀티플레이 경계

VFX 연결 데이터와 에디터 검증만 변경한다. 전투 Command, 상태 변경, 결과 이벤트 및 랜덤 처리에는 영향을 주지 않는다.
