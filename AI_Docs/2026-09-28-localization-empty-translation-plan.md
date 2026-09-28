# 빈 번역 셀 표시 수정 구현 계획

1. 구형 `LocalizeStringEvent`와 `LocalizedTMPText`가 함께 있을 때의 실패 테스트를 추가한다.
2. 최신 로케일 갱신만 적용되는 버전 계약 테스트를 추가한다.
3. 로비의 세 `Erosion_Value`가 동적 출력으로 분리되는 씬 회귀 테스트를 추가한다.
4. `LocalizedTMPText`가 초기화와 로케일 테이블 준비를 기다린 뒤 최신 요청만 갱신하도록 수정한다.
5. `ErosionDifficultyCatalogUI`의 문장 출력 참조를 문장용 TMP로 교정한다.
6. 세 `Erosion_Value`에 `LocalizationIgnore`를 배치하고 정적 바인딩을 제거한다.
7. 관련 EditMode 테스트와 C# 컴파일을 검증한다.
