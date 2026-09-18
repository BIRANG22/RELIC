# WebGL SteamBattleStateSynchronizer 컴파일 경계 수정

## 문제

`STEAMWORKS_NET`은 Standalone에만 정의되어 있으며 WebGL에는 정의되어 있지 않다. `SteamBattleStateSynchronizer`의 Steam 구현 블록은 이 심볼로 제외되지만, 그 블록 앞의 일부 공개 진입점이 제외된 내부 메서드를 직접 호출한다. 따라서 WebGL 컴파일에서 `CS1061` 및 `CS0103` 오류가 발생한다.

## 설계

- 공개 API는 모든 플랫폼에서 계속 존재시켜 호출하는 전투/UI 코드가 플랫폼 분기문을 갖지 않게 한다.
- Steam 전용 내부 구현 호출은 `#if STEAMWORKS_NET`으로 감싼다.
- Steam 미지원 플랫폼에서는 동기화 요청 메서드가 아무 동작도 하지 않거나 `false`를 반환한다.
- 로컬 싱글플레이 전투 상태는 기존 호출자가 계속 처리하므로, WebGL의 no-op은 Steam 멀티플레이 경로에만 적용된다.

## 검증

- EditMode 소스 회귀 테스트로 Steam 전용 내부 메서드를 호출하는 공개 진입점이 플랫폼 가드 안에 있는지 검사한다.
- Unity 에디터 컴파일로 WebGL 전환 시 오류가 없어졌는지 확인한다.

