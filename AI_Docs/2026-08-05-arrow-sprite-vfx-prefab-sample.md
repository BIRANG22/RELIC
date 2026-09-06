# 화살 스프라이트 VFX 프리팹 샘플

## 목적

`Assets/Project/Art/VFX/SpriteAni/화살` 폴더에 있는 2D 스프라이트 애니메이션을 전투 VFX 프리팹으로 연결할 때의 기본 형태를 확인하기 위한 샘플을 만든다.

## 조사 결과

- `New Animation.anim`은 `SpriteRenderer.m_Sprite`를 21프레임으로 교체하는 애니메이션이다.
- 샘플레이트는 12fps이고 길이는 1.75초다.
- `화살을_마법화살_같은_느낌으로_바꿔줘_00008.controller`는 `New Animation.anim`을 기본 상태로 가진 Animator Controller다.
- 전투 VFX 스폰 쪽은 생성된 prefab에 `BattleVfxPlaybackPauseController`를 런타임에 붙이고, `Renderer` 정렬은 `BattleVfxEntry` 설정으로 보정한다.

## 권장 프리팹 구조

샘플 프리팹은 같은 GameObject에 아래 컴포넌트만 가진다.

- `Transform`
- `SpriteRenderer`
- `Animator`

애니메이션 클립의 바인딩 경로가 비어 있으므로 `SpriteRenderer`와 `Animator`가 같은 GameObject에 있어야 한다. 첫 프레임 PNG를 `SpriteRenderer`의 초기 sprite로 지정하고, Animator Controller는 기존 controller를 연결한다.

## 생성 파일

- `Assets/Project/Art/VFX/SpriteAni/화살/Vfx_SpriteAni_Arrow.prefab`

## 전투 연결 시 참고

나중에 스킬ID별 고유 이펙트 DB를 만들 때는 이 prefab을 `BattleVfxEntry.prefab` 또는 새 스킬 VFX DB의 prefab 필드에 연결하면 된다. 이 prefab 자체는 전투 결과를 계산하지 않고 연출 리소스로만 사용한다.
