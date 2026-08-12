# GameData Event Excel Sync Plan

## Goal

`Assets/Resources/Data/GameDataRuntime.csv`에 먼저 반영된 이벤트 지속 선택지와 선택지별 비주얼 액션 컬럼을 원본 `Assets/ExcelSource/GameData.xlsx`의 `Event` 시트에 반영하고, 기존 Export 스크립트로 Runtime CSV를 다시 생성한다.

## Scope

- `Event` 시트에 `PersistAcrossNextEvent`, `SuccessVisualObjectId`, `SuccessVisualActionId`, `FailureVisualObjectId`, `FailureVisualActionId` 컬럼을 추가한다.
- `Event_01` 선택지에는 `event_visual_test_crystal`와 선택지 순서별 성공 액션 ID를 기록한다.
- `Event_08` 계열은 공통 종료 선택지 한 행만 유지하고, 해당 행에 `PersistAcrossNextEvent = true`를 기록한다.
- Runtime CSV는 `Assets/Editor/GameDataXlsxToSectionedCsv.ps1`로 재생성한다.

## Verification

- Excel `Event` 시트 헤더와 `Event_01`/`Event_08` 행 값을 직접 검사한다.
- Runtime CSV의 `Event` 헤더, `Event_01` 비주얼 ID, `Event_08` 계열 네 행과 지속 플래그를 검사한다.
- `git diff --check`를 실행한다.
- Unity batchmode 테스트는 프로젝트 규칙에 따라 실행하지 않는다.
