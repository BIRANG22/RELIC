# Localization Excel 단방향 연동 설계

## 목표

`Assets/ExcelSource/Localization.xlsx`를 번역 원본으로 사용하고, Unity Editor 메뉴에서 현재 `Text` String Table Collection으로 단방향 Merge Import한다.

## 워크북 구조

- 파일: `Assets/ExcelSource/Localization.xlsx`
- 시트: `Text`
- 열: `Key`, `Id`, `Korean(ko)`, `English(en)`, `Chinese (Simplified)(zh-Hans)`, `Japanese(ja)`, `Spanish(es)`
- 기존 `ui_start` 항목과 Unity가 발급한 ID를 초기 데이터로 포함한다.
- 새 항목은 `Id`를 비우거나 `0`으로 입력한다.

## Editor 구조

- `LocalizationXlsxReader`는 Open XML 기반 `.xlsx`에서 지정 시트의 셀 문자열을 읽고 Unity CSV 문자열로 변환한다.
- `LocalizationExcelImporter`는 `Tools/Localization/Import Localization Excel` 메뉴를 제공한다.
- 임포터는 Unity Localization의 `Csv.ImportInto`를 사용하여 `Text` 컬렉션을 Merge 갱신하고 에셋을 저장한다.
- 엑셀에 없는 기존 Key는 삭제하지 않는다.
- 외부 Excel 라이브러리, Excel 설치, PowerShell 프로세스에 의존하지 않는다.

## 오류 처리

- 파일, `Text` 시트, `Text` String Table Collection, 필수 `Key` 열이 없으면 에셋을 변경하지 않고 명확한 오류를 출력한다.
- Excel이 저장 중이거나 손상된 경우 예외 내용을 Unity Console에 출력한다.
- Excel 서식이나 표 확장으로 생성된 완전한 빈 행은 CSV에서 제외한다.
- 일부 셀만 존재하는 데이터 행은 헤더 열 개수까지 빈 필드로 채워 CSV 필드 수를 일정하게 유지한다.

## 검증

- EditMode 테스트로 워크북 행 읽기, CSV escaping, 필수 열 검증을 확인한다.
- Editor 어셈블리 컴파일과 실제 워크북 렌더링을 확인한다.

## 영향 범위

Editor 전용 제작 파이프라인이며 런타임 및 멀티플레이 전투 로직에는 영향이 없다.
