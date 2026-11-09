# VFX 사운드 직접 참조 제거 설계

## 의도

실게임에서 사용하는 VFX 프리팹이 `AudioClip`과 `AudioSource`를 직접 보유하지 않게 하고, 모든 VFX 사운드를 `SoundDatabase -> AudioManager` 경로로 재생한다.

## 범위

- SoundDatabase에 등록된 캐릭터 VFX 10개에서 내장 `AudioSource`를 제거한다.
- `GR_Residue`, `Vfx_Gr_poisson`을 새 `Grid Effect VFX SFX` 목록에 등록한다.
- `Vfx_Mon_B_01_attack_02_pre`를 기존 `Monster Skill VFX SFX` 목록에 등록한다.
- 미등록 3개 프리팹의 클립, 볼륨, 피치, 루프 값을 DB 큐로 이전한 뒤 내장 `AudioSource`를 제거한다.
- 프로젝트 소유 실사용 VFX 프리팹에 직접 오디오 참조가 다시 생기지 않도록 EditMode 테스트를 추가한다.

## 비범위

- `Project/Download` 아래 원본 에셋과 아트 테스트 씬은 수정하지 않는다.
- 기존 DB 등록 VFX 10개의 큐와 음원 선택은 변경하지 않는다.
- 전투 판정이나 상태 변경 로직은 변경하지 않는다.

## 접근 방식

1. 기존 Player 목록에 그리드 효과를 함께 등록: 수정량은 적지만 인스펙터 분류가 부정확하다.
2. 별도 Grid Effect 목록 추가: 인스펙터에서 용도를 명확히 구분하면서 기존 프리팹 기반 조회 구조를 재사용할 수 있다.

승인된 방식은 2번이다. 세 목록은 초기화 시 하나의 프리팹 조회 딕셔너리에 등록한다.

## 안전 조건

- 동일 프리팹이 여러 VFX 사운드 목록에 중복 등록되면 기존 경고 동작을 유지한다.
- DB 미등록 VFX는 기존과 같이 소리 없이 처리되며, 내장 AudioSource로 우회하지 않는다.
- VFX 사운드는 연출 계층에만 영향을 주며 멀티플레이 전투 결과에는 영향을 주지 않는다.
