# Test_VFX Workbench 사용법

## 시작

1. `Assets/Project/Scenes/YDM/Test_VFX.unity` 씬을 연다.
2. Unity Play를 누른다.
3. 화면 왼쪽 위의 `Test_VFX Workbench` 패널을 사용한다.
4. 패널이 보이지 않으면 `` ` `` 키를 누른다.

## 빠른 테스트 흐름

1. `VFX Prefab` 영역에서 VFX를 고른다.
2. `Target`을 `Player`, `Monster`, `Midpoint`, `WorldPoint` 중 하나로 고른다.
3. `Spawn Offset`, `Rotation Euler`, `Scale Multiplier` 값을 바꾼다.
4. `Object Layer`, `Sorting Layer`, `Order Offset` 값을 바꿔 렌더 순서를 확인한다.
5. `Play Once` 또는 `Space`로 재생한다.
6. 여러 값을 빠르게 비교하려면 `Repeat` 또는 `R`을 켠다.
7. 필요할 때 `Clear` 또는 `Delete`로 생성된 VFX를 지운다.

## 단축키

| 키 | 동작 |
| --- | --- |
| `` ` `` | 패널 보이기/숨기기 |
| `F1` | 단축키 도움말 보이기/숨기기 |
| `Space` | 현재 선택한 VFX 1회 재생 |
| `Delete` 또는 `C` | 생성된 VFX 정리 |
| `R` | 반복 재생 켜기/끄기 |
| `[` | 이전 VFX 선택 |
| `]` | 다음 VFX 선택 |
| `1` | 타겟을 Player로 변경 |
| `2` | 타겟을 Monster로 변경 |
| `3` | 타겟을 Midpoint로 변경 |
| `4` | 타겟을 WorldPoint로 변경 |
| `F` | 카메라를 테스트 구도에 맞춤 |
| `U` | 플레이어와 몬스터 재배치 |
| `Tab` | Render Mode 순환 |
| `G` | Flip Type 순환 |

텍스트 입력칸을 편집 중일 때는 단축키가 동작하지 않는다. 입력을 끝낸 뒤 빈 영역을 클릭하고 단축키를 사용한다.

## 패널 영역 설명

### Scene

- `Respawn Units`: 플레이어와 몬스터를 다시 생성한다.
- `Frame Camera`: 카메라를 테스트 위치로 되돌린다.
- `Target`: VFX가 기준으로 삼을 위치를 정한다.
- `Follow target for Individual RenderTexture mode`: RenderTexture 프록시 방식에서 타겟을 따라가게 한다.
- `World Point`: 타겟이 `WorldPoint`일 때 사용할 월드 좌표다.

### VFX Prefab

- `Search`: VFX 프리팹 목록을 이름으로 필터링한다.
- `Refresh`: VFX 폴더를 다시 검색한다.
- `<`, `>`: 이전/다음 VFX를 선택한다.
- 목록 버튼: 해당 VFX를 바로 선택한다.

기본 검색 폴더는 `Assets/Project/Art/VFX`다.

### Spawn Data

- `Render Mode`: VFX 렌더링 방식을 고른다.
  - `IndividualWorldRenderTexture`: VFX를 별도 RenderTexture에 렌더링하고 월드 프록시로 보여준다.
  - `SharedRenderTextureOverlay`: 현재 시스템에는 별도 처리 구현이 없어 직접 월드 스폰으로 대체된다.
  - `DirectWorldRenderer`: 프리팹을 월드에 직접 생성하고 Renderer sorting 값을 적용한다.
- `Flip Type`: VFX 좌우/파티클 플립 방식을 테스트한다.
- `SFX`: VFX와 함께 재생할 SFX ID, 딜레이, 볼륨, 내장 AudioSource 처리 방식을 테스트한다.
- `Object Layer`: 생성된 VFX 오브젝트와 자식의 Unity Layer다.
- `Sorting Layer`: Renderer 또는 프록시 Renderer에 적용할 Sorting Layer다.
- `Order Offset`: 기본 sorting order에 더할 값이다.
- `Sorting Y Offset`: Y 정렬 기준 위치를 보정한다.
- `Y Multiplier`: Y 위치를 sorting order로 바꿀 때 쓰는 배율이다.
- `Spawn Offset`: 타겟 위치에서 VFX가 생성될 오프셋이다.
- `Rotation Euler`: 생성된 VFX의 로컬 회전값이다.
- `Scale Multiplier`: 생성된 VFX의 기존 스케일에 곱할 값이다.
- `Proxy Offset`: RenderTexture 프록시 표시 위치 오프셋이다.
- `Proxy Height`: 프록시 Quad의 월드 높이다.
- `RT Width`, `RT Height`: RenderTexture 해상도다.
- `RT Camera Size`: VFX를 찍는 전용 카메라의 orthographic size다.
- `Lifetime`: 생성된 VFX가 유지되는 시간이다.
- `Auto Destroy Direct VFX`: 직접 월드 스폰된 VFX도 자동 제거할지 정한다.

### Unit Action VFX

플레이어와 몬스터 프리팹에 들어 있는 기존 `BattleUnitAnimator` VFX를 바로 호출한다.

- `Move`: 이동 VFX/애니메이션 확인
- `Hit`: 피격 VFX 확인
- `Guard`: 방어 VFX 확인
- `Atk1`, `Atk2`, `Atk3`: 공격 액션별 VFX 확인
- `Status`: `Status Effect Id`에 적은 상태이상 VFX 확인
- `Flip Facing`: 유닛 방향 반전 확인

## 추천 확인 순서

1. `1`, `2`, `3` 키로 타겟 위치 차이를 본다.
2. `[` / `]`와 `Space`로 VFX를 빠르게 넘기며 확인한다.
3. `Object Layer`를 `VFX`, `Default` 등으로 바꿔 카메라/레이어 영향을 확인한다.
4. `Sorting Layer`, `Order Offset`, `Sorting Y Offset`을 바꿔 캐릭터 앞뒤 관계를 맞춘다.
5. `Tab`으로 렌더 모드를 바꿔 RenderTexture 프록시와 직접 월드 스폰의 차이를 비교한다.
6. `G`로 Flip Type을 바꿔 몬스터/플레이어 방향에서 어색하지 않은지 확인한다.
7. 값이 맞으면 해당 값을 실제 프리팹 또는 VFX 설정 데이터에 옮긴다.

## 주의

- 이 씬은 VFX, SFX, 레이어, sorting 테스트용이다.
- HP, 코스트, 턴, 상태이상 결과 같은 전투 핵심 상태는 변경하지 않는다.
- VFX 재생 완료 여부가 전투 결과를 결정하지 않는다.
