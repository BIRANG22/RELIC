# GameData 현지화 최신화 설계

## 원인

- Localization Excel Import는 Localization.xlsx만 String Table로 복사하며 GameData.xlsx 변경을 선행 반영하지 않는다.
- 따라서 동일 안정 키의 원문 변경, 새 ID, 삭제된 ID가 Localization.xlsx에 남거나 누락된다.
- 로비 SkillSettingPanel은 배틀이 사용하는 EffectIds 기반 SkillDescriptionFormatter 경로를 우회한다.

## 설계

- 수동 Localization Import 전에 GameData 현지화 원본을 동기화한다.
- 동일 키 원문 변경 시 한국어를 갱신하고 기존 번역은 비워 오래된 의미가 표시되지 않게 한다.
- 새 키를 추가하고 더 이상 존재하지 않는 GameData 키는 제거한다.
- 스키마용 두 번째 헤더 행은 데이터로 취급하지 않는다.
- 로비와 공통 GameData 스킬 설명도 EffectIds를 포함한 SkillDescriptionFormatter를 사용한다.

## 영향

- 현지화 에디터 파이프라인과 표시 문자열만 변경한다.
- 전투 상태, 판정, 랜덤에는 영향을 주지 않는다.
