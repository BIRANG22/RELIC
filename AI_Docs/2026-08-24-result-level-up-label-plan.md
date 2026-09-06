# Result Level Up Label Plan

## 목표

배틀 결과 보상패널의 경험치 영역에서 기본 표기는 `LV`만 보이고, 실제 레벨이 오른 캐릭터에게만 `UP`이 함께 보이도록 수정한다.

## 조사 결과

- `ExplorationResultCharacterRowUI.SetExperience`는 현재 `levelUpRoot.SetActive(leveledUp)`로 오브젝트 전체를 켜고 끈다.
- Battle 씬의 각 행 `LevelUp` 오브젝트는 TMP 텍스트가 `LV UP` 하나로 배치되어 있다.
- 경험치 획득 여부가 아니라 `BattleStageClearExperiencePreview.LeveledUp` 값이 실제 레벨업 여부를 나타낸다.

## 구현 방침

- 결과 행의 `LevelUp` 텍스트 오브젝트는 행 표시 중 항상 활성화한다.
- 텍스트 값은 `leveledUp == true`면 `LV UP`, 아니면 `LV`로 설정한다.
- 기존 `gainedExperience > 0` 기준이 다시 들어오지 않도록 EditMode 소스/동작 테스트를 갱신한다.
- 전투 결과 계산, 경험치 계산, 멀티플레이 동기화 로직은 변경하지 않는다.

## 검증

- `Assembly-CSharp.csproj` MSBuild
- `Assembly-CSharp-Editor.csproj` MSBuild
