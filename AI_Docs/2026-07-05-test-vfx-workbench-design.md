# Test_VFX Workbench Design

## Goal
`Test_VFX` 씬을 전투 결과와 분리된 VFX 테스트 공간으로 만든다. 플레이어 캐릭터와 몬스터를 기준점으로 두고, VFX prefab과 `BattleVfxEntry`의 주요 스폰/렌더/정렬 값을 플레이 중 즉시 바꿔 확인할 수 있게 한다.

## Scope
- 기존 전투 VFX 흐름인 `BattleVfxEntry`, `BattleWorldVfxRenderer`, `BattleWorldVfxSortUtility`를 재사용한다.
- 테스트 씬 전용 컨트롤러가 플레이 모드에서 샘플 플레이어/몬스터를 배치하고 IMGUI 패널을 표시한다.
- 패널에서 prefab, 대상, render mode, object layer, sorting layer, order offset, world offset, rotation, scale, RenderTexture 크기, render camera size, proxy height, lifetime, flip type을 조정한다.
- 기본 유닛 액션 VFX도 빠르게 확인할 수 있도록 Move/Hit/Guard/Attack 버튼을 제공한다.
- 전투 HP, 코스트, 상태이상, 턴 데이터는 변경하지 않는다.

## Architecture
- `TestVfxSpawnSettings`: 테스트용 설정 값을 `BattleVfxEntry`로 변환하는 직렬화 데이터.
- `TestVfxWorkbenchUtility`: 레이어 재귀 적용, renderer sorting 적용, particle restart 같은 순수 보조 기능.
- `TestVfxWorkbench`: `Test_VFX` 씬에서 동작하는 MonoBehaviour. 에디터에서는 `AssetDatabase`로 테스트용 prefab 목록을 자동 수집하고, 플레이 모드 UI와 스폰 버튼을 제공한다.
- `TestVfxSceneBootstrapper`(Editor): `Test_VFX` 씬에 워크벤치 오브젝트가 없으면 메뉴로 설치할 수 있게 한다.

## Multiplayer Boundary
이 기능은 디버그/프레젠테이션 전용이다. 전투 결과 계산, 랜덤 판정, 상태 변경, 네트워크 API는 추가하지 않는다. VFX는 결과를 계산하지 않고 독립적으로 재생만 한다.

## Testing
- EditMode 테스트는 `Assets/Tests/EditMode~/` 아래에 둔다.
- 설정값이 `BattleVfxEntry`에 올바르게 반영되는지 확인한다.
- 레이어 재귀 적용과 direct renderer sorting이 기존 `BattleWorldVfxSortUtility` 규칙과 맞는지 확인한다.

