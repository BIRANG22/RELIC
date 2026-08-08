# 녹턴 포털 미리보기 VFX 프록시 수정 계획

1. `BattleVfxEntry`가 월드 RenderTexture와 Alpha 블렌드를 사용하는 실패 테스트를 추가한다.
2. 직접 프리팹 생성 코드를 `BattleWorldVfxRenderer.TrySpawnDetached` 경로로 교체한다.
3. 예약 참조 카운트와 프록시 핸들 정리 동작을 유지한다.
4. 런타임 및 에디터 어셈블리를 컴파일하고 변경 범위를 검토한다.
5. Unity 에디터 수동 재현이 필요한 항목을 완료 보고에 명시한다.
