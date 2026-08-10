# 준비 번역문 교체 설계

## 조사 결과

- `Assets/ExcelSource/Localization.xlsx`의 `Text` 시트는 `Key`, `Id`, `Korean(ko)`, `English(en)`, `Chinese (Simplified)(zh-Hans)`, `Japanese(ja)`, `Spanish(es)` 컬럼을 사용한다.
- 사용자가 제공한 한국어 원문은 현재 엑셀의 `Korean(ko)` 값과 직접 매칭할 수 있다.
- 같은 한국어 원문이 여러 Key에 반복되는 항목이 있다. 예를 들어 `가까운 캐릭터에게\n이동합니다.`는 여러 몬스터 특수 행동 설명에 사용된다.
- `강철 닻`처럼 엑셀 번역 컬럼과 Unity `Text_*.asset`에 한국어 또는 깨진 문자열이 남아 있는 항목이 있다.
- `검심`은 단독 원문 행이 없고 `검심 Ⅰ`부터 `검심 Ⅴ`까지 등급 suffix가 붙은 룬 이름으로 존재한다.

## 권장 설계

- 사용자가 제공한 한국어 원문을 기준으로 `Text` 시트의 모든 매칭 행을 찾는다.
- 매칭 행의 `English(en)`, `Chinese (Simplified)(zh-Hans)`, `Japanese(ja)`, `Spanish(es)` 값만 제공 번역문으로 교체한다.
- 엑셀의 `Key`를 `Text Shared Data.asset`의 `m_Id`와 매칭한 뒤, 각 locale asset의 `m_Localized` 값도 같은 번역문으로 동기화한다.
- 한국어 원문 컬럼과 기존 Key/Id는 변경하지 않는다.
- 줄바꿈 표기 `\n`은 현재 엑셀 데이터 관례에 맞춰 문자 그대로 유지한다.
- `검심 Ⅰ` 같은 등급 suffix 항목은 제공 번역문 뒤에 기존 suffix를 보존해 적용한다.
