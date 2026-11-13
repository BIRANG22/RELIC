# 전투 예약·아라벨라 연출·보상 버튼 수정 설계

## 목표

1. 타임라인에서 예약 행동을 직접 취소한 뒤 리두로 같은 슬롯과 순서에 복원한다.
2. 아라벨라 등장 연출 종료 시 시네마틱 카메라의 잔여 패럴랙스·흔들림 오프셋 없이 일반 카메라 제어로 인계한다.
3. 전투 배속에서 VFX와 VFX 전용 SFX의 재생 길이를 같은 배속 기준으로 맞춘다.
4. 아라벨라 전기 공격의 준비음을 행동당 한 번만 재생한다.
5. 기억 보상 패널의 넘기기 버튼에 공통 UI 클릭음을 연결한다.

## 권장 구조

### 예약 직접 취소

로컬 사용자 입력인 `RemoveCommand`와 네트워크 상태 적용인 `RemoveCommandFromNetwork`를 구분한다. 로컬 직접 취소는 제거된 `PlayerReservedCommand`, 슬롯, 순서를 리두 스택에 기록하고, 네트워크 수신 경로는 상태만 적용한다. 전투 상태는 계속 `PlayerReservedCommand`와 슬롯 인덱스 기반으로 변경한다.

### 카메라 인계

`BattleCameraController.EndCinematicCameraKeepingCurrentPose`가 시네마틱 소유권을 해제하기 전에 외부 흔들림, 마우스 패럴랙스, 추적 속도를 제거한다. 연출 프리젠터는 카메라 Transform을 직접 보정하지 않고 기존 컨트롤러 API만 사용한다.

### VFX 사운드 배속

`BattleVfxAudioUtility`가 VFX cue의 기본 pitch에 `BattlePresentationSpeedSettings.CurrentMultiplier`를 곱한 유효 pitch를 계산하여 `AudioManager.PlayVfxSfxCue`에 전달한다. 일반 SFX와 BGM은 변경하지 않는다. cue delay도 기존처럼 같은 배속 기준으로 축소한다.

### 아라벨라 전기음

`Arabella.attack.02.prepare` cue를 `playOncePerAction`으로 지정한다. 다중 타격/대상 VFX는 유지하되 준비음만 첫 재생에서 허용한다.

### 기억 보상 넘기기 버튼

전투 씬의 기억 보상 `SkipButton`에는 이미 `DBAudioSource`가 연결되어 있지만, 버튼 콜백이 상호작용을 먼저 잠그면 동일 클릭 프레임의 `DBAudioSource.OnPointerClick`이 차단될 수 있다. `DiscardReward`가 잠금 전에 `ui.normal.click`을 한 번 재생하고, 씬의 포인터 클릭 재생은 꺼서 실행 순서와 무관하게 정확히 한 번만 들리도록 한다.

## 검증

- EditMode 테스트로 직접 취소 후 리두 순서 복원, 카메라 인계 상태 초기화, VFX cue pitch 배속, 아라벨라 cue 일회성, SkipButton 사운드 연결을 검증한다.
- `Assembly-CSharp.csproj`, `Assembly-CSharp-Editor.csproj`를 빌드한다.
- Unity 에디터 화면의 실제 흔들림과 최종 청감은 수동 확인 대상으로 남긴다.

## 멀티플레이 경계

전투 판정과 명령 데이터는 변경하지 않는다. 로컬 입력에서만 히스토리를 기록하고 네트워크 적용 경로는 기존 ID/인덱스 기반 상태 변경을 유지한다. 카메라·VFX·SFX·UI는 결과 이벤트의 프레젠테이션 계층에만 영향을 준다.
