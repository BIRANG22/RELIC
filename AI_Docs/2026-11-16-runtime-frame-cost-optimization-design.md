# 런타임 프레임 비용 최적화 설계

## 목적

전투와 튜토리얼 및 블러 UI가 활성화된 동안 매 프레임 반복되는 계층 검색, 컴포넌트 조회, UI 복제 동기화를 줄인다. 전투 결과 계산과 멀티플레이 Command/State/Result 데이터는 변경하지 않는다.

## 조사 결과

- `UIBlurBackgroundManager.LateUpdate`는 블러가 활성화된 동안 모든 replica의 계층 signature를 재귀 계산하고, 모든 Transform/Graphic/Mask를 매 프레임 동기화한다.
- `UIBlurReplicaSource.GraphicPair`는 동기화할 때 Graphic마다 부모 계층의 `CanvasGroup`을 다시 조회한다.
- `BattleUnitAnimator.Update`는 모든 유닛에서 매 프레임 persistent idle VFX 상태를 재평가하며, 몬스터 페이즈 확인을 위해 `GetComponent`/`GetComponentInParent`를 반복한다.
- `BattleUnitAnimator.LateUpdate`는 persistent idle VFX가 존재하면 렌더러 sorting 값을 매 프레임 다시 기록한다.
- `TutorialFocusGuideRunner.UpdatePresentation`은 튜토리얼 진행 중 특정 UI 타깃의 자식 컴포넌트를 매 프레임 다시 수집한다.

## 접근 방식

1. 전체 이벤트 버스 재작성
   - 장기적으로 가장 명확하지만 UI/VFX 생명주기와 프리팹 연결을 넓게 변경하여 회귀 위험이 크다.
2. 일정 프레임 간격 폴링
   - 변경량은 작지만 반복 비용과 간헐적 스파이크가 남고 반응 지연이 생긴다.
3. **변경 감지와 캐시 기반 조기 종료(채택)**
   - 현재 동작과 직렬화 연결을 유지하면서 실제 상태가 바뀐 경우에만 비싼 작업을 수행한다.

## 설계

### UI 블러

- replica 계층의 Transform과 직계 자식 InstanceId를 구성 시 캐시하고, 매 프레임 `GetComponent`를 호출하는 재귀 signature 계산을 제거한다.
- `CanvasGroup` 목록과 부모 관계는 replica 구성 시 캐시하여 프레임별 배열 할당을 제거한다.
- 동적 텍스트, 색상, 스프라이트 동기화는 기존과 같이 유지해 표시 회귀를 피한다.
- RenderTexture 해상도 정책은 화질 변경을 동반하므로 이번 범위에서는 유지한다.

### Persistent Idle VFX

- 소유 `MonsterUnit`과 sorting 기준 Renderer를 최초 조회 후 캐시한다.
- 몬스터 ID, 방향, suspend 상태, 원하는 VFX entry를 작은 상태 snapshot으로 비교한다.
- snapshot이 변하지 않으면 VFX 재평가 및 sorting 갱신을 즉시 종료한다.
- 실제 sorting layer/order가 달라졌을 때만 자식 Renderer에 값을 기록한다.

### 튜토리얼 포커스

- `SkillList`, `StatusEffectList`, `Artifact`의 실제 표시 자식 목록을 타깃 변경 시 캐시한다.
- 자식 계층이 변경되었거나 등록된 항목이 파괴되었을 때만 다시 수집한다.
- 움직이는 타깃의 화면 좌표 계산은 유지하되 `GetComponentsInChildren` 반복 호출만 제거한다.

## 비목표

- 전투 규칙, 피해 계산, 예약 명령, 네트워크 snapshot 형식 변경
- 모든 `Update`를 이벤트 시스템으로 일괄 전환
- 블러 셰이더 및 RenderTexture 해상도 변경
- VFX 풀링 시스템 신규 도입

## 검증

- EditMode 테스트에서 dirty 상태가 없을 때 replica 동기화가 생략되는지 검증한다.
- EditMode 테스트에서 idle VFX snapshot 및 sorting 상태가 동일할 때 갱신이 생략되는지 검증한다.
- EditMode 테스트에서 튜토리얼 포커스 자식 캐시가 계층 변경 전까지 재사용되는지 검증한다.
- `Assembly-CSharp.csproj`와 `Assembly-CSharp-Editor.csproj`를 컴파일한다.
- Unity Profiler와 실제 에디터 화면 확인은 프로젝트 규칙에 따라 미검증 항목으로 보고한다.

## 멀티플레이 경계

UI와 VFX presentation의 갱신 빈도만 변경한다. 전투 Command, State Change, Result/Event, RuntimeId 및 네트워크 snapshot에는 영향이 없다.
