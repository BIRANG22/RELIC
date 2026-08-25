# Lobby Tutorial Anchor VFX RenderTexture Plan

## Problem

`LobbyTutorialController` shows `TutorialDisplay/AnchorImage`, and the
`Vfx_root_anchor` prefab under that UI object contains the `Start` and `Loop`
ParticleSystem hierarchy. The particles are world-rendered objects parented
under a UI Canvas hierarchy, so their world size and position can change when
the runtime Game view resolution or Canvas viewport scale changes.

## Investigation

- The saved Lobby scene does not contain an object named `Startw`.
- The affected hierarchy is `LobbyTutorialController > TutorialDisplay >
  AnchorImage > Vfx_root_anchor`.
- `Vfx_root_anchor` contains `Start` and `Loop` ParticleSystem groups.
- The prefab and children are on layer 0, so they can be rendered as world
  particles instead of through the Canvas UI renderer.
- Battle VFX avoid this class of issue by rendering each world VFX in an
  isolated off-screen render space and displaying only a proxy.
- The relic offer fix now uses the same idea: keep the serialized scene VFX as
  a hidden template, clone it into an isolated render space, and display the
  result through a RawImage under the UI element.

## Design

1. Add a reusable Lobby UI world-VFX RenderTexture proxy component.
2. Keep the original UI-child ParticleSystem object hidden as a template.
3. Clone the template into a scene-level render root that is not under the
   Canvas hierarchy.
4. Render the clone with a private orthographic camera into a per-instance
   RenderTexture.
5. Display the texture through a RawImage child of the UI anchor, using
   inspector-controlled proxy size and anchored position.
6. Let `LobbyTutorialController` automatically bind `AnchorImage/Vfx_root_anchor`
   and show or hide the proxy with the tutorial display state.
7. Add EditMode tests proving the source particle root remains inactive and the
   visible output is a fixed-size UI RawImage proxy.

## Relic Offer Prefab Note

The `Vfx_root_relic` objects under `RelicOffer_1` to `RelicOffer_3` are still
used as hidden templates for runtime RenderTexture clones. They should not be
deleted unless the scene is changed to reference a separate prefab asset field
instead.
