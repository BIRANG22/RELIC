# Localization Manager 설계

## 목표

기존 Unity Localization `Text` String Table, `Localization.xlsx`, `GameLocalization` 및 `GameDataLocalization`을 단일 런타임 경로로 유지한다. 개발자가 한국어 원문을 작성한 뒤 `Tools/Localization/Localization Manager`에서 스캔·안전 적용하여 신규 키, Excel 행, TMP 연결을 만들 수 있게 한다.

## 구조

- Runtime: `LocalizedTMPText`는 `GameLocalization.Get`만 사용해 Locale 변경 시 TMP를 갱신한다. 에디터/개발 빌드에서 한 번만 텍스트 변경 이벤트를 관찰하는 registry가 미연결 한국어를 기록한다. 프레임 polling이나 씬 전체 탐색은 없다.
- Editor: 검사기는 C# 문자열, 씬, 프리팹, `GameData.xlsx`, 기존 `LocalizeStringEvent`를 `LocalizationScanResult`로 수집한다. 적용기는 명백한 TMP 항목만 `LocalizeStringEvent`로 연결하며, 애매한 Script 문자열은 보고만 한다.
- Workbook: Open XML writer가 `Text` 시트의 신규 행만 병합한다. 기존 행과 번역 열은 수정하지 않으며 Apply마다 `.backup` 복사본을 하나 만든다. 이후 기존 importer가 String Table을 동기화한다.

## 키와 판정

- TMP 키는 이미 연결된 키를 최우선으로 유지하며, 동일 한국어가 하나의 기존 키와만 일치하면 재사용한다. 그 외에는 source asset/계층/object 이름을 정규화한 `ui.<source>.<object>` 키를 만들고 충돌 시 숫자 suffix를 붙인다.
- GameData 키는 `GameLocalization.BuildDataKey(sheet, stableId, field)` 규칙을 사용한다. 헤더가 `name`, `description`, `tooltip`, `title`, `introduction`, `choice` 등 표시용으로 판정되고 안정 ID 열이 있을 때만 자동 후보가 된다.
- Script의 `TMP_Text.text = "한국어"`만 안전 후보이며, `Debug.Log`, Editor 폴더, 보간·연산·복합식은 확인 필요 항목이다.

## 안전성과 검증

- Scan과 Apply는 분리되고, 전체 적용도 안전 TMP/Excel 항목만 적용한다. 미사용·중복·누락·변경 Korean은 삭제하거나 덮어쓰지 않고 결과로만 보고한다.
- Editor 코드와 Open XML 작업은 `Assets/Editor`에만 둔다. 런타임은 String Table 조회·이벤트 구독·개발 누락 기록만 포함한다.
- 전투 판정 및 네트워크 상태에는 번역 텍스트를 전달하지 않고 기존 ID와 데이터만 유지한다.
