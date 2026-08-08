# 녹턴 포털 미리보기 VFX 프록시 수정 설계

## 원인

`Vfx_Gr_Mon_E_01_move`는 `VFX` 레이어에 있지만 메인 카메라는 해당 레이어를 렌더링하지 않는다. 기존 미리보기 코드는 프리팹을 직접 생성하여 전용 VFX 카메라와 월드 프록시 경로를 건너뛰었기 때문에 오브젝트가 존재해도 화면에 표시되지 않았다.

## 설계

- `GridEffectSpriteDatabase`에서 프리팹을 조회하는 구조는 유지한다.
- 조회한 프리팹으로 `BattleVfxEntry`를 생성하고 `BattleWorldVfxRenderer.TrySpawnDetached`를 호출한다.
- 검정 표현을 보존하도록 `IndividualWorldRenderTexture`와 `Alpha` 프록시 블렌드를 사용한다.
- 예약 해제 및 초기화 시 `BattleWorldVfxHandle.gameObject`를 파괴하여 RenderTexture, 런타임 머티리얼, 렌더 그룹을 함께 정리한다.
- 전투 상태와 명령은 변경하지 않고 프레젠테이션 경로만 수정한다.

## 검증

- DB 매핑 및 런타임 VFX 엔트리 설정을 EditMode 테스트로 고정한다.
- 런타임과 에디터 어셈블리를 컴파일한다.
- Unity 에디터에서 녹턴 이동 예약 시 목적지 표시를 수동 확인한다.
