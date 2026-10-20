# 배틀룸 이동 전환 Walk 애니메이션 교체 계획

1. 기존 전환 테스트를 `walk` 상태와 VFX 미생성 요구사항으로 변경하고 실패를 확인한다.
2. `BattleUnitAnimator`에 VFX 없는 `PlayWalk()` 경로를 추가한다.
3. `BattleRoomPartyTransitionPresenter`와 `BattleMapSelectionCharacterMarker`가 새 경로를 사용하도록 교체한다.
4. 전환 전용 Move VFX 복제 코드와 불필요해진 테스트를 제거한다.
5. 컴파일, 관련 EditMode 테스트, 변경 범위를 검증한다.

## 이동 속도 조정 추가 작업

1. 배틀 씬의 두 전환 Presenter가 `±2.6666667` 오프셋을 갖는 회귀 테스트를 추가한다.
2. Presenter 코드 기본값과 배틀 씬의 직렬화 값을 동일하게 변경한다.
3. 기존 `0.55초 / 0.6초` 재생 시간이 유지되는지 검증한다.

## 입장 HUD 추가 작업

1. 입장 표시 억제 중 모든 캐릭터 HUD가 비활성화되는 회귀 테스트를 추가한다.
2. 선택 상태를 반영한 후 억제를 해제하면 선택 HUD만 표시되는지 검증한다.
3. `BattleRoomLoader`의 입장 시작과 완료 흐름에 캐릭터 HUD 억제·해제를 연결한다.
