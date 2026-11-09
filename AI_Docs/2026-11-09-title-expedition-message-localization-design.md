# 타이틀 탐사 상태 메시지 번역 설계

## 의도

타이틀에서 탐사 포기 완료 또는 이어하기 데이터 부재 안내가 `Untranslated`/`미번역`으로 표시되지 않도록 원본 워크북과 Unity 런타임 문자열 테이블을 함께 보완한다.

## 범위

- `system.expedition.abandoned`, `system.expedition.missing` 키를 한국어, 영어, 일본어, 중국어 간체·번체, 스페인어로 추가한다.
- `Localization.xlsx`를 원본으로 유지하고 `Text Shared Data.asset`, `Text_*.asset`을 같은 내용으로 동기화한다.
- 필수 런타임 키 목록에 두 키를 추가해 최소 한국어·영어 문구 누락을 회귀 테스트로 방지한다.

## 비범위

- 확인창 문구 `dialog.expedition.abandon_confirm`은 이미 6개 언어가 존재하므로 변경하지 않는다.
- 타이틀 UI 흐름과 저장 데이터 삭제 로직은 변경하지 않는다.

## 문구

| Key | ko | en | ja | zh-Hans | zh-Hant | es |
| --- | --- | --- | --- | --- | --- | --- |
| `system.expedition.abandoned` | 탐사를 포기했습니다. | Expedition abandoned. | 探索を放棄しました。 | 已放弃探索。 | 已放棄探索。 | Expedición abandonada. |
| `system.expedition.missing` | 진행 중인 탐사가 없습니다. | No expedition is in progress. | 進行中の探索はありません。 | 当前没有正在进行的探索。 | 目前沒有正在進行的探索。 | No hay ninguna expedición en curso. |

## 멀티플레이 영향

표시 문자열 데이터만 변경하므로 전투 상태와 동기화에는 영향이 없다.
