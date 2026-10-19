# 모든 TMP 텍스트 언어별 폰트 자동 적용 설계

## 목적

`LocalizedTMPFont`를 수동 배치한 일부 UI뿐 아니라 씬 배치, 프리팹 복제, 런타임 생성 텍스트를 포함한 모든 `TMP_Text`가 `LocaleFontDatabase`의 현재 언어 폰트를 사용하게 한다.

## 적용 규칙

- `LocalizedTMPFont`가 있는 텍스트는 컴포넌트의 `roleId`와 개별 DB 설정을 우선한다.
- 역할 컴포넌트가 없는 텍스트는 공용 DB의 `body` 역할을 사용한다.
- 역할 컴포넌트가 없어도 현재 폰트가 DB의 `sourceFontRoles`에 등록되어 있으면 해당 역할을 우선한다.
- 구형 `MASTOD__ SDF`는 `number` 역할과 `preserveSourceFont`로 등록해 원본 atlas와 material을 그대로 보존한다.
- 씬 로드, Locale 변경, TMP 텍스트 변경 이벤트에서 적용 대상을 갱신한다.
- 렌더 직전에는 새 TMP의 Instance ID만 탐지해 첫 렌더 전에 폰트를 적용한다.
- 비활성 오브젝트도 씬 전체 갱신에 포함한다.
- DB, Locale 또는 매핑이 없으면 현재 폰트를 변경하지 않는다.
- 이미 목표 폰트가 적용된 경우 재대입하지 않아 TMP 변경 이벤트 재진입을 막는다.

## 구조

`GlobalLocalizedTMPFontApplicator`가 공용 DB 조회와 전체 TMP 탐색을 담당한다. 런타임 초기화 시 `SceneManager.sceneLoaded`, `LocalizationSettings.SelectedLocaleChanged`, `TMPro_EventManager.TEXT_CHANGED_EVENT`, `Application.onBeforeRender`를 구독한다. 렌더 직전 탐색은 이미 처리한 Instance ID를 건너뛰고 제거된 ID를 정리한다.

Domain Reload가 꺼진 에디터에서도 정적 구독이 남지 않도록 `SubsystemRegistration`에서 이벤트 구독과 캐시를 초기화한다. 모든 런타임 콜백은 Play Mode에서만 실제 적용한다.

개별 역할이 필요한 기존 UI는 `LocalizedTMPFont`를 유지한다. 전역 적용기는 해당 컴포넌트를 발견하면 직접 `body`를 덮어쓰지 않고 컴포넌트에 현재 Locale 갱신을 위임한다.

## 검증

- 역할 미지정 TMP에 `body` 폰트가 적용되는지 확인한다.
- 기존 `LocalizedTMPFont(number/title)` 역할이 전역 기본값보다 우선하는지 확인한다.
- 구형 MASTOD 폰트가 `number` 역할의 폰트로 변환되는지 확인한다.
- 프로젝트 전체의 구형 MASTOD 참조를 조사해 동일 증상 범위를 확인한다.
- DB 또는 Locale 누락 시 기존 폰트가 유지되는지 확인한다.
- 런타임 코드 컴파일을 확인한다.
- Unity 에디터가 열려 있다는 전제에 따라 batchmode 테스트는 실행하지 않는다.

## 멀티플레이 영향

클라이언트의 TMP Font Asset 참조만 변경한다. 전투 Command, State, Result/Event, ID, Snapshot 및 네트워크 동기화에는 영향을 주지 않는다.
