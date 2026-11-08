# Deleted TMP Font Reference Repair Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 삭제된 TMP 폰트와 머티리얼을 참조하는 모든 YAML 에셋을 현재 유효한 폰트로 교체하고 재발을 검사한다.

**Architecture:** Unity `AssetDatabase`로 GUID 존재 여부를 확인하는 EditMode 테스트를 먼저 추가한다. 테스트가 보고한 YAML 참조만 현재 `LocaleFontDatabase`와 일치하는 폰트 및 머티리얼로 최소 교체한다.

**Tech Stack:** Unity 6, C#, NUnit, TextMeshPro, Unity YAML

**Spec:** `AI_Docs/Deleted_TMP_Font_Reference_Repair_Design.md`

## Global Constraints

- 문서는 `AI_Docs`에만 작성한다.
- 테스트는 `Assets/Tests/EditMode~`에만 작성한다.
- Unity 에디터를 직접 제어하거나 batchmode 테스트를 실행하지 않는다.
- 런타임 로컬라이징 동작과 전투 상태는 변경하지 않는다.

## Review Focus

- 삭제된 폰트 GUID와 삭제된 폰트의 서브 머티리얼 GUID를 모두 검출한다.
- 유효한 현재 GUID는 오탐하지 않는다.
- 프리팹뿐 아니라 씬과 ScriptableObject YAML도 검사한다.
- 외부 패키지 및 Library 캐시는 검사 범위에서 제외한다.
- 역할별 폰트를 임의로 기본 폰트로 평준화하지 않는다.

---

### Task 1: TMP YAML 참조 무결성 검사

**Files:**
- Create: `Assets/Tests/EditMode~/TMPFontReferenceIntegrityTests.cs`
- Modify: 검출된 프리팹·씬·에셋 YAML

**Interfaces:**
- Consumes: Unity `AssetDatabase.GUIDToAssetPath(string)`
- Produces: 존재하지 않는 `m_fontAsset` 및 `m_sharedMaterial` GUID를 실패로 보고하는 EditMode 테스트

- [ ] 무결성 테스트를 작성한다.
- [ ] 테스트가 현재 삭제 GUID 참조 때문에 실패하는 것을 확인한다.
- [ ] 보고된 참조를 현재 역할에 맞는 유효한 폰트와 머티리얼로 교체한다.
- [ ] 테스트와 C# 컴파일을 다시 실행해 통과를 확인한다.

