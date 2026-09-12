# Char_04 UI 유휴 애니메이션 참조 복구

## 원인

`D_UI_idle.prefab`은 UI `Image`와 Animator를 사용하며, D UI 클립도 `Image.m_Sprite`를 대상으로 한다. 그러나 `D_Robby_UI_idle.controller`의 상태가 현재 프로젝트에 존재하지 않는 이전 AnimationClip GUID를 참조해 모든 모션이 유실돼 있었다.

## 변경

- `ines_select_idle`, `ines_skill_idle`, `ines_rune_idle` 상태를 대응하는 현재 D UI 유휴 클립에 연결한다.
- `ines_select_to_skill`, `ines_rune_to_select`, `ines_skill_to_rune` 상태를 대응하는 전환 클립에 연결한다.
- 역방향 상태는 기존처럼 같은 전환 클립을 음수 속도로 재생한다.

## 검증

- 에디터 테스트로 각 상태가 기대 AnimationClip을 가리키는지 확인한다.
- C# 컴파일과 에셋 참조 검사를 수행한다.

## 속도 정규화

- D UI 클립은 16프레임을 60 FPS(약 0.267초)로 저장해 A/B/C UI 프리뷰보다 약 3.75배 빠르게 재생되고 있었다.
- 여섯 D UI 클립의 키프레임 시간을 1/16초 간격으로 재배치하고, 샘플레이트와 종료 시간을 각각 16 FPS, 1초로 통일한다.
